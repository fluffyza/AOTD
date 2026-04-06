using Godot;
using System.Collections.Generic;

public partial class CraftedStructure : WorldPlacedPiece
{
	public Dictionary<Vector3I, string> SourceRecipePieces { get; set; } = new();
	public Vector3I PlacedCell { get; set; } = Vector3I.Zero;

	private MeshHighlightController _highlightController;

	public override void _Ready()
	{
		_highlightController = GetNodeOrNull<MeshHighlightController>("MeshHighlightController");
	}

	public virtual void SetHighlighted(bool highlighted)
	{
		_highlightController?.SetHighlighted(highlighted);
	}
}
