using Godot;

public partial class Furnace : CraftedStructure
{
	[Export] public NodePath ProcessingContainerPath;

	private ProcessingContainer _processingContainer;

	public override void _Ready()
	{
		base._Ready();

		if (ProcessingContainerPath != null && !ProcessingContainerPath.IsEmpty)
			_processingContainer = GetNodeOrNull<ProcessingContainer>(ProcessingContainerPath);
	}

	public override void _Process(double delta)
	{
		_processingContainer?.Tick(delta);
	}

	public ProcessingContainer GetProcessingContainer()
	{
		return _processingContainer;
	}
}
