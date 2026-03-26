using Godot;
using System;

[GlobalClass]
public partial class CraftingRecipe : Resource
{
	[Export] public string RecipeId = "";
	[Export] public string StationType = "Backpack";

	[Export] public bool IsShapeless = true;

	[Export] public string OutputItemId = "";
	[Export] public int OutputAmount = 1;

	// Shapeless recipes use Ingredients
	[Export] public Godot.Collections.Array<RecipeIngredient> Ingredients = new();

	// Shaped recipes use a small pattern like:
	// "C."
	// "S."
	[Export] public Godot.Collections.Array<string> PatternRows = new();

	// Symbol -> itemId
	// Example: "C" => "coal", "S" => "stick"
	[Export] public Godot.Collections.Dictionary<string, string> PatternKey = new();
}
