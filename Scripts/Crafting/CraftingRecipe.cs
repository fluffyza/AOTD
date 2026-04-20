using Godot;

[GlobalClass]
public partial class CraftingRecipe : Resource
{
	public enum CraftingStationTier
	{
		Backpack = 0,
		Workbench = 1
	}

	[Export] public string RecipeId = "";
	[Export] public CraftingStationTier MinimumStation = CraftingStationTier.Backpack;

	[Export] public bool IsShapeless = true;

	[Export] public string OutputItemId = "";
	[Export] public int OutputAmount = 1;

	[Export] public Godot.Collections.Array<RecipeIngredient> Ingredients = new();

	[Export] public Godot.Collections.Array<string> PatternRows = new();
	[Export] public Godot.Collections.Dictionary<string, string> PatternKey = new();
}
