using Godot;

public partial class Chest : CraftedStructure
{
	private AnimationPlayer _animationPlayer;
	private bool _isOpen = false;

	public override void _Ready()
	{
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("MeshPivot/chest/AnimationPlayer");
	}

	public void Interact()
	{
		if (_animationPlayer == null)
			return;

		if (_isOpen)
			_animationPlayer.PlayBackwards("Open");
		else
			_animationPlayer.Play("Open");

		_isOpen = !_isOpen;
	}
}
