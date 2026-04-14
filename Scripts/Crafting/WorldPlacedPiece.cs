using Godot;

public partial class WorldPlacedPiece : Node3D
{
	[Export] public string ItemId = "";
	public Vector3I GridCell;

	public int PlacementOrder = -1;

	// Store the player's flattened cardinal forward when this piece was placed.
	public Vector3 PlacementForward = Vector3.Forward;
}
