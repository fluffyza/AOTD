using Godot;
using System.Collections.Generic;

public partial class StorageContainer : Node
{
	[Signal]
	public delegate void StorageChangedEventHandler();

	[Export] public int Size = 3;

	private readonly List<InventorySlot> _slots = new();

	public override void _Ready()
	{
		while (_slots.Count < Size)
			_slots.Add(new InventorySlot());
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

	public bool AddItem(ItemDefinition item, int count)
	{
		if (item == null || count <= 0)
			return false;

		for (int i = 0; i < _slots.Count; i++)
		{
			var slot = _slots[i];
			if (!slot.IsEmpty && slot.CanStackWith(item))
			{
				int maxStack = slot.Item.MaxStackSize;
				int spaceLeft = maxStack - slot.Count;
				int toMove = Mathf.Min(spaceLeft, count);

				if (toMove > 0)
				{
					slot.Count += toMove;
					count -= toMove;
				}

				if (count <= 0)
				{
					EmitSignal(SignalName.StorageChanged);
					return true;
				}
			}
		}

		for (int i = 0; i < _slots.Count; i++)
		{
			var slot = _slots[i];
			if (slot.IsEmpty)
			{
				int toPlace = Mathf.Min(item.MaxStackSize, count);
				slot.SetItem(item, toPlace);
				count -= toPlace;

				if (count <= 0)
				{
					EmitSignal(SignalName.StorageChanged);
					return true;
				}
			}
		}

		EmitSignal(SignalName.StorageChanged);
		return count <= 0;
	}
}
