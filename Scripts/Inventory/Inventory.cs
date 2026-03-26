using Godot;
using System.Collections.Generic;

public partial class Inventory : Node
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	public const int HotbarSize = 9;
	public const int BackpackColumns = 8;
	public const int BackpackRows = 2;
	public const int BackpackSize = BackpackColumns * BackpackRows;
	public const int TotalSize = HotbarSize + BackpackSize;
	private ItemDatabase _itemDatabase;
	private readonly List<InventorySlot> _slots = new();

	public int SelectedIndex { get; private set; } = 0;

	public override void _Ready()
	{
		for (int i = 0; i < TotalSize; i++)
			_slots.Add(new InventorySlot());

		_itemDatabase = GetNodeOrNull<ItemDatabase>("/root/ItemDatabase");

		if (_itemDatabase == null)
			GD.PrintErr("Inventory: ItemDatabase autoload not found.");
	}

	public InventorySlot GetSlot(int index)
	{
		if (index < 0 || index >= _slots.Count)
			return null;

		return _slots[index];
	}

	public List<InventorySlot> GetAllSlots()
	{
		return _slots;
	}

	public InventorySlot GetSelectedSlot()
	{
		return GetSlot(SelectedIndex);
	}

	public void SelectSlot(int index)
	{
		if (index < 0 || index >= HotbarSize)
			return;

		SelectedIndex = index;
		EmitSignal(SignalName.InventoryChanged);
	}

	public void CycleSelection(int direction)
	{
		SelectedIndex = (SelectedIndex + direction + HotbarSize) % HotbarSize;
		EmitSignal(SignalName.InventoryChanged);
	}

	public string GetSelectedSlotLabel()
	{
		var slot = GetSelectedSlot();
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return "Empty";

		return $"{slot.Item.DisplayName} x{slot.Count}";
	}
	
	public bool AddItem(ItemDefinition item, int count)
	{
		if (item == null || count <= 0)
			return false;

		for (int i = 0; i < _slots.Count; i++)
		{
			if (!_slots[i].IsEmpty &&
				_slots[i].Item != null &&
				_slots[i].Item.ItemId == item.ItemId)
			{
				_slots[i].Count += count;
				EmitSignal(SignalName.InventoryChanged);
				return true;
			}
		}

		for (int i = 0; i < _slots.Count; i++)
		{
			if (_slots[i].IsEmpty)
			{
				_slots[i].SetItem(item, count);
				EmitSignal(SignalName.InventoryChanged);
				return true;
			}
		}

		return false;
	}

	public bool AddItem(string itemId, int count)
	{
		itemId = NormalizeItemId(itemId);

		if (string.IsNullOrEmpty(itemId) || count <= 0)
			return false;

		var item = _itemDatabase?.GetItem(itemId);
		if (item == null)
		{
			GD.PrintErr($"Inventory: Could not add item '{itemId}' because it was not found in ItemDatabase.");
			return false;
		}

		return AddItem(item, count);
	}

	public bool ConsumeSelectedItem(int amount)
	{
		var slot = GetSelectedSlot();
		if (slot == null || slot.IsEmpty || amount <= 0)
			return false;

		slot.Count -= amount;

		if (slot.Count <= 0)
			slot.Clear();

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public void SwapSlots(int a, int b)
	{
		if (a < 0 || a >= _slots.Count || b < 0 || b >= _slots.Count || a == b)
			return;

		(_slots[a], _slots[b]) = (_slots[b], _slots[a]);
		EmitSignal(SignalName.InventoryChanged);
	}
	
	private string NormalizeItemId(string itemId)
	{
		return string.IsNullOrWhiteSpace(itemId)
			? ""
			: itemId.Trim().ToLower();
	}
	
}
