using Godot;

public partial class SpritePivot : Node3D
{
	public override void _Process(double delta)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null)
			return;

		Vector3 direction = camera.GlobalPosition - GlobalPosition;
		direction.Y = 0;

		if (direction.LengthSquared() < 0.001f)
			return;

		float angle = Mathf.Atan2(direction.X, direction.Z);

		GlobalRotation = new Vector3(
			GlobalRotation.X,
			angle,
			GlobalRotation.Z
		);
	}
}
