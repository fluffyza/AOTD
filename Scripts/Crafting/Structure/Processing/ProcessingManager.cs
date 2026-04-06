using Godot;
using System.Collections.Generic;

public partial class ProcessingManager : Node
{
	private ItemDatabase _itemDatabase;
	private readonly List<ProcessingRecipe> _recipes = new();

	private readonly Dictionary<string, float> _fuelTimes = new();

	public override void _Ready()
	{
		_itemDatabase = GetNodeOrNull<ItemDatabase>("/root/ItemDatabase");

		if (_itemDatabase == null)
		{
			GD.PrintErr("ProcessingManager: ItemDatabase autoload not found.");
			return;
		}

		BuildStarterRecipes();
		BuildFuelTable();
	}

	private void BuildStarterRecipes()
	{
		_recipes.Clear();

		_recipes.Add(new ProcessingRecipe
		{
			StationType = "Furnace",
			InputItemId = "iron_ore",
			OutputItemId = "iron_ingot",
			ProcessTimeSeconds = 5.0f
		});

		// Keep this ready for later when you add food.
		// Example:
		// _recipes.Add(new ProcessingRecipe
		// {
		//     StationType = "Campfire",
		//     InputItemId = "raw_meat",
		//     OutputItemId = "cooked_meat",
		//     ProcessTimeSeconds = 5.0f
		// });
	}

	private void BuildFuelTable()
	{
		_fuelTimes.Clear();

		_fuelTimes["stick"] = 2.0f;
		_fuelTimes["wood"] = 5.0f;
		_fuelTimes["coal"] = 12.0f;
	}

	public bool TryGetRecipe(string stationType, string inputItemId, out ProcessingRecipe recipe)
	{
		recipe = null;

		if (string.IsNullOrWhiteSpace(stationType) || string.IsNullOrWhiteSpace(inputItemId))
			return false;

		foreach (var candidate in _recipes)
		{
			if (candidate == null)
				continue;

			if (candidate.StationType == stationType && candidate.InputItemId == inputItemId)
			{
				recipe = candidate;
				return true;
			}
		}

		return false;
	}

	public bool IsValidFuel(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return false;

		return _fuelTimes.ContainsKey(itemId.Trim().ToLower());
	}

	public float GetFuelBurnTime(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return 0f;

		itemId = itemId.Trim().ToLower();
		return _fuelTimes.TryGetValue(itemId, out float burnTime) ? burnTime : 0f;
	}

	public bool CanProcessAtStation(string stationType, string itemId)
	{
		return TryGetRecipe(stationType, itemId, out _);
	}

	public ItemDefinition GetOutputItem(string outputItemId)
	{
		return _itemDatabase?.GetItem(outputItemId);
	}
}
