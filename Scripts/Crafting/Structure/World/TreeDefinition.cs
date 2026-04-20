using Godot;

[GlobalClass]
public partial class TreeDefinition : Resource
{
	[Export] public string TreeId = "";
	[Export] public string DisplayName = "";

	[ExportGroup("Visual")]
	[Export] public Texture2D SpriteTexture;
	[Export] public Vector3 SpriteScale = Vector3.One;
	[Export] public Color NormalTint = Colors.White;
	[Export] public Color HighlightTint = new Color(1f, 0f, 0f, 1f);

	[ExportGroup("Harvest")]
	[Export] public int MaxHealth = 3;

	[Export] public string PrimaryDropItemId = "wood";
	[Export] public int MinPrimaryDrop = 5;
	[Export] public int MaxPrimaryDrop = 8;

	[Export] public string SecondaryDropItemId = "acorn";
	[Export] public float SecondaryDropChance = 0.25f;
	[Export] public int SecondaryDropAmount = 1;
}
