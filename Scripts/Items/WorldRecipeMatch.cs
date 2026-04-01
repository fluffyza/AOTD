using Godot;
using System.Collections.Generic;

public class WorldRecipeMatch
{
	public string RecipeId;
	public string ResultItemId;
	public List<Vector3I> OccupiedCells = new();
}
