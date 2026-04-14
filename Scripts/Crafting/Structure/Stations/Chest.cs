using Godot;

public partial class Chest : CraftedStructure
{
	[Export] public NodePath AnimationPlayerPath = "MeshPivot/chest/AnimationPlayer";
	[Export] public NodePath StorageContainerPath = "StorageContainer";

	private AnimationPlayer _animationPlayer;
	private StorageContainer _storageContainer;
	private bool _isOpen = false;

	public override void _Ready()
	{
		_animationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
		_storageContainer = GetNodeOrNull<StorageContainer>(StorageContainerPath);

		if (_storageContainer == null)
			GD.PrintErr($"{Name}: StorageContainer is missing.");
	}

	public StorageContainer GetStorageContainer()
	{
		return _storageContainer;
	}

	public void Interact(BackpackUI backpackUi)
	{
		if (backpackUi == null || _storageContainer == null)
			return;

		if (_animationPlayer != null)
		{
			if (!_isOpen)
			{
				_animationPlayer.Play("Open");
				_isOpen = true;
			}
		}

		backpackUi.OpenChest(this, _storageContainer);
	}

	public void CloseChestVisual()
	{
		if (_animationPlayer == null || !_isOpen)
			return;

		_animationPlayer.PlayBackwards("Open");
		_isOpen = false;
	}
}
