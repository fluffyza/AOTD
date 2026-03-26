using Godot;

[GlobalClass]
public partial class RecipeIngredient : Resource
{
	[Export] public string ItemId = "";
	[Export] public int Amount = 1;
}
