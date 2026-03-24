using Godot;

public partial class PlacementPreview : Node3D
{
	public void UpdateFromTarget(BlockTargetting targetting)
	{
		if (!targetting.HasHit)
		{
			Visible = false;
			return;
		}

		Visible = true;
		GlobalPosition = GridUtils.CellToWorld(targetting.RemoveCell) + new Vector3(0, 0.02f, 0);
		GlobalRotation = Vector3.Zero;
		Scale = new Vector3(1.08f, 1.08f, 1.08f);
	}
}
