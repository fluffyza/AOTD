using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class WorldCraftingManager : Node
{
	private readonly List<WorldStructureRecipe> _recipes = new();

	public override void _Ready()
	{
		BuildWorldRecipes();
	}

	private void BuildWorldRecipes()
	{
		_recipes.Clear();

		_recipes.Add(new WorldStructureRecipe
		{
			RecipeId = "log_stone_to_workbench",
			OutputItemId = "workbench",
			OutputScene = GD.Load<PackedScene>("res://Scenes/placeable_workbench.tscn"),
			Layer0 = new Godot.Collections.Array<string>
			{
				"WW",
				"WW"
			},
			Layer1 = new Godot.Collections.Array<string>
			{
				"SS",
				".."
			},
			KeyMap = new Godot.Collections.Dictionary<string, string>
			{
				{ "W", "wood" },
				{ "S", "stone" }
			}
		});
	}
	
	private Vector3I GetOpenSideDirectionForRotation(int rotation)
	{
		return rotation switch
		{
			0 => new Vector3I(0, 0, 1),
			1 => new Vector3I(1, 0, 0),
			2 => new Vector3I(0, 0, -1),
			3 => new Vector3I(-1, 0, 0),
			_ => new Vector3I(0, 0, 1)
		};
	}

	private bool IsOpenSideFacingPlayer(Vector3I anchorCell, int rotation, Vector3 playerWorldPosition)
	{
		Vector3 structureCenter = new Vector3(anchorCell.X + 1.0f, anchorCell.Y, anchorCell.Z + 1.0f);

		Vector3 toPlayer = playerWorldPosition - structureCenter;
		toPlayer.Y = 0f;

		if (toPlayer.LengthSquared() < 0.001f)
			return true;

		toPlayer = toPlayer.Normalized();

		Vector3I openDirCell = GetOpenSideDirectionForRotation(rotation);
		Vector3 openDir = new Vector3(openDirCell.X, 0f, openDirCell.Z).Normalized();

		return openDir.Dot(toPlayer) > 0.5f;
	}

	public bool TryCraftAtAnchor(
		Vector3I anchorCell,
		Dictionary<Vector3I, WorldPlacedPiece> placedPieces,
		out WorldStructureRecipe matchedRecipe,
		out Basis spawnBasis,
		out List<WorldPlacedPiece> matchedPieces)
	{
		matchedRecipe = null;
		spawnBasis = Basis.Identity;
		matchedPieces = new List<WorldPlacedPiece>();

		foreach (var recipe in _recipes)
		{
			for (int rotation = 0; rotation < 4; rotation++)
			{
				if (TryMatchRecipeAtRotation(recipe, anchorCell, placedPieces, rotation, out matchedPieces))
				{
					matchedRecipe = recipe;
					spawnBasis = Basis.Identity; // don't trust rotation here for the bench
					return true;
				}
			}
		}

		return false;
	}

	private bool TryMatchRecipeAtRotation(
		WorldStructureRecipe recipe,
		Vector3I anchorCell,
		Dictionary<Vector3I, WorldPlacedPiece> placedPieces,
		int rotation,
		out List<WorldPlacedPiece> matchedPieces)
	{
		matchedPieces = new List<WorldPlacedPiece>();

		var layers = new[]
		{
			recipe.Layer0,
			recipe.Layer1
		};

		for (int y = 0; y < layers.Length; y++)
		{
			var layer = layers[y];
			if (layer == null || layer.Count == 0)
				continue;

			for (int z = 0; z < layer.Count; z++)
			{
				string row = layer[z];

				for (int x = 0; x < row.Length; x++)
				{
					char symbol = row[x];

					Vector3I local = new Vector3I(x, y, z);
					Vector3I rotated = RotateCell(local, rotation, row.Length, layer.Count);
					Vector3I worldCell = anchorCell + rotated;

					if (symbol == '.')
					{
						if (placedPieces.ContainsKey(worldCell))
							return false;

						continue;
					}

					string key = symbol.ToString();
					if (!recipe.KeyMap.ContainsKey(key))
						return false;

					if (!placedPieces.TryGetValue(worldCell, out var piece))
						return false;

					string expectedItemId = recipe.KeyMap[key].ToString();
					if (piece.ItemId != expectedItemId)
						return false;

					matchedPieces.Add(piece);
				}
			}
		}

		return matchedPieces.Count > 0;
	}

	private Vector3I RotateCell(Vector3I cell, int rotation, int width, int depth)
	{
		return rotation switch
		{
			0 => new Vector3I(cell.X, cell.Y, cell.Z),
			1 => new Vector3I(depth - 1 - cell.Z, cell.Y, cell.X),
			2 => new Vector3I(width - 1 - cell.X, cell.Y, depth - 1 - cell.Z),
			3 => new Vector3I(cell.Z, cell.Y, width - 1 - cell.X),
			_ => cell
		};
	}
}
