using Godot;

public partial class Mob : CharacterBody3D
{
	[Export] public Sprite3D Sprite;
	[Export] public float LightDetectionRange = 20f;
	[Export] public float DarkBrightness = 0.15f;
	[Export] public float LitBrightness = 1.0f;
	[Export] public float LightingLerpSpeed = 5f;

	private float _currentBrightness = 1f;

	[Export] public float MoveSpeed = 0.8f;
	[Export] public float WanderRadius = 4.0f;
	[Export] public float MoveTimeMin = 1.2f;
	[Export] public float MoveTimeMax = 3.0f;
	[Export] public float IdleTimeMin = 0.8f;
	[Export] public float IdleTimeMax = 1.8f;

	[Export] public float FloatAmplitude = 0.15f;
	[Export] public float FloatSpeed = 2.0f;

	private readonly RandomNumberGenerator _rng = new();

	private Vector3 _spawnPosition;
	private Vector3 _moveDirection = Vector3.Zero;

	private float _stateTimer = 0f;
	private float _floatTime = 0f;
	private bool _isMoving = false;
	
	private float _minX = -20f;
	private float _maxX = 20f;
	private float _minZ = -20f;
	private float _maxZ = 20f;

	private Sprite3D _sprite;

	public override void _Ready()
	{
		if (Sprite == null)
			Sprite = GetNodeOrNull<Sprite3D>("Sprite3D");
		_rng.Randomize();

		_spawnPosition = GlobalPosition;
		_sprite = GetNodeOrNull<Sprite3D>("Sprite3D");

		StartIdle();
	}
	
	public void SetWanderBounds(float minX, float maxX, float minZ, float maxZ)
	{
		_minX = minX;
		_maxX = maxX;
		_minZ = minZ;
		_maxZ = maxZ;
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateMobLighting(delta);
		float dt = (float)delta;

		_stateTimer -= dt;
		_floatTime += dt;

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

			velocity = _moveDirection * MoveSpeed;
		}

		Velocity = velocity;
		MoveAndSlide();
		ClampToBounds();
		UpdateFloating();
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

	private void UpdateFloating()
	{
		if (_sprite == null)
			return;

		float yOffset = Mathf.Sin(_floatTime * FloatSpeed) * FloatAmplitude;
		_sprite.Position = new Vector3(0f, 0.8f + yOffset, 0f);
	}
}
