using Godot;

[GlobalClass]
public partial class ProcessingRecipe : Resource
{
	[Export] public string StationType = "";
	[Export] public string InputItemId = "";
	[Export] public string OutputItemId = "";
	[Export] public float ProcessTimeSeconds = 5.0f;
}
