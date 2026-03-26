using Godot;
using System.Collections.Generic;

public partial class ItemDatabase : Node
{
	[Export] public PackedScene BlockScene;
	[Export] public PackedScene TorchScene;

	private readonly Dictionary<string, ItemDefinition> _items = new();

	public override void _Ready()
	{
		RegisterStarterItems();
	}

	private void RegisterStarterItems()
	{
		Register(new ItemDefinition("wood", "Wood", true, 99, BlockScene, true, true, new Color(0.45f, 0.28f, 0.14f)));
		Register(new ItemDefinition("coal", "Coal", true, 99, BlockScene, true, true, new Color(0.1f, 0.1f, 0.1f)));
		Register(new ItemDefinition("stone", "Stone", true, 99, BlockScene, true, true, new Color(0.85f, 0.85f, 0.85f)));
		Register(new ItemDefinition("dirt", "Dirt", true, 99, BlockScene, true, true, new Color(0.45f, 0.28f, 0.14f)));
		Register(new ItemDefinition("stick", "Stick", false, 99, null, false, false, null));
		Register(new ItemDefinition("torch", "Torch", true, 99, TorchScene, false, false, null));
		Register(new ItemDefinition("acorn", "Acorn", false, 99, null, false, false, null));
	}

	private void Register(ItemDefinition item)
	{
		if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
		{
			GD.PrintErr("ItemDatabase: Tried to register null or invalid item.");
			return;
		}

		string key = NormalizeItemId(item.ItemId);

		if (_items.ContainsKey(key))
		{
			GD.PrintErr($"ItemDatabase: Duplicate item id '{key}'");
			return;
		}

		_items[key] = item;
	}

	public ItemDefinition GetItem(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return null;

		string key = NormalizeItemId(itemId);

		if (_items.TryGetValue(key, out var item))
			return item;

		GD.PrintErr($"ItemDatabase: Item '{itemId}' was not found.");
		return null;
	}

	public bool HasItem(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return false;

		return _items.ContainsKey(NormalizeItemId(itemId));
	}

	private string NormalizeItemId(string itemId)
	{
		return string.IsNullOrWhiteSpace(itemId)
			? ""
			: itemId.Trim().ToLower();
	}
}
