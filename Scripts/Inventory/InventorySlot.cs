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

}
