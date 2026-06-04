using Godot;

public partial class ArrowProjectile : Area3D
{
	[Export] public float StickTime = 30f;
	[Export] public Vector3 VisualRotationOffsetDegrees = Vector3.Zero;
	[Export] public GpuParticles3D TrailParticles;
	[Export] public Vector3 FlyingFireGravity = new Vector3(0f, 2f, 3f);
	[Export] public Vector3 StuckFireGravity = new Vector3(0f, 6f, 0f);
	
	[Export] public float StickIntoSurfaceAmount = 0.25f;
	
	private Vector3 _velocity;
	private float _gravity;
	private int _damage;
	private bool _stuck = false;
	private float _stuckTimer = 0f;
	private float _lightTime;
	
	[Export] public GpuParticles3D FireParticles;
	[Export] public OmniLight3D FireLight;

	private bool _isFireArrow = false;


	public void Setup(Vector3 direction, int damage, float speed, float gravity, bool isFireArrow = false)
	{
		_damage = damage;
		_gravity = gravity;
		_velocity = direction.Normalized() * speed;
		_isFireArrow = isFireArrow;
			
		if (FireParticles != null)
			FireParticles.Emitting = _isFireArrow;
			
		if (FireLight != null)
			FireLight.Visible = _isFireArrow;

		if (_isFireArrow)
			SetFireParticleGravity(FlyingFireGravity);

		LookAt(GlobalPosition + direction, Vector3.Up);
		RotationDegrees += VisualRotationOffsetDegrees;
	}

	public override void _Ready()
	{
		if (FireLight != null)
			FireLight.Visible = false;
	
		BodyEntered += OnBodyEntered;
		if (TrailParticles == null)
			TrailParticles = GetNodeOrNull<GpuParticles3D>("ArrowTrailParticles");
	}

	public override void _PhysicsProcess(double delta)
	{
		
		if (_isFireArrow && FireLight != null)
		{
			_lightTime += (float)delta;

			FireLight.LightEnergy =
				0.6f +
				Mathf.Sin(_lightTime * 18f) * 0.08f +
				Mathf.Sin(_lightTime * 27f) * 0.04f;
		}
		
		float dt = (float)delta;

		if (_stuck)
		{
			if (TrailParticles != null)
				TrailParticles.Emitting = false;
				
			_stuckTimer += dt;

			if (_stuckTimer >= StickTime)
				QueueFree();

			return;
		}

		_velocity.Y -= _gravity * dt;
		GlobalPosition += _velocity * dt;

		if (_velocity.LengthSquared() > 0.01f)
		{
			LookAt(GlobalPosition + _velocity.Normalized(), Vector3.Up);
			RotationDegrees += VisualRotationOffsetDegrees;
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_stuck)
			return;

		if (TrailParticles != null)
			TrailParticles.Emitting = false;

		Mob mob = FindMob(body);
		if (mob != null)
		{
			mob.TakeDamage(_damage);
			mob.ApplyKnockback(_velocity.Normalized() * 3f);
			QueueFree();
			return;
		}
		
		GlobalPosition += _velocity.Normalized() * StickIntoSurfaceAmount;
		_stuck = true;
		_velocity = Vector3.Zero;

		if (_isFireArrow)
		{
			SetFireParticleGravity(StuckFireGravity);

			if (FireParticles != null)
			{
				FireParticles.Restart();
				FireParticles.Emitting = true;
			}
		}
	}

	private Mob FindMob(Node node)
	{
		while (node != null)
		{
			if (node is Mob mob)
				return mob;

			node = node.GetParent();
		}

		return null;
	}
	
	private void SetFireParticleGravity(Vector3 gravity)
	{
		if (FireParticles == null)
			return;

		if (FireParticles.ProcessMaterial is ParticleProcessMaterial material)
			material.Gravity = gravity;
	}


}
