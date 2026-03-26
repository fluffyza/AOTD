using Godot;
using System.Collections.Generic;

public partial class BlockManager : Node
{
	[Export] public PackedScene BlockScene;
	[Export] public PackedScene TorchScene;
	
	private ItemDatabase _itemDatabase;
	
	public override void _Ready()
	{
		_itemDatabase = GetNodeOrNull<ItemDatabase>("/root/ItemDatabase");

		if (_itemDatabase == null)
			GD.PrintErr("BlockManager: ItemDatabase autoload not found.");
	}

	public bool TryGetItemDef(string itemId, out ItemDefinition def)
	{
		def = _itemDatabase?.GetItem(itemId);
		return def != null;
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
		var def = _itemDatabase?.GetItem(itemId);
		if (def == null || def.WorldScene == null)
		{
			GD.PrintErr($"BlockManager: no item definition or world scene for '{itemId}'");
			return null;
		}

		var item = def.WorldScene.Instantiate<Node3D>();
		item.Position = worldPosition;
		item.SetMeta("item_id", itemId);

		if (def.IsBlock && def.HasBlockColor)
			ApplyOpaqueColorToBlock(item, def.BlockColor);

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
		block.Position = worldPosition;

		if (unbreakable)
		{
			block.SetMeta("unbreakable", true);

			// Hardcoded color for unbreakable blocks
			ApplyOpaqueColorToBlock(block, new Color(0.35f, 0.35f, 0.35f));
		}
		else
		{
			string itemId = GetRandomMineBlockId();
			block.SetMeta("item_id", itemId);

			var def = _itemDatabase?.GetItem(itemId);
			if (def != null && def.HasBlockColor)
				ApplyOpaqueColorToBlock(block, def.BlockColor);
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
