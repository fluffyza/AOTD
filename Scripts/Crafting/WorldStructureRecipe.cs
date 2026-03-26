using Godot;

[GlobalClass]
public partial class WorldStructureRecipe : Resource
{
	[Export] public string RecipeId = "";
	[Export] public string OutputItemId = "";
	[Export] public PackedScene OutputScene;

	// Bottom to top layers.
	// Each layer is rows.
	[Export] public Godot.Collections.Array<string> Layer0 = new();
	[Export] public Godot.Collections.Array<string> Layer1 = new();

	// Symbol -> itemId
	[Export] public Godot.Collections.Dictionary<string, string> KeyMap = new();
}
