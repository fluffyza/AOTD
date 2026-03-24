using Godot;

public partial class MineManager : Node
{
	[Export] public NodePath WorldManagerPath;
	[Export] public NodePath BlockManagerPath;

	private WorldManager _worldManager;
	private BlockManager _blockManager;

	public override void _Ready()
	{
		_worldManager = GetNode<WorldManager>(WorldManagerPath);
		_blockManager = GetNode<BlockManager>(BlockManagerPath);
	}

	public void GenerateMine(Vector3I entranceCenter)
	{
		int halfEntrance = 1; // 3x3
		int roomHalf = 4;     // 9x9x9

		Vector3I roomCenter = entranceCenter + new Vector3I(0, -4, 0);

		for (int x = -roomHalf; x <= roomHalf; x++)
		{
			for (int y = -roomHalf; y <= roomHalf; y++)
			{
				for (int z = -roomHalf; z <= roomHalf; z++)
				{
					Vector3I cell = roomCenter + new Vector3I(x, y, z);

					bool isBoundary =
						x == -roomHalf || x == roomHalf ||
						y == -roomHalf || y == roomHalf ||
						z == -roomHalf || z == roomHalf;

					PlaceMineBlock(cell, isBoundary);
				}
			}
		}

		for (int x = -halfEntrance; x <= halfEntrance; x++)
		{
			for (int z = -halfEntrance; z <= halfEntrance; z++)
			{
				Vector3I cell = entranceCenter + new Vector3I(x, 0, z);

				_worldManager.RemoveBlockIfExists(cell);
				PlaceMineBlock(cell, false);
			}
		}
	}

	private void PlaceMineBlock(Vector3I cell, bool unbreakable)
	{
		if (_worldManager.HasBlock(cell))
			return;

		var block = _blockManager.CreateMineBlock(
			GridUtils.CellToWorld(cell),
			unbreakable
		);

		if (block == null)
			return;

		_worldManager.AddPlacedNode(cell, block);
	}
}
