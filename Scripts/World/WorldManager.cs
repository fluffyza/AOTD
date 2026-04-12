using Godot;
using System.Collections.Generic;

public partial class WorldManager : Node
{
	[Export] public NodePath BlockManagerPath;
	[Export] public PackedScene TreeScene;
	[Export] public float TreeSpawnChance = 0.12f;//0f;
	[Export] public PackedScene GrassClumpScene;
	[Export] public PackedScene RockOneScene;
	[Export] public PackedScene RockTwoScene;
	[Export] public PackedScene RockThreeScene;

	[Export] public float GrassSpawnChance = 0.35f;
	[Export] public float RockSpawnChance = 0.08f;
	
	[Export] public PackedScene GrassTileScene;
	
	
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
		if (GrassTileScene == null)
		{
			GD.PrintErr("GrassTileScene not assigned.");
			return;
		}

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int x = -halfSize; x <= halfSize; x++)
		{
			for (int z = -halfSize; z <= halfSize; z++)
			{
				Vector3I cell = new Vector3I(x, yLevel, z);

				if (_placedItems.ContainsKey(cell))
					continue;

				Vector3 worldPos = GridUtils.CellToWorld(cell);

				var tile = GrassTileScene.Instantiate<Node3D>();

				// Slight variation
				float heightOffset = rng.RandfRange(-0.03f, 0.03f);
				float tiltX = rng.RandfRange(-1.5f, 1.5f);
				float tiltZ = rng.RandfRange(-1.5f, 1.5f);

				tile.Position = worldPos + new Vector3(0, heightOffset, 0);
				tile.RotationDegrees = new Vector3(tiltX, 0, tiltZ);

				tile.SetMeta("unbreakable", true);
				tile.SetMeta("is_ground_tile", true);

				AddPlacedNode(cell, tile);
				
				var decorAnchor = tile.GetNodeOrNull<Marker3D>("DecorAnchor");
				Vector3 decorPos = decorAnchor != null ? decorAnchor.GlobalPosition : tile.GlobalPosition;

				TrySpawnTree(decorPos, x, z);
				TrySpawnGrass(decorPos, x, z);
				TrySpawnRock(decorPos, x, z);
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
		new Vector3I(-2, 0, 0),

		new Vector3I(0, 0, -1),
		new Vector3I(-1, 0, -1),
		new Vector3I(-2, 0, -1),

		new Vector3I(0, 0, -2),
		new Vector3I(-1, 0, -2),
		new Vector3I(-2, 0, -2),

		new Vector3I(0, -1, 0),
		new Vector3I(-1, -1, 0),
		new Vector3I(-2, -1, 0),

		new Vector3I(0, -1, -1),
		new Vector3I(-1, -1, -1),
		new Vector3I(-2, -1, -1),

		new Vector3I(0, -1, -2),
		new Vector3I(-1, -1, -2),
		new Vector3I(-2, -1, -2),

		new Vector3I(0, -2, 0),
		new Vector3I(-1, -2, 0),
		new Vector3I(-2, -2, 0),

		new Vector3I(0, -2, -1),
		new Vector3I(-1, -2, -1),
		new Vector3I(-2, -2, -1),

		new Vector3I(0, -2, -2),
		new Vector3I(-1, -2, -2),
		new Vector3I(-2, -2, -2)
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

		AddPlacedNode(anchorCell, instance);

		Vector3 spawnWorldPos = GetStructureCenterWorld(matchedPieces);
		instance.GlobalPosition = spawnWorldPos;

		if (recipe.RecipeId == "log_stone_to_workbench")
			instance.GlobalBasis = GetWorkbenchSpawnBasis(matchedPieces);
		else
			instance.GlobalBasis = spawnBasis;
			
		if (instance is CraftedStructure structure)
		{
			structure.ItemId = recipe.OutputItemId;
			structure.GridCell = anchorCell;
			structure.PlacedCell = anchorCell;

			foreach (var piece in matchedPieces)
				structure.SourceRecipePieces[piece.GridCell] = piece.ItemId;
		}

		GD.Print($"World crafted: {recipe.RecipeId}");
		return true;
	}

	private Vector3 GetStructureCenterWorld(List<WorldPlacedPiece> matchedPieces)
	{
		int minX = int.MaxValue;
		int maxX = int.MinValue;
		int minY = int.MaxValue;
		int minZ = int.MaxValue;
		int maxZ = int.MinValue;

		foreach (var piece in matchedPieces)
		{
			var c = piece.GridCell;

			if (c.X < minX) minX = c.X;
			if (c.X > maxX) maxX = c.X;
			if (c.Y < minY) minY = c.Y;
			if (c.Z < minZ) minZ = c.Z;
			if (c.Z > maxZ) maxZ = c.Z;
		}

		Vector3 minWorld = GridUtils.CellToWorld(new Vector3I(minX, minY, minZ));
		Vector3 maxWorld = GridUtils.CellToWorld(new Vector3I(maxX, minY, maxZ));

		return (minWorld + maxWorld) * 0.5f;
	}
	
	private Basis GetWorkbenchSpawnBasis(List<WorldPlacedPiece> matchedPieces)
	{
		var stones = new List<WorldPlacedPiece>();

		foreach (var piece in matchedPieces)
		{
			if (piece.ItemId == "stone")
				stones.Add(piece);
		}

		if (stones.Count != 2)
			return Basis.Identity;

		int minX = int.MaxValue;
		int maxX = int.MinValue;
		int minZ = int.MaxValue;
		int maxZ = int.MinValue;

		foreach (var piece in matchedPieces)
		{
			var c = piece.GridCell;

			if (c.X < minX) minX = c.X;
			if (c.X > maxX) maxX = c.X;
			if (c.Z < minZ) minZ = c.Z;
			if (c.Z > maxZ) maxZ = c.Z;
		}

		var a = stones[0].GridCell;
		var b = stones[1].GridCell;

		float yawDeg = 0f;

		// Stones form a horizontal row (same Z)
		if (a.Z == b.Z)
		{
			// SS / ..  -> south
			if (a.Z == minZ)
				yawDeg = 180f;

			// .. / SS -> north
			else
				yawDeg = 0f;
		}
		// Stones form a vertical column (same X)
		else if (a.X == b.X)
		{
			// S. / S. -> east
			if (a.X == minX)
				yawDeg = -90f;

			// .S / .S -> west
			else
				yawDeg = 90f;
		}

		return Basis.FromEuler(new Vector3(0f, Mathf.DegToRad(yawDeg), 0f));
	}
	
	public bool TryGetWorldCraftPreviewCells(Vector3I lookedAtCell, out List<Vector3I> previewCells)
	{
		previewCells = new List<Vector3I>();

		if (_worldCraftingManager == null)
			return false;

		var craftingPieces = GetPlacedCraftingPieces();
		if (craftingPieces.Count == 0)
			return false;

		foreach (var offset in CraftAnchorOffsets)
		{
			var anchorCell = lookedAtCell + offset;

			if (_worldCraftingManager.TryGetPreviewCellsAtAnchor(
				anchorCell,
				craftingPieces,
				out previewCells))
			{
				return true;
			}
		}

		return false;
	}
	
	public bool TryDeconstructStructure(CraftedStructure structure)
	{
		if (structure == null || !IsInstanceValid(structure))
			return false;

		if (structure.SourceRecipePieces == null || structure.SourceRecipePieces.Count == 0)
			return false;

		if (!_placedItems.ContainsKey(structure.PlacedCell))
			return false;

		_placedItems.Remove(structure.PlacedCell);
		structure.QueueFree();

		foreach (var kvp in structure.SourceRecipePieces)
		{
			Vector3I cell = kvp.Key;
			string itemId = kvp.Value;

			if (!TryPlaceInventoryItem(cell, itemId))
				GD.PrintErr($"Failed to restore recipe piece '{itemId}' at {cell}.");
		}

		return true;
	}
	
	private void TrySpawnGrass(Vector3 worldPosition, int x, int z)
	{
		if (GrassClumpScene == null)
			return;

		if (GD.Randf() > GrassSpawnChance)
			return;

		var grass = GrassClumpScene.Instantiate<Node3D>();
		AddChild(grass);

		float offsetX = GD.Randf() * 0.6f - 0.3f;
		float offsetZ = GD.Randf() * 0.6f - 0.3f;
		float yaw = GD.Randf() * 360f;
		float scale = GD.Randf() * 0.25f + 0.9f;
		grass.GlobalPosition = worldPosition + new Vector3(offsetX, .4f, offsetZ);
		grass.RotationDegrees = new Vector3(0f, yaw, 0f);
		grass.Scale = new Vector3(scale, scale, scale);
	}
	
	private void TrySpawnRock(Vector3 worldPosition, int x, int z)
	{
		if (GD.Randf() > RockSpawnChance)
			return;

		PackedScene chosenRock = null;

		var roll = GD.Randf();
		if (roll < 0.33f)
			chosenRock = RockOneScene;
		else if (roll < 0.66f)
			chosenRock = RockTwoScene;
		else
			chosenRock = RockThreeScene;

		if (chosenRock == null)
			return;

		var rock = chosenRock.Instantiate<Node3D>();
		AddChild(rock);

		float offsetX = GD.Randf() * 0.5f - 0.25f;
		float offsetZ = GD.Randf() * 0.5f - 0.25f;
		float yaw = GD.Randf() * 360f;
		float scale = GD.Randf() * 0.3f + 0.85f;
		
		rock.GlobalPosition = worldPosition + new Vector3(offsetX, 0.5f, offsetZ);
		rock.RotationDegrees = new Vector3(0f, yaw, 0f);
		rock.Scale = new Vector3(scale, scale, scale);
	}
}
