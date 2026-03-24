using Godot;
using System.Collections.Generic;

public partial class WorldManager : Node
{
	[Export] public NodePath BlockManagerPath;

	private BlockManager _blockManager;

	private readonly Dictionary<Vector3I, Node3D> _placedItems = new();

	public override void _Ready()
	{
		_blockManager = GetNode<BlockManager>(BlockManagerPath);
	}

	public bool HasBlock(Vector3I cell)
	{
		return _placedItems.ContainsKey(cell);
	}

	public bool TryGetBlock(Vector3I cell, out Node3D block)
	{
		return _placedItems.TryGetValue(cell, out block);
	}

	public Dictionary<Vector3I, Node3D> GetPlacedItems()
	{
		return _placedItems;
	}

	public void AddPlacedNode(Vector3I cell, Node3D node)
	{
		if (node == null)
			return;

		_placedItems[cell] = node;
		GetTree().CurrentScene.AddChild(node);
	}

	public bool RemoveBlockIfExists(Vector3I cell)
	{
		if (_placedItems.TryGetValue(cell, out Node3D block))
		{
			_placedItems.Remove(cell);
			block.QueueFree();
			return true;
		}

		return false;
	}

	public Node3D GetPlacedBlockFromHit(Node collider)
	{
		Node current = collider;

		while (current != null)
		{
			foreach (var kvp in _placedItems)
			{
				if (kvp.Value == current)
					return kvp.Value;
			}

			current = current.GetParent();
		}

		return null;
	}

	public bool TryGetLookedAtPlacedItem(
		Godot.Collections.Dictionary result,
		out Vector3I cell,
		out Node3D item)
	{
		cell = default;
		item = null;

		Vector3 hitPosition = (Vector3)result["position"];
		Vector3 hitNormal = (Vector3)result["normal"];

		Vector3I lookedAtCell = GridUtils.WorldToCell(hitPosition - hitNormal * 0.01f);
		if (_placedItems.TryGetValue(lookedAtCell, out Node3D directItem))
		{
			cell = lookedAtCell;
			item = directItem;
			return true;
		}

		Node collider = result["collider"].As<Node>();
		Node3D hitItem = GetPlacedBlockFromHit(collider);
		if (hitItem != null)
		{
			cell = GridUtils.WorldToCell(hitItem.GlobalPosition);
			item = hitItem;
			return true;
		}

		return false;
	}

	public void GenerateSurfaceFloor(int halfSize, int yLevel)
	{
		for (int x = -halfSize; x <= halfSize; x++)
		{
			for (int z = -halfSize; z <= halfSize; z++)
			{
				Vector3I cell = new Vector3I(x, yLevel, z);

				if (_placedItems.ContainsKey(cell))
					continue;

				var block = _blockManager.CreateSurfaceBlock(GridUtils.CellToWorld(cell));
				if (block == null)
					continue;

				AddPlacedNode(cell, block);
			}
		}
	}

	public bool TryPlaceInventoryItem(Vector3I cell, string itemId)
	{
		if (_placedItems.ContainsKey(cell))
			return false;

		var item = _blockManager.CreatePlacedItem(itemId, GridUtils.CellToWorld(cell));
		if (item == null)
			return false;

		AddPlacedNode(cell, item);
		return true;
	}

	public bool TryRemoveBreakableBlock(Vector3I cell, out string droppedItemId)
	{
		droppedItemId = "";

		if (!_placedItems.TryGetValue(cell, out Node3D item))
			return false;

		if (item.HasMeta("unbreakable") && (bool)item.GetMeta("unbreakable"))
			return false;

		_placedItems.Remove(cell);
		droppedItemId = _blockManager.GetDroppedItemId(item);
		item.QueueFree();
		return true;
	}
}
