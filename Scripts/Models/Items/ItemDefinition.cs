public class ItemDefinition
{
	public string ItemId { get; set; }
	public string DisplayName { get; set; }

	public ItemDefinition(string itemId, string displayName)
	{
		ItemId = itemId;
		DisplayName = displayName;
	}
}
