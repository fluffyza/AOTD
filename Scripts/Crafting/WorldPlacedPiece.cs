using Godot;

public partial class WorldPlacedPiece : Node3D
{
	[Export] public string ItemId = "";
	public Vector3I GridCell;
}
