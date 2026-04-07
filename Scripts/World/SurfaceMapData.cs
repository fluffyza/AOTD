using Godot;
using Godot.Collections;

public partial class SurfaceMapData : Resource
{
	[Export] public int YLevel { get; set; } = 0;
	[Export] public Array<SurfaceTileData> Tiles { get; set; } = new();
}
