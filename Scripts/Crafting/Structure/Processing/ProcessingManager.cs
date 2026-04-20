using Godot;
using System.Collections.Generic;

public partial class ProcessingManager : Node
{
	[Export] public Godot.Collections.Array<ProcessingRecipe> Recipes = new();

	private ItemDatabase _itemDatabase;
	private readonly List<ProcessingRecipe> _recipes = new();

	public override void _Ready()
	{
		_itemDatabase = GetNodeOrNull<ItemDatabase>("/root/ItemDatabase");

		if (_itemDatabase == null)
		{
			GD.PrintErr("ProcessingManager: ItemDatabase autoload not found.");
			return;
		}

		_recipes.Clear();

		foreach (var recipe in Recipes)
		{
			if (recipe == null)
				continue;

			_recipes.Add(recipe);
		}
	}

	public bool TryGetRecipe(
		ProcessingRecipe.ProcessingStationType stationType,
		string inputItemId,
		out ProcessingRecipe recipe)
	{
		recipe = null;

		if (string.IsNullOrWhiteSpace(inputItemId))
			return false;

		inputItemId = inputItemId.Trim().ToLower();

		foreach (var candidate in _recipes)
		{
			if (candidate == null)
				continue;

			if (candidate.StationType == stationType &&
				candidate.InputItemId == inputItemId)
			{
				recipe = candidate;
				return true;
			}
		}

		return false;
	}

	public bool IsValidFuel(string itemId)
	{
		var item = _itemDatabase?.GetItem(itemId);
		return item != null && item.CanBeFuel && item.FuelBurnTimeSeconds > 0f;
	}

	public float GetFuelBurnTime(string itemId)
	{
		var item = _itemDatabase?.GetItem(itemId);
		if (item == null || !item.CanBeFuel)
			return 0f;

		return item.FuelBurnTimeSeconds;
	}

	public bool CanProcessAtStation(
		ProcessingRecipe.ProcessingStationType stationType,
		string itemId)
	{
		return TryGetRecipe(stationType, itemId, out _);
	}

	public ItemDefinition GetOutputItem(string outputItemId)
	{
		return _itemDatabase?.GetItem(outputItemId);
	}
}
