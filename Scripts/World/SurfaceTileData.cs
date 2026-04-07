using Godot;

public partial class SurfaceTileData : Resource
{
	[Export] public int X { get; set; }
	[Export] public int Z { get; set; }

	[Export] public float HeightOffset { get; set; } = 0f;
	[Export] public float TiltXDegrees { get; set; } = 0f;
	[Export] public float TiltZDegrees { get; set; } = 0f;

	[Export] public string TileType { get; set; } = "grass";
	[Export] public bool Buildable { get; set; } = true;
}
