using Godot;

public static class GridUtils
{
	public static Vector3I WorldToCell(Vector3 worldPos)
	{
		return new Vector3I(
			Mathf.FloorToInt(worldPos.X),
			Mathf.FloorToInt(worldPos.Y),
			Mathf.FloorToInt(worldPos.Z)
		);
	}

	public static Vector3 CellToWorld(Vector3I cell)
	{
		return new Vector3(
			cell.X + 0.5f,
			cell.Y + 0.5f,
			cell.Z + 0.5f
		);
	}
}
