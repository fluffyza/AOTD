using Godot;

[GlobalClass]
public partial class ItemDefinition : Resource
{
	[Export] public string ItemId = "";
	[Export] public string DisplayName = "";
	[Export] public bool IsPlaceable = true;
	[Export] public int MaxStackSize = 99;

	[Export] public PackedScene WorldScene;
	[Export] public bool IsBlock = false;

	[Export] public bool HasBlockColor = false;
	[Export] public Color BlockColor = Colors.White;

	[Export] public Texture2D TopTexture;
	[Export] public Texture2D SideTexture;
	[Export] public Texture2D BottomTexture;

	public bool HasBlockTextures =>
		TopTexture != null || SideTexture != null || BottomTexture != null;

	public ItemDefinition() { }

	public ItemDefinition(
		string itemId,
		string displayName,
		bool isPlaceable = true,
		int maxStackSize = 99,
		PackedScene worldScene = null,
		bool isBlock = false,
		bool hasBlockColor = false,
		Color? blockColor = null,
		Texture2D topTexture = null,
		Texture2D sideTexture = null,
		Texture2D bottomTexture = null)
	{
		ItemId = itemId;
		DisplayName = displayName;
		IsPlaceable = isPlaceable;
		MaxStackSize = maxStackSize;
		WorldScene = worldScene;
		IsBlock = isBlock;
		HasBlockColor = hasBlockColor;
		BlockColor = blockColor ?? Colors.White;

		TopTexture = topTexture;
		SideTexture = sideTexture;
		BottomTexture = bottomTexture;
	}
}
