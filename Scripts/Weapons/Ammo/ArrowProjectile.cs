using Godot;

public partial class ArrowProjectile : Area3D
{
	[Export] public float StickTime = 30f;
	[Export] public Vector3 VisualRotationOffsetDegrees = Vector3.Zero;
	[Export] public GpuParticles3D TrailParticles;
	
	private Vector3 _velocity;
	private float _gravity;
	private int _damage;
	private bool _stuck = false;
	private float _stuckTimer = 0f;

	public void Setup(Vector3 direction, int damage, float speed, float gravity)
	{
		_damage = damage;
		_gravity = gravity;
		_velocity = direction.Normalized() * speed;

		LookAt(GlobalPosition + direction, Vector3.Up);
		RotationDegrees += VisualRotationOffsetDegrees;
	}

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		if (TrailParticles == null)
			TrailParticles = GetNodeOrNull<GpuParticles3D>("ArrowTrailParticles");
	}

	public override void _PhysicsProcess(double delta)
	{
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

		_stuck = true;
		_velocity = Vector3.Zero;
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
}
