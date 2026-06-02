using Godot;
using System.Collections.Generic;

public partial class SwordSlashArea : Area3D
{
	[Export] public Sprite3D SlashSprite;
	[Export] public float LifeTime = 0.22f;
	[Export] public float KnockbackForce = 4.0f;
	[Export] public float ForwardSpeed = 3.0f;

	private Vector3 _moveDirection = Vector3.Zero;
	private Player _owner;
	private int _damage = 2;
	private float _timer = 0f;

	private readonly HashSet<Mob> _hitMobs = new();

	public void Setup(Player owner, int damage, Vector3 moveDirection)
	{
		_owner = owner;
		_damage = damage;
		_moveDirection = moveDirection.Normalized();
	}

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		if (SlashSprite == null)
			SlashSprite = GetNodeOrNull<Sprite3D>("Sprite3D");
	}

	public override void _Process(double delta)
	{
		_timer += (float)delta;
		GlobalPosition += _moveDirection * ForwardSpeed * (float)delta;

		float t = Mathf.Clamp(_timer / LifeTime, 0f, 1f);

		if (SlashSprite != null)
		{
			Color c = SlashSprite.Modulate;
			c.A = 1f - t;
			SlashSprite.Modulate = c;
		}

		if (t >= 1f)
			QueueFree();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		foreach (Node3D body in GetOverlappingBodies())
		{
			TryHitBody(body);
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		TryHitBody(body);
	}
	
	private void TryHitBody(Node3D body)
	{
		Mob mob = FindMob(body);

		if (mob == null || _hitMobs.Contains(mob))
			return;

		_hitMobs.Add(mob);

		mob.TakeDamage(_damage);

		if (_owner != null)
		{
			Vector3 knockDir = mob.GlobalPosition - _owner.GlobalPosition;
			knockDir.Y = 0f;

			if (knockDir.LengthSquared() > 0.001f)
				mob.ApplyKnockback(knockDir.Normalized() * KnockbackForce);
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
}
