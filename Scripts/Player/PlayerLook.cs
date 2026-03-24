using Godot;

public partial class PlayerLook : Node
{
	[Export] public float MouseSensitivity = 0.0025f;
	[Export] public NodePath HeadPath;

	private Node3D _head;

	public override void _Ready()
	{
		_head = GetNode<Node3D>(HeadPath);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public void HandleInput(InputEvent @event, Node3D playerBody)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			playerBody.RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			_head.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

			Vector3 headRot = _head.Rotation;
			headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-80), Mathf.DegToRad(80));
			_head.Rotation = headRot;
		}

		if (@event.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseModeEnum.Visible;

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
			Input.MouseMode = Input.MouseModeEnum.Captured;
	}
}
