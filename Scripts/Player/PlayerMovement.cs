using Godot;

public partial class PlayerMovement : Node
{
	[Export] public float Speed = 6.0f;
	[Export] public float JumpVelocity = 4.5f;

	private float _gravity;

	public override void _Ready()
	{
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	}

	public void HandlePhysics(CharacterBody3D body, double delta)
	{
		Vector3 velocity = body.Velocity;

		if (!body.IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		if (Input.IsActionJustPressed("jump") && body.IsOnFloor())
			velocity.Y = JumpVelocity;

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (body.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
		}

		body.Velocity = velocity;
		body.MoveAndSlide();
	}
}
