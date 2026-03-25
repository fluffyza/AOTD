using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CraftingManager : Node
{
	private readonly List<CraftingRecipe> _recipes = new();

	public override void _Ready()
	{
		BuildStarterRecipes();
	}

	private void BuildStarterRecipes()
	{
		var woodToStick = new CraftingRecipe
		{
			RecipeId = "wood_to_sticks",
			StationType = "Backpack",
			IsShapeless = true,
			OutputItem = new ItemDefinition("stick", "Stick"),
			OutputAmount = 4,
			Inputs = new Godot.Collections.Array<RecipeIngredient>
			{
				new RecipeIngredient
				{
					Item = new ItemDefinition("wood", "Wood"),
					Amount = 1
				}
			}
		};

		_recipes.Add(woodToStick);
		
		var torchRecipe = new CraftingRecipe
		{
			RecipeId = "coal_stick_to_torch",
			StationType = "Backpack",
			IsShapeless = false,
			OutputItem = new ItemDefinition("torch", "Torch"),
			OutputAmount = 4,
			Inputs = new Godot.Collections.Array<RecipeIngredient>
			{
				new RecipeIngredient
				{
					Item = new ItemDefinition("coal", "Coal"),
					Amount = 1
				},
				new RecipeIngredient
				{
					Item = new ItemDefinition("stick", "Stick"),
					Amount = 1
				}
			}
		};

		_recipes.Add(torchRecipe);
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

		var nonEmptySlots = inputSlots
			.Where(slot => slot != null && !slot.IsEmpty)
			.ToList();

		if (nonEmptySlots.Count == 0)
			return false;

		foreach (var candidate in _recipes)
		{
			if (candidate.StationType != stationType)
				continue;

			bool matched = false;
			int matchedCraftCount = 0;

			if (candidate.IsShapeless)
				matched = TryMatchShapelessRecipe(candidate, nonEmptySlots, out matchedCraftCount);
			else
				matched = TryMatchShapedRecipe(candidate, inputSlots, out matchedCraftCount);

			if (matched)
			{
				recipe = candidate;
				craftCount = matchedCraftCount;
				outputItem = candidate.OutputItem;
				outputAmount = candidate.OutputAmount * matchedCraftCount;
				return true;
			}
		}

		return false;
	}

	private bool TryMatchShapelessRecipe(
		CraftingRecipe recipe,
		List<InventorySlot> nonEmptySlots,
		out int craftCount)
	{
		craftCount = 0;

		if (recipe.Inputs == null || recipe.Inputs.Count != 1)
			return false;

		var required = recipe.Inputs[0];
		if (required == null || required.Item == null || required.Amount <= 0)
			return false;

		// Wood -> sticks only works from one occupied slot / one stack
		if (nonEmptySlots.Count != 1)
			return false;

		var slot = nonEmptySlots[0];

		if (slot == null || slot.IsEmpty || slot.Item == null)
			return false;

		if (slot.Item.ItemId != required.Item.ItemId)
			return false;

		if (slot.Count < required.Amount)
			return false;

		craftCount = slot.Count / required.Amount;
		return craftCount > 0;
	}
	
	private bool TryConsumeShapedRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe == null || inputSlots == null || inputSlots.Length < 4)
			return false;

		switch (recipe.RecipeId)
		{
			case "coal_stick_to_torch":
				return TryConsumeTorchRecipeInputs(craftCount, inputSlots);
		}

		return false;
	}
	
	private bool TryConsumeShapelessRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe.Inputs == null || recipe.Inputs.Count != 1)
			return false;

		var required = recipe.Inputs[0];
		if (required == null || required.Item == null || required.Amount <= 0)
			return false;

		int amountToConsume = required.Amount * craftCount;

		int totalAvailable = inputSlots
			.Where(slot =>
				slot != null &&
				!slot.IsEmpty &&
				slot.Item != null &&
				slot.Item.ItemId == required.Item.ItemId)
			.Sum(slot => slot.Count);

		if (totalAvailable < amountToConsume)
			return false;

		for (int i = 0; i < inputSlots.Length && amountToConsume > 0; i++)
		{
			var slot = inputSlots[i];
			if (slot == null || slot.IsEmpty || slot.Item == null || slot.Item.ItemId != required.Item.ItemId)
				continue;

			int taken = Mathf.Min(slot.Count, amountToConsume);
			slot.RemoveAmount(taken);
			amountToConsume -= taken;

			if (slot.IsEmpty)
				inputSlots[i] = new InventorySlot();
		}

		return true;
	}

	public bool TryConsumeRecipeInputs(
		CraftingRecipe recipe,
		int craftCount,
		InventorySlot[] inputSlots)
	{
		if (recipe == null || inputSlots == null || craftCount <= 0)
			return false;

		if (recipe.IsShapeless)
			return TryConsumeShapelessRecipeInputs(recipe, craftCount, inputSlots);

		return TryConsumeShapedRecipeInputs(recipe, craftCount, inputSlots);
	}
	
	private bool TryMatchShapedRecipe(
		CraftingRecipe recipe,
		InventorySlot[] inputSlots,
		out int craftCount)
	{
		craftCount = 0;

		if (recipe == null || inputSlots == null || inputSlots.Length < 4)
			return false;

		switch (recipe.RecipeId)
		{
			case "coal_stick_to_torch":
				return TryMatchTorchRecipe(inputSlots, out craftCount);
		}

		return false;
	}
	
	private bool TryMatchTorchRecipe(InventorySlot[] inputSlots, out int craftCount)
	{
		craftCount = 0;

		if (inputSlots == null || inputSlots.Length < 4)
			return false;

		var topLeft = inputSlots[0];
		var topRight = inputSlots[1];
		var bottomLeft = inputSlots[2];
		var bottomRight = inputSlots[3];

		bool leftColumnMatch =
			IsItem(topLeft, "coal") &&
			IsItem(bottomLeft, "stick") &&
			IsEmpty(topRight) &&
			IsEmpty(bottomRight);

		bool rightColumnMatch =
			IsItem(topRight, "coal") &&
			IsItem(bottomRight, "stick") &&
			IsEmpty(topLeft) &&
			IsEmpty(bottomLeft);

		if (!leftColumnMatch && !rightColumnMatch)
			return false;

		if (leftColumnMatch)
		{
			craftCount = Mathf.Min(topLeft.Count, bottomLeft.Count);
			return craftCount > 0;
		}

		if (rightColumnMatch)
		{
			craftCount = Mathf.Min(topRight.Count, bottomRight.Count);
			return craftCount > 0;
		}

		return false;
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
	
	private bool TryConsumeTorchRecipeInputs(int craftCount, InventorySlot[] inputSlots)
	{
		var topLeft = inputSlots[0];
		var topRight = inputSlots[1];
		var bottomLeft = inputSlots[2];
		var bottomRight = inputSlots[3];

		bool leftColumnMatch =
			IsItem(topLeft, "coal") &&
			IsItem(bottomLeft, "stick") &&
			IsEmpty(topRight) &&
			IsEmpty(bottomRight);

		bool rightColumnMatch =
			IsItem(topRight, "coal") &&
			IsItem(bottomRight, "stick") &&
			IsEmpty(topLeft) &&
			IsEmpty(bottomLeft);

		if (leftColumnMatch)
		{
			topLeft.RemoveAmount(craftCount);
			bottomLeft.RemoveAmount(craftCount);

			if (topLeft.IsEmpty) inputSlots[0] = new InventorySlot();
			if (bottomLeft.IsEmpty) inputSlots[2] = new InventorySlot();

			return true;
		}

		if (rightColumnMatch)
		{
			topRight.RemoveAmount(craftCount);
			bottomRight.RemoveAmount(craftCount);

			if (topRight.IsEmpty) inputSlots[1] = new InventorySlot();
			if (bottomRight.IsEmpty) inputSlots[3] = new InventorySlot();

			return true;
		}

		return false;
	}
	
}
