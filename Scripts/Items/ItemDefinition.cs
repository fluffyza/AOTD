using Godot;

[GlobalClass]
public partial class ItemDefinition : Resource
{
	[Export] public string ItemId = "";
	[Export] public string DisplayName = "";
	[Export] public Texture2D Icon;

	[Export] public int MaxStackSize = 99;

	[Export] public bool IsBlock = false;
	[Export] public bool CanPlaceInWorld = false;

	[Export] public PackedScene WorldScene;
	[Export] public bool CanBeFuel = false;
	[Export] public float FuelBurnTimeSeconds = 0f;

	[Export] public Texture2D TopTexture;
	[Export] public Texture2D SideTexture;
	[Export] public Texture2D BottomTexture;
	[Export] public Color BlockColor = Colors.White;
	[Export] public bool UseBlockColor = false;
	public bool HasBlockTextures =>
		TopTexture != null || SideTexture != null || BottomTexture != null;

	public bool HasBlockColor => UseBlockColor;
}
