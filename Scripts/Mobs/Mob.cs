using Godot;
using System.Collections.Generic;

public partial class Mob : CharacterBody3D
{
	private enum MobMode
	{
		Inactive,
		Active,
		Attacking,
		RetreatingFromCrystal
	}
	
	[Export] public float CrystalSafeRadius = 6.0f;
	[Export] public float CrystalStareTime = 3.0f;
	[Export] public float CrystalRetreatTime = 2.0f;
	[Export] public float CrystalRetreatSpeed = 0.9f;

	private Node3D _sunfallCrystal;
	private float _crystalStareTimer = 0f;
	private float _crystalRetreatTimer = 0f;
	private Vector3 _crystalRetreatDirection = Vector3.Zero;
	
	private ShaderMaterial _mobMaterial;
	[Export] public int MaxLife = 10;
	private int _life;
	private bool _hasStartedChasing = false;
	
	[Export] public Sprite3D Sprite;
	[Export] public Node3D Player;

	[Export] public Texture2D[] InactiveIdleFrames;
	[Export] public Texture2D[] InactiveMoveFrames;
	[Export] public Texture2D[] ActiveIdleFrames;
	[Export] public Texture2D[] ActiveMoveFrames;
	[Export] public Texture2D[] AttackFrames;

	[Export] public float AnimationFps = 8f;

	[Export] public float ActivateRange = 4.0f;
	[Export] public float DeactivateRange = 8.0f;
	[Export] public float AttackRange = 1.0f;

	[Export] public float InactiveMoveSpeed = 0.6f;
	[Export] public float ActiveMoveSpeed = 0.8f;
	[Export] public float WanderRadius = 4.0f;
	[Export] public float MoveTimeMin = 1.2f;
	[Export] public float MoveTimeMax = 3.0f;
	[Export] public float IdleTimeMin = 0.8f;
	[Export] public float IdleTimeMax = 1.8f;

	[Export] public float LightAvoidanceRange = 8f;
	[Export] public float LightAvoidanceStrength = 1.2f;

	[Export] public float LightDetectionRange = 20f;
	[Export] public float DarkBrightness = 0.15f;
	[Export] public float LitBrightness = 1.0f;
	[Export] public float LightingLerpSpeed = 5f;

	[Export] public float FloatAmplitude = 0.0f;
	[Export] public float FloatSpeed = 0.0f;
	[Export] public float BaseSpriteHeight = 0.6f;
	
	[Export] public float ShaderPixelSize = 1.0f;

	private readonly RandomNumberGenerator _rng = new();

	private MobMode _mode = MobMode.Inactive;

	private int _frameIndex = 0;
	private float _animTimer = 0f;
	private string _currentAnim = "";
	private float _activeWaitTimer = 0f;
	
	private Vector3 _spawnPosition;
	private Vector3 _moveDirection = Vector3.Zero;
	private Vector3 _knockbackVelocity = Vector3.Zero;

	private float _stateTimer = 0f;
	private float _floatTime = 0f;
	private float _currentBrightness = 1f;
	private bool _isMoving = false;
	private bool _hasHitThisAttack = false;

	private float _minX = -20f;
	private float _maxX = 20f;
	private float _minZ = -20f;
	private float _maxZ = 20f;
	
	[Export] public float WatchRange = 4.0f;
	[Export] public float ChaseRange = 3.0f;

	public override void _Ready()
	{
		if (Sprite == null)
			Sprite = GetNodeOrNull<Sprite3D>("Sprite3D");

		if (Player == null)
			Player = GetTree().GetFirstNodeInGroup("player") as Node3D;
			
		_sunfallCrystal = GetTree().GetFirstNodeInGroup("sunfall_crystal") as Node3D;
			
		if (Sprite.MaterialOverride is ShaderMaterial originalMat)
		{
			_mobMaterial = originalMat.Duplicate() as ShaderMaterial;
			Sprite.MaterialOverride = _mobMaterial;
		}
		
		_rng.Randomize();

		_spawnPosition = GlobalPosition;
		_life = MaxLife;

		ApplyShaderSetup();
		
		if (_mobMaterial != null)
		{
			_mobMaterial.SetShaderParameter("pixel_size", ShaderPixelSize);
		}

		StartIdle();
		PlayAnimation("inactive_idle");

		GD.Print($"Mob spawned with {_life} life.");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		UpdateMobLighting(delta);
		UpdateState(dt);
		UpdateAnimationState();
		UpdateAnimation(dt);
		UpdateFloating(dt);

		MoveAndSlide();
		ClampToBounds();
	}
	
	private bool IsPlayerInsideCrystalSafeZone()
	{
		if (_sunfallCrystal == null || Player == null)
			return false;

		Vector3 crystalPos = _sunfallCrystal.GlobalPosition;
		Vector3 playerPos = Player.GlobalPosition;

		crystalPos.Y = 0f;
		playerPos.Y = 0f;

		return crystalPos.DistanceTo(playerPos) <= CrystalSafeRadius;
	}

	private void UpdateState(float dt)
	{
		if (Player == null)
			return;

		float distanceToPlayer = GlobalPosition.DistanceTo(Player.GlobalPosition);
		bool visibleToCamera = IsVisibleToPlayerCamera();

		if (_mode == MobMode.Inactive)
		{
			if (distanceToPlayer <= WatchRange && !visibleToCamera)
			{
				BecomeActive();
				return;
			}

			UpdateInactiveWander(dt);
		}
		else if (_mode == MobMode.Active)
		{
			if (distanceToPlayer >= DeactivateRange && !visibleToCamera)
			{
				BecomeInactive();
				return;
			}

			if (!_hasStartedChasing)
			{
				if (distanceToPlayer > ChaseRange)
				{
					Velocity = Vector3.Zero;
					PlayAnimation("active_idle");
					return;
				}

				if (!visibleToCamera)
					_hasStartedChasing = true;
				else
				{
					Velocity = Vector3.Zero;
					PlayAnimation("active_idle");
					return;
				}
			}
			
			if (WouldEnterCrystalSafeZone())
			{
				StartCrystalRetreat();
				return;
			}

			if (distanceToPlayer <= AttackRange && !visibleToCamera)
			{
				StartAttack();
				return;
			}

			ChasePlayer();
		}
		else if (_mode == MobMode.RetreatingFromCrystal)
		{
			if (!IsPlayerInsideCrystalSafeZone())
			{
				_mode = MobMode.Active;
				return;
			}

			if (_crystalStareTimer > 0f)
			{
				_crystalStareTimer -= dt;
				Velocity = Vector3.Zero;
				PlayAnimation("active_idle");
				return;
			}

			if (_crystalRetreatTimer > 0f)
			{
				_crystalRetreatTimer -= dt;
				Velocity = _crystalRetreatDirection * CrystalRetreatSpeed;
				PlayAnimation("active_move");
				return;
			}

			BecomeInactive();
			return;
		}
		else if (_mode == MobMode.Attacking)
		{
			Velocity = Vector3.Zero;
		}

		Velocity += _knockbackVelocity;
		_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, dt * 8f);
	}
	
	private bool WouldEnterCrystalSafeZone()
	{
		if (_sunfallCrystal == null)
			return false;

		Vector3 crystalPos = _sunfallCrystal.GlobalPosition;
		Vector3 mobPos = GlobalPosition;

		crystalPos.Y = 0f;
		mobPos.Y = 0f;

		return crystalPos.DistanceTo(mobPos) <= CrystalSafeRadius;
	}
	
	private void StartCrystalRetreat()
	{
		if (_sunfallCrystal == null)
			return;

		_mode = MobMode.RetreatingFromCrystal;
		_crystalStareTimer = CrystalStareTime;
		_crystalRetreatTimer = CrystalRetreatTime;

		Vector3 away = GlobalPosition - _sunfallCrystal.GlobalPosition;
		away.Y = 0f;

		if (away.LengthSquared() < 0.001f)
			away = -GlobalTransform.Basis.Z;

		_crystalRetreatDirection = away.Normalized();

		Velocity = Vector3.Zero;
		PlayAnimation("active_idle");

		GD.Print("Mob stopped at crystal boundary.");
	}

	private void UpdateInactiveWander(float dt)
	{
		_stateTimer -= dt;

		if (_stateTimer <= 0f)
		{
			if (_isMoving)
				StartIdle();
			else
				StartMoving();
		}

		Vector3 velocity = Vector3.Zero;

		if (_isMoving)
		{
			Vector3 toSpawn = _spawnPosition - GlobalPosition;
			toSpawn.Y = 0f;

			if (toSpawn.Length() > WanderRadius)
				_moveDirection = toSpawn.Normalized();

			Vector3 lightAvoidance = GetLightAvoidanceDirection();

			Vector3 finalDirection = (_moveDirection + lightAvoidance * LightAvoidanceStrength);
			finalDirection.Y = 0f;

			if (finalDirection.LengthSquared() > 0.001f)
				finalDirection = finalDirection.Normalized();

			velocity = finalDirection * InactiveMoveSpeed;
			//PlayAnimation("inactive_move");
		}
		else
		{
			//PlayAnimation("inactive_idle");
		}

		Velocity = velocity;
	}

	private void UpdateAnimationState()
	{
		if (_mode == MobMode.Attacking)
			return;

		if (_mode == MobMode.RetreatingFromCrystal)
			return;

		if (_mode == MobMode.Inactive)
			PlayAnimation(_isMoving ? "inactive_move" : "inactive_idle");
		else if (_mode == MobMode.Active)
			PlayAnimation(Velocity.LengthSquared() > 0.01f ? "active_move" : "active_idle");
	}

	private void ChasePlayer()
	{
		Vector3 direction = Player.GlobalPosition - GlobalPosition;
		direction.Y = 0f;

		if (direction.LengthSquared() > 0.001f)
			direction = direction.Normalized();

		Velocity = direction * ActiveMoveSpeed;
		PlayAnimation("active_move");
	}

	private void BecomeActive()
	{
		_hasStartedChasing = false;
		_mode = MobMode.Active;
		_isMoving = false;
		_hasHitThisAttack = false;
		_currentAnim = "";
		_activeWaitTimer = 2.0f;
		Velocity = Vector3.Zero;
		PlayAnimation("active_idle");
		GD.Print("Mob became ACTIVE.");
	}

	private void BecomeInactive()
	{
		_hasStartedChasing = false;
		_mode = MobMode.Inactive;
		_hasHitThisAttack = false;
		_currentAnim = "";
		StartIdle();
		PlayAnimation("inactive_idle");
		GD.Print("Mob became INACTIVE.");
	}

	private void StartAttack()
	{
		_mode = MobMode.Attacking;
		_hasHitThisAttack = false;
		Velocity = Vector3.Zero;
		PlayAnimation("attack");
	}

	private void OnAttackFinished()
	{
		if (!_hasHitThisAttack)
		{
			_hasHitThisAttack = true;
			GD.Print("Player got hit.");
		}

		_mode = MobMode.Active;
	}

	private void PlayAnimation(string animName)
	{
		if (_currentAnim == animName)
			return;

		_currentAnim = animName;
		_frameIndex = 0;
		_animTimer = 0f;

		ApplyFrame(GetCurrentFrames());
	}

	private void UpdateAnimation(float dt)
	{
		Texture2D[] frames = GetCurrentFrames();

		if (frames == null || frames.Length == 0)
			return;

		_animTimer += dt;

		if (_animTimer < 1f / AnimationFps)
			return;

		_animTimer = 0f;
		_frameIndex++;

		// Attack hit happens on frame 2, index 1
		if (_currentAnim == "attack" && _frameIndex == 1 && !_hasHitThisAttack)
		{
			_hasHitThisAttack = true;
			GD.Print("Player got hit.");
		}

		if (_frameIndex >= frames.Length)
		{
			if (_currentAnim == "attack")
			{
				_mode = MobMode.Active;
				_currentAnim = "";
				PlayAnimation("active_idle");
				return;
			}

			_frameIndex = 0;
		}

		ApplyFrame(frames);
	}

	private Texture2D[] GetCurrentFrames()
	{
		return _currentAnim switch
		{
			"inactive_idle" => InactiveIdleFrames,
			"inactive_move" => InactiveMoveFrames,
			"active_idle" => ActiveIdleFrames,
			"active_move" => ActiveMoveFrames,
			"attack" => AttackFrames,
			_ => InactiveIdleFrames
		};
	}

	private void ApplyFrame(Texture2D[] frames)
	{
		if (Sprite == null || frames == null || frames.Length == 0)
			return;

		_frameIndex = Mathf.Clamp(_frameIndex, 0, frames.Length - 1);

		Texture2D texture = frames[_frameIndex];
		Sprite.Texture = texture;

		if (_mobMaterial != null)
			_mobMaterial.SetShaderParameter("texture_albedo", Sprite.Texture);
	}

	private bool IsVisibleToPlayerCamera()
	{
		Camera3D camera = GetViewport().GetCamera3D();

		if (camera == null)
			return false;

		if (!camera.IsPositionInFrustum(GlobalPosition))
			return false;

		return true;
	}

	private Vector3 GetLightAvoidanceDirection()
	{
		var lights = GetTree().GetNodesInGroup("world_light");
		Vector3 avoid = Vector3.Zero;

		foreach (Node node in lights)
		{
			if (node is not OmniLight3D light)
				continue;

			if (!light.Visible)
				continue;

			float distance = GlobalPosition.DistanceTo(light.GlobalPosition);

			if (distance > LightAvoidanceRange)
				continue;

			Vector3 away = GlobalPosition - light.GlobalPosition;
			away.Y = 0f;

			if (away.LengthSquared() < 0.001f)
				continue;

			float strength = 1f - Mathf.Clamp(distance / LightAvoidanceRange, 0f, 1f);
			avoid += away.Normalized() * strength;
		}

		return avoid;
	}

	private void UpdateMobLighting(double delta)
	{
		float lightFactor = GetNearestWorldLightFactor(LightDetectionRange);

		float targetBrightness = Mathf.Lerp(
			DarkBrightness,
			LitBrightness,
			lightFactor
		);

		_currentBrightness = Mathf.Lerp(
			_currentBrightness,
			targetBrightness,
			(float)delta * LightingLerpSpeed
		);

		Color tint = new Color(
			_currentBrightness,
			_currentBrightness,
			_currentBrightness,
			1f
		);

		if (Sprite != null)
			Sprite.Modulate = tint;
	}

	private float GetNearestWorldLightFactor(float maxDistance)
	{
		var lights = GetTree().GetNodesInGroup("world_light");
		float bestFactor = 0f;

		foreach (Node node in lights)
		{
			if (node is not OmniLight3D light)
				continue;

			if (!light.Visible)
				continue;

			float distance = GlobalPosition.DistanceTo(light.GlobalPosition);

			if (distance > maxDistance)
				continue;

			float normalized = 1f - Mathf.Clamp(distance / maxDistance, 0f, 1f);
			float factor = normalized * normalized * normalized;

			bestFactor = Mathf.Max(bestFactor, factor);
		}

		return bestFactor;
	}

	private void StartMoving()
	{
		_isMoving = true;
		_stateTimer = _rng.RandfRange(MoveTimeMin, MoveTimeMax);

		float angle = _rng.RandfRange(0f, Mathf.Tau);

		_moveDirection = new Vector3(
			Mathf.Cos(angle),
			0f,
			Mathf.Sin(angle)
		).Normalized();
	}

	private void StartIdle()
	{
		_isMoving = false;
		_stateTimer = _rng.RandfRange(IdleTimeMin, IdleTimeMax);
		_moveDirection = Vector3.Zero;
	}

	private void UpdateFloating(float dt)
	{
		if (Sprite == null)
			return;

		_floatTime += dt;

		float yOffset = Mathf.Sin(_floatTime * FloatSpeed) * FloatAmplitude;
		Sprite.Position = new Vector3(0f, BaseSpriteHeight + yOffset, 0f);
	}

	private void ClampToBounds()
	{
		Vector3 pos = GlobalPosition;

		bool hitEdge = false;

		if (pos.X < _minX)
		{
			pos.X = _minX;
			hitEdge = true;
		}
		else if (pos.X > _maxX)
		{
			pos.X = _maxX;
			hitEdge = true;
		}

		if (pos.Z < _minZ)
		{
			pos.Z = _minZ;
			hitEdge = true;
		}
		else if (pos.Z > _maxZ)
		{
			pos.Z = _maxZ;
			hitEdge = true;
		}

		GlobalPosition = pos;

		if (hitEdge)
		{
			Vector3 toSpawn = _spawnPosition - GlobalPosition;
			toSpawn.Y = 0f;

			if (toSpawn.LengthSquared() > 0.01f)
				_moveDirection = toSpawn.Normalized();
		}
	}

	private void ApplyShaderSetup()
	{
		if (Sprite == null)
			return;

		if (_mobMaterial != null)
		{
			_mobMaterial.SetShaderParameter("pixel_size", ShaderPixelSize);

			if (Sprite.Texture != null)
				_mobMaterial.SetShaderParameter("texture_albedo", Sprite.Texture);
		}
	}

	public void SetWanderBounds(float minX, float maxX, float minZ, float maxZ)
	{
		_minX = minX;
		_maxX = maxX;
		_minZ = minZ;
		_maxZ = maxZ;
	}

	public void TakeHit(Player player)
	{
		int damage = 1;

		if (player != null && player.IsHoldingItem("iron_sword"))
			damage = 2;

		TakeDamage(damage);
	}

	public void TakeDamage(int damage)
	{
		_life -= damage;

		GD.Print($"Mob hit for {damage}. Life: {_life}/{MaxLife}");

		if (_life <= 0)
		{
			GD.Print("Mob died.");
			QueueFree();
		}
	}

	public void ApplyKnockback(Vector3 force)
	{
		_knockbackVelocity = force;
	}
}
