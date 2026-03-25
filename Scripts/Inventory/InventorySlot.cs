using Godot;

public partial class InventorySlot : RefCounted
{
	public ItemDefinition Item { get; set; }
	public int Count { get; set; } = 0;

	public bool IsEmpty => Item == null || Count <= 0;

	public void SetItem(ItemDefinition item, int count)
	{
		Item = item;
		Count = count;
	}

	public void Clear()
	{
		Item = null;
		Count = 0;
	}

	public int RemoveAmount(int amount)
	{
		if (IsEmpty || amount <= 0)
			return 0;

		int removed = Mathf.Min(amount, Count);
		Count -= removed;

		if (Count <= 0)
			Clear();

		return removed;
	}

	public bool CanStackWith(ItemDefinition other)
	{
		return !IsEmpty &&
			   Item != null &&
			   other != null &&
			   Item.ItemId == other.ItemId;
	}
}
