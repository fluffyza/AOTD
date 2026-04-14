using Godot;
using System.Collections.Generic;

public partial class WorldCraftingManager : Node
{
	private readonly List<WorldStructureRecipe> _recipes = new();

	public override void _Ready()
	{
		BuildWorldRecipes();
	}

	public bool TryGetPreviewCellsAtAnchor(
		Vector3I anchorCell,
		Dictionary<Vector3I, WorldPlacedPiece> placedPieces,
		out List<Vector3I> previewCells)
	{
		previewCells = new List<Vector3I>();

		foreach (var recipe in _recipes)
		{
			for (int rotation = 0; rotation < 4; rotation++)
			{
				if (TryMatchRecipeAtRotation(recipe, anchorCell, placedPieces, rotation, out var matchedPieces))
				{
					foreach (var piece in matchedPieces)
						previewCells.Add(piece.GridCell);

					return previewCells.Count > 0;
				}
			}
		}

		return false;
	}

	private void BuildWorldRecipes()
	{
		_recipes.Clear();

		_recipes.Add(new WorldStructureRecipe
		{
			RecipeId = "log_stone_to_workbench",
			OutputItemId = "workbench",
			OutputScene = GD.Load<PackedScene>("res://Scenes/Towers/placeable_workbench.tscn"),
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
			Layer2 = new Godot.Collections.Array<string>(),
			KeyMap = new Godot.Collections.Dictionary<string, string>
			{
				{ "W", "wood" },
				{ "S", "stone" }
			}
		});

		_recipes.Add(new WorldStructureRecipe
		{
			RecipeId = "campfire_recipe",
			OutputItemId = "campfire",
			OutputScene = GD.Load<PackedScene>("res://Scenes/Towers/placeable_campfire.tscn"),
			Layer0 = new Godot.Collections.Array<string>
			{
				"W"
			},
			Layer1 = new Godot.Collections.Array<string>
			{
				"C"
			},
			Layer2 = new Godot.Collections.Array<string>
			{
				"W"
			},
			KeyMap = new Godot.Collections.Dictionary<string, string>
			{
				{ "W", "wood" },
				{ "C", "coal" }
			}
		});

		_recipes.Add(new WorldStructureRecipe
		{
			RecipeId = "furnace_recipe",
			OutputItemId = "furnace",
			OutputScene = GD.Load<PackedScene>("res://Scenes/Towers/placeable_furnace.tscn"),
			Layer0 = new Godot.Collections.Array<string>
			{
				"SSS",
				"SCS",
				"SSS"
			},
			Layer1 = new Godot.Collections.Array<string>
			{
				"S.S",
				"...",
				"S.S"
			},
			Layer2 = new Godot.Collections.Array<string>
			{
				"SSS",
				"SSS",
				"SSS"
			},
			KeyMap = new Godot.Collections.Dictionary<string, string>
			{
				{ "S", "stone" },
				{ "C", "campfire" }
			}
		});
		
		_recipes.Add(new WorldStructureRecipe
		{
			RecipeId = "chest_recipe",
			OutputItemId = "chest",
			OutputScene = GD.Load<PackedScene>("res://Scenes/Towers/placeable_chest.tscn"),
			Layer0 = new Godot.Collections.Array<string>
			{
		        "WW"
			},
			Layer1 = new Godot.Collections.Array<string>
			{
		        "SS"
			},
			Layer2 = new Godot.Collections.Array<string>(),
			KeyMap =  new Godot.Collections.Dictionary<string, string>
			{
				{ "W", "wood" },
				{ "S", "stone" }
			}
		});
		
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
					spawnBasis = GetRecipeSpawnBasis(recipe, matchedPieces);
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
			recipe.Layer1,
			recipe.Layer2
		};

		for (int y = 0; y < layers.Length; y++)
		{
			var layer = layers[y];
			if (layer == null || layer.Count == 0)
				continue;

			int depth = layer.Count;
			int width = layer[0].Length;

			for (int z = 0; z < depth; z++)
			{
				string row = layer[z];

				for (int x = 0; x < row.Length; x++)
				{
					char symbol = row[x];

					Vector3I local = new Vector3I(x, y, z);
					Vector3I rotated = RotateCell(local, rotation, width, depth);
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
	
	private Basis GetRecipeSpawnBasis(
		WorldStructureRecipe recipe,
		List<WorldPlacedPiece> matchedPieces)
	{
		if (recipe == null || matchedPieces == null || matchedPieces.Count == 0)
			return Basis.Identity;

		if (recipe.OutputItemId != "chest")
			return Basis.Identity;

		WorldPlacedPiece firstPlaced = matchedPieces[0];

		foreach (var piece in matchedPieces)
		{
			if (piece.PlacementOrder < firstPlaced.PlacementOrder)
				firstPlaced = piece;
		}

		Vector3 facing = firstPlaced.PlacementForward;
		facing.Y = 0f;

		if (facing.LengthSquared() < 0.0001f)
			facing = Vector3.Forward;

		facing = facing.Normalized();

		// Chest should face opposite the player's facing when first block was placed.
		Vector3 chestForward = -facing;

		float yawDeg = 0f;

		if (chestForward == Vector3.Forward)
			yawDeg = 180f;
		else if (chestForward == Vector3.Back)
			yawDeg = 0f;
		else if (chestForward == Vector3.Right)
			yawDeg = 90f;
		else if (chestForward == Vector3.Left)
			yawDeg = -90f;

		return Basis.FromEuler(new Vector3(0f, Mathf.DegToRad(yawDeg), 0f));
	}

}
