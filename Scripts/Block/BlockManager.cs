using Godot;
using System.Collections.Generic;

public partial class BlockManager : Node
{
	[Export] public PackedScene BlockScene;
	[Export] public PackedScene TorchScene;

	public class ItemDef
	{
		public string Id;
		public string DisplayName;
		public PackedScene Scene;
		public bool IsBlock;
		public Color? BlockColor;
	}

	private readonly Dictionary<string, ItemDef> _itemDefs = new();

	public override void _Ready()
	{
		_itemDefs["torch"] = new ItemDef
		{
			Id = "torch",
			DisplayName = "Torch",
			Scene = TorchScene,
			IsBlock = false,
			BlockColor = null
		};

		_itemDefs["stone"] = new ItemDef
		{
			Id = "stone",
			DisplayName = "Stone",
			Scene = BlockScene,
			IsBlock = true,
			BlockColor = new Color(0.85f, 0.85f, 0.85f)
		};

		_itemDefs["coal"] = new ItemDef
		{
			Id = "coal",
			DisplayName = "Coal",
			Scene = BlockScene,
			IsBlock = true,
			BlockColor = new Color(0.1f, 0.1f, 0.1f)
		};

		_itemDefs["dirt"] = new ItemDef
		{
			Id = "dirt",
			DisplayName = "Dirt",
			Scene = BlockScene,
			IsBlock = true,
			BlockColor = new Color(0.45f, 0.28f, 0.14f)
		};
	}

	public bool TryGetItemDef(string itemId, out ItemDef def)
	{
		return _itemDefs.TryGetValue(itemId, out def);
	}

	public string GetRandomMineBlockId()
	{
		float roll = GD.Randf();

		if (roll < 0.65f)
			return "stone";
		if (roll < 0.80f)
			return "dirt";

		return "coal";
	}

	public Node3D CreatePlacedItem(string itemId, Vector3 worldPosition)
	{
		if (!_itemDefs.TryGetValue(itemId, out var def) || def.Scene == null)
		{
			GD.PrintErr($"BlockManager: no item definition for '{itemId}'");
			return null;
		}

		var item = def.Scene.Instantiate<Node3D>();
		item.Position  = worldPosition;
		item.SetMeta("item_id", itemId);

		if (def.IsBlock && def.BlockColor.HasValue)
			ApplyOpaqueColorToBlock(item, def.BlockColor.Value);

		return item;
	}

	public Node3D CreateMineBlock(Vector3 worldPosition, bool unbreakable = false)
	{
		if (BlockScene == null)
		{
			GD.PrintErr("BlockManager: BlockScene is not assigned!");
			return null;
		}

		var block = BlockScene.Instantiate<Node3D>();
		block.Position  = worldPosition;

		if (unbreakable)
		{
			block.SetMeta("unbreakable", true);
			ApplyOpaqueColorToBlock(block, new Color(0.35f, 0.35f, 0.35f));
		}
		else
		{
			string itemId = GetRandomMineBlockId();
			block.SetMeta("item_id", itemId);

			if (_itemDefs.TryGetValue(itemId, out var def) && def.BlockColor.HasValue)
				ApplyOpaqueColorToBlock(block, def.BlockColor.Value);
		}

		return block;
	}

	public Node3D CreateSurfaceBlock(Vector3 worldPosition)
	{
		if (BlockScene == null)
		{
			GD.PrintErr("BlockManager: BlockScene is not assigned!");
			return null;
		}

		var block = BlockScene.Instantiate<Node3D>();
		block.Position  = worldPosition;
		block.SetMeta("unbreakable", true);
		ApplyOpaqueColorToBlock(block, new Color(0.35f, 0.35f, 0.35f));
		return block;
	}

	public string GetDroppedItemId(Node3D item)
	{
		if (item != null && item.HasMeta("item_id"))
			return item.GetMeta("item_id").AsString();

		return "stone";
	}

	private void ApplyOpaqueColorToBlock(Node node, Color color)
	{
		if (node is MeshInstance3D meshInstance)
		{
			var material = new StandardMaterial3D();
			material.AlbedoColor = new Color(color.R, color.G, color.B, 1.0f);
			material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
			material.NoDepthTest = false;
			meshInstance.MaterialOverride = material;
		}

		foreach (Node child in node.GetChildren())
			ApplyOpaqueColorToBlock(child, color);
	}
}
