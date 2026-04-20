using Godot;
using System.Collections.Generic;

public partial class ItemDatabase : Node
{
	[Export] public Godot.Collections.Array<ItemDefinition> Items = new();

	private readonly Dictionary<string, ItemDefinition> _itemsById = new();

	public override void _Ready()
	{
		_itemsById.Clear();

		foreach (var item in Items)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
				continue;

			string id = item.ItemId.Trim().ToLower();

			if (_itemsById.ContainsKey(id))
			{
				GD.PrintErr($"Duplicate item id in ItemDatabase: {id}");
				continue;
			}

			_itemsById[id] = item;
		}
	}

	public ItemDefinition GetItem(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return null;

		itemId = itemId.Trim().ToLower();
		return _itemsById.TryGetValue(itemId, out var item) ? item : null;
	}

	public IEnumerable<ItemDefinition> GetAllItems()
	{
		return _itemsById.Values;
	}
}
