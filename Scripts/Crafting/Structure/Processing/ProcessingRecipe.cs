using Godot;

[GlobalClass]
public partial class ProcessingRecipe : Resource
{
	public enum ProcessingStationType
	{
		Campfire = 0,
		Furnace = 1
	}

	[Export] public ProcessingStationType StationType = ProcessingStationType.Furnace;
	[Export] public string InputItemId = "";
	[Export] public string OutputItemId = "";
	[Export] public float ProcessTimeSeconds = 5.0f;
}
