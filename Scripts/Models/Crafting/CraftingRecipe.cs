using Godot;

public partial class CraftingRecipe : Resource
{
	[Export] public string RecipeId = "";
	[Export] public string StationType = "";
	[Export] public bool IsShapeless = true;

	public Godot.Collections.Array<RecipeIngredient> Inputs = new();

	public ItemDefinition OutputItem;
	public int OutputAmount = 1;
}
