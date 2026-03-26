using Godot;
using System.Collections.Generic;

public partial class WorldManager : Node
{
	[Export] public NodePath BlockManagerPath;
	[Export] public PackedScene TreeScene;
	[Export] public float TreeSpawnChance = 0.12f;
	
	private WorldCraftingManager _worldCraftingManager;
	private BlockManager _blockManager;

	private readonly Dictionary<Vector3I, Node3D> _placedItems = new();

	public override void _Ready()
	{
		_blockManager = GetNode<BlockManager>(BlockManagerPath);
		_worldCraftingManager = GetNode<WorldCraftingManager>("../WorldCraftingManager");
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

				Vector3 worldPos = GridUtils.CellToWorld(cell);

				var block = _blockManager.CreateSurfaceBlock(worldPos);
				if (block == null)
					continue;

				AddPlacedNode(cell, block);
				TrySpawnTree(worldPos, x, z);
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

		// Register crafting piece data if this placed object supports it.
		if (item is WorldPlacedPiece placedPiece)
		{
			placedPiece.ItemId = itemId;
			placedPiece.GridCell = cell;
			GD.Print($"Registered world piece: {placedPiece.ItemId} at {placedPiece.GridCell}");
		}
		else
		{
			GD.Print($"Placed item is NOT a WorldPlacedPiece: {item.Name}");
		}

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
	
	private void TrySpawnTree(Vector3 worldPosition, int x, int z)
	{
		if (TreeScene == null)
			return;

		if (GD.Randf() > TreeSpawnChance)
			return;

		var tree = TreeScene.Instantiate<Node3D>();
		AddChild(tree);

		tree.GlobalPosition = worldPosition + new Vector3(0, 1.0f, 0);
	}
	
	public Dictionary<Vector3I, WorldPlacedPiece> GetPlacedCraftingPieces()
	{
		var result = new Dictionary<Vector3I, WorldPlacedPiece>();

		foreach (var kvp in _placedItems)
		{
			if (kvp.Value is WorldPlacedPiece placedPiece)
				result[kvp.Key] = placedPiece;
		}

		return result;
	}
	
	private static readonly Vector3I[] CraftAnchorOffsets =
	{
		new Vector3I(0, 0, 0),
		new Vector3I(-1, 0, 0),
		new Vector3I(0, 0, -1),
		new Vector3I(-1, 0, -1),
		new Vector3I(0, -1, 0),
		new Vector3I(-1, -1, 0),
		new Vector3I(0, -1, -1),
		new Vector3I(-1, -1, -1)
	};

	public bool TryCraftWorldStructureFromCell(Vector3I lookedCell)
	{
		if (_worldCraftingManager == null)
			return false;

		var craftingPieces = GetPlacedCraftingPieces();

		foreach (var offset in CraftAnchorOffsets)
		{
			var anchorCell = lookedCell + offset;

			if (_worldCraftingManager.TryCraftAtAnchor(
				anchorCell,
				craftingPieces,
				out WorldStructureRecipe matchedRecipe,
				out Basis spawnBasis,
				out var matchedPieces))
			{
				return CompleteWorldCraft(anchorCell, matchedRecipe, spawnBasis, matchedPieces);
			}
		}

		return false;
	}


	private bool CompleteWorldCraft(
		Vector3I anchorCell,
		WorldStructureRecipe recipe,
		Basis spawnBasis,
		List<WorldPlacedPiece> matchedPieces)
	{
		foreach (var piece in matchedPieces)
		{
			_placedItems.Remove(piece.GridCell);
			piece.QueueFree();
		}

		if (recipe == null || recipe.OutputScene == null)
		{
			GD.PrintErr("WorldManager: Recipe or OutputScene missing.");
			return false;
		}

		var instance = recipe.OutputScene.Instantiate<Node3D>();
		if (instance == null)
		{
			GD.PrintErr("WorldManager: Failed to instantiate output scene.");
			return false;
		}

		instance.Position = GridUtils.CellToWorld(anchorCell);
		instance.Basis = spawnBasis;

		AddPlacedNode(anchorCell, instance);
		GD.Print($"World crafted: {recipe.RecipeId}");
		return true;
	}


}
