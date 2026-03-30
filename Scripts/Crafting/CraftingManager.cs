using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CraftingManager : Node
{
	private ItemDatabase _itemDatabase;
	private readonly List<CraftingRecipe> _recipes = new();
	private int _lastShapeOffsetX = 0;
	private int _lastShapeOffsetY = 0;
	
	public override void _Ready()
	{
		_itemDatabase = GetNodeOrNull<ItemDatabase>("/root/ItemDatabase");

		if (_itemDatabase == null)
		{
			GD.PrintErr("CraftingManager: ItemDatabase autoload not found.");
			return;
		}

		BuildStarterRecipes();
	}

	private void BuildStarterRecipes()
	{
		_recipes.Clear();

		_recipes.Add(new CraftingRecipe
		{
			RecipeId = "wood_to_sticks",
			StationType = "Backpack",
			IsShapeless = true,
			OutputItemId = "stick",
			OutputAmount = 4,
			Ingredients = new Godot.Collections.Array<RecipeIngredient>
			{
				new RecipeIngredient
				{
					ItemId = "wood",
					Amount = 1
				}
			}
		});
		
		_recipes.Add(new CraftingRecipe
		{
			RecipeId = "wood_to_stick_workbench",
			StationType = "Workbench",
			IsShapeless = false,
			OutputItemId = "stick",
			OutputAmount = 4,
			PatternRows = new Godot.Collections.Array<string>
			{
				"W"
			},
			PatternKey = new Godot.Collections.Dictionary<string, string>
			{
				{ "W", "wood" }
			}
		});

		_recipes.Add(new CraftingRecipe
		{
			RecipeId = "coal_stick_to_torch",
			StationType = "Backpack",
			IsShapeless = false,
			OutputItemId = "torch",
			OutputAmount = 4,
			PatternRows = new Godot.Collections.Array<string>
			{
				"C",
				"S"
			},
			PatternKey = new Godot.Collections.Dictionary<string, string>
			{
				{ "C", "coal" },
				{ "S", "stick" }
			}
		});
		
		_recipes.Add(new CraftingRecipe
		{
			RecipeId = "coal_stick_to_torch_workbench",
			StationType = "Workbench",
			IsShapeless = false,
			OutputItemId = "torch",
			OutputAmount = 4,
			PatternRows = new Godot.Collections.Array<string>
			{
				"C",
				"S"
			},
			PatternKey = new Godot.Collections.Dictionary<string, string>
			{
				{ "C", "coal" },
				{ "S", "stick" }
			}
		});
		
		_recipes.Add(new CraftingRecipe
		{
			RecipeId = "stone_pickaxe",
			StationType = "Workbench",
			IsShapeless = false,
			OutputItemId = "pickaxe",
			OutputAmount = 1,
			PatternRows = new Godot.Collections.Array<string>
			{
				"SSS",
				".T.",
				".T."
			},
			PatternKey = new Godot.Collections.Dictionary<string, string>
			{
				{ "S", "stone" },
				{ "T", "stick" }
			}
		});


	}

	public bool TryGetCraftingResult(
		string stationType,
		InventorySlot[] inputSlots,
		out CraftingRecipe recipe,
		out int craftCount,
		out ItemDefinition outputItem,
		out int outputAmount)
	{
		recipe = null;
		craftCount = 0;
		outputItem = null;
		outputAmount = 0;

		if (inputSlots == null || inputSlots.Length == 0)
			return false;

		foreach (var candidate in _recipes)
		{
			if (candidate == null || candidate.StationType != stationType)
				continue;

			int matchedCraftCount;
			bool matched = candidate.IsShapeless
				? TryMatchShapelessRecipe(candidate, inputSlots, out matchedCraftCount)
				: TryMatchShapedRecipe(candidate, inputSlots, out matchedCraftCount);

			if (!matched)
				continue;

			var resultItem = _itemDatabase.GetItem(candidate.OutputItemId);
			if (resultItem == null)
			{
				GD.PrintErr($"CraftingManager: Output item '{candidate.OutputItemId}' not found.");
				continue;
			}

			recipe = candidate;
			craftCount = matchedCraftCount;
			outputItem = resultItem;
			outputAmount = candidate.OutputAmount * matchedCraftCount;
			return true;
		}

		return false;
	}

	public bool TryConsumeRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe == null || inputSlots == null || craftCount <= 0)
			return false;

		return recipe.IsShapeless
			? TryConsumeShapelessRecipeInputs(recipe, craftCount, inputSlots)
			: TryConsumeShapedRecipeInputs(recipe, craftCount, inputSlots);
	}

	private bool TryMatchShapelessRecipe(
		CraftingRecipe recipe,
		InventorySlot[] inputSlots,
		out int craftCount)
	{
		craftCount = 0;

		if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
			return false;

		var nonEmptySlots = inputSlots
			.Where(slot => slot != null && !slot.IsEmpty && slot.Item != null)
			.ToList();

		if (nonEmptySlots.Count == 0)
			return false;

		// Total counts by item id
		var totals = new Dictionary<string, int>();
		foreach (var slot in nonEmptySlots)
		{
			if (!totals.ContainsKey(slot.Item.ItemId))
				totals[slot.Item.ItemId] = 0;

			totals[slot.Item.ItemId] += slot.Count;
		}

		// Reject extra unrelated items
		if (totals.Keys.Count != recipe.Ingredients.Count)
			return false;

		foreach (var ingredient in recipe.Ingredients)
		{
			if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId) || ingredient.Amount <= 0)
				return false;

			if (!totals.ContainsKey(ingredient.ItemId))
				return false;
		}

		int maxCrafts = int.MaxValue;

		foreach (var ingredient in recipe.Ingredients)
		{
			int available = totals[ingredient.ItemId];
			int possible = available / ingredient.Amount;
			maxCrafts = Mathf.Min(maxCrafts, possible);
		}

		craftCount = maxCrafts;
		return craftCount > 0;
	}

	private bool TryConsumeShapelessRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe.Ingredients == null || recipe.Ingredients.Count == 0)
			return false;

		foreach (var ingredient in recipe.Ingredients)
		{
			int amountToConsume = ingredient.Amount * craftCount;

			for (int i = 0; i < inputSlots.Length && amountToConsume > 0; i++)
			{
				var slot = inputSlots[i];
				if (slot == null || slot.IsEmpty || slot.Item == null)
					continue;

				if (slot.Item.ItemId != ingredient.ItemId)
					continue;

				int taken = Mathf.Min(slot.Count, amountToConsume);
				slot.RemoveAmount(taken);
				amountToConsume -= taken;

				if (slot.IsEmpty)
					inputSlots[i] = new InventorySlot();
			}

			if (amountToConsume > 0)
				return false;
		}

		return true;
	}

	private bool TryMatchShapedRecipe(
		CraftingRecipe recipe,
		InventorySlot[] inputSlots,
		out int craftCount)
	{
		craftCount = 0;

		if (recipe.PatternRows == null || recipe.PatternRows.Count == 0)
			return false;

		if (!TryGetGridSize(inputSlots, out int gridWidth, out int gridHeight))
			return false;

		int recipeWidth = recipe.PatternRows[0].Length;
		int recipeHeight = recipe.PatternRows.Count;

		if (recipeWidth > gridWidth || recipeHeight > gridHeight)
			return false;

		for (int offsetY = 0; offsetY <= gridHeight - recipeHeight; offsetY++)
		{
			for (int offsetX = 0; offsetX <= gridWidth - recipeWidth; offsetX++)
			{
				if (TryMatchPatternAtOffset(
					recipe,
					inputSlots,
					gridWidth,
					gridHeight,
					offsetX,
					offsetY,
					out craftCount))
				{
					_lastShapeOffsetX = offsetX;
					_lastShapeOffsetY = offsetY;
					return true;
				}
			}
		}

		craftCount = 0;
		return false;
	}

	private bool TryMatchPatternAtOffset(
		CraftingRecipe recipe,
		InventorySlot[] inputSlots,
		int gridWidth,
		int gridHeight,
		int offsetX,
		int offsetY,
		out int craftCount)
	{
		craftCount = int.MaxValue;

		int recipeWidth = recipe.PatternRows[0].Length;
		int recipeHeight = recipe.PatternRows.Count;

		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				var slot = inputSlots[(y * gridWidth) + x];

				bool insidePattern =
					x >= offsetX &&
					x < offsetX + recipeWidth &&
					y >= offsetY &&
					y < offsetY + recipeHeight;

				if (!insidePattern)
				{
					if (!IsEmpty(slot))
						return false;

					continue;
				}

				char symbol = recipe.PatternRows[y - offsetY][x - offsetX];

				if (symbol == '.')
				{
					if (!IsEmpty(slot))
						return false;

					continue;
				}

				string key = symbol.ToString();
				if (!recipe.PatternKey.ContainsKey(key))
					return false;

				string expectedItemId = recipe.PatternKey[key].ToString();

				if (!IsItem(slot, expectedItemId))
					return false;

				craftCount = Mathf.Min(craftCount, slot.Count);
			}
		}

		if (craftCount == int.MaxValue)
			craftCount = 0;

		return craftCount > 0;
	}

	private bool TryConsumeShapedRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe.PatternRows == null || recipe.PatternRows.Count == 0)
			return false;

		if (!TryGetGridSize(inputSlots, out int gridWidth, out int gridHeight))
			return false;

		for (int y = 0; y < recipe.PatternRows.Count; y++)
		{
			string row = recipe.PatternRows[y];

			for (int x = 0; x < row.Length; x++)
			{
				char symbol = row[x];
				if (symbol == '.')
					continue;

				int gridX = _lastShapeOffsetX + x;
				int gridY = _lastShapeOffsetY + y;
				int index = (gridY * gridWidth) + gridX;

				var slot = inputSlots[index];
				if (slot == null || slot.IsEmpty)
					return false;

				slot.RemoveAmount(craftCount);

				if (slot.IsEmpty)
					inputSlots[index] = new InventorySlot();
			}
		}

		return true;
	}

	private bool IsEmpty(InventorySlot slot)
	{
		return slot == null || slot.IsEmpty || slot.Item == null;
	}

	private bool IsItem(InventorySlot slot, string itemId)
	{
		return slot != null &&
			   !slot.IsEmpty &&
			   slot.Item != null &&
			   slot.Item.ItemId == itemId;
	}
	
	private bool TryGetGridSize(InventorySlot[] inputSlots, out int gridWidth, out int gridHeight)
	{
		gridWidth = 0;
		gridHeight = 0;

		if (inputSlots == null)
			return false;

		if (inputSlots.Length == 4)
		{
			gridWidth = 2;
			gridHeight = 2;
			return true;
		}

		if (inputSlots.Length == 9)
		{
			gridWidth = 3;
			gridHeight = 3;
			return true;
		}

		GD.PrintErr($"CraftingManager: Unsupported crafting grid size: {inputSlots.Length}");
		return false;
	}


}
