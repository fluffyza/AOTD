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

		ApplyBlockAppearance(item, def);

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
			ApplyOpaqueColorToBlock(block, new Color(0.35f, 0.35f, 0.35f));
		}
		else
		{
			string itemId = GetRandomMineBlockId();
			block.SetMeta("item_id", itemId);

			var def = _itemDatabase?.GetItem(itemId);
			if (def != null)
				ApplyBlockAppearance(block, def);
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
		block.Position = worldPosition;
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

	private void ApplyBlockAppearance(Node node, ItemDefinition def)
	{
		if (def == null || !def.IsBlock)
			return;

		if (def.HasBlockTextures)
		{
			if (TryApplyBlockTextures(node, def))
				return;
		}

		RestoreOriginalMeshIfNeeded(node);
	
		if (def.HasBlockColor)
			ApplyOpaqueColorToBlock(node, def.BlockColor);
	}

	private bool TryApplyBlockTextures(Node node, ItemDefinition def)
	{
		var meshInstance = FindFirstMeshInstance(node);
		if (meshInstance == null)
			return false;

		var top = def.TopTexture ?? def.SideTexture ?? def.BottomTexture;
		var side = def.SideTexture ?? def.TopTexture ?? def.BottomTexture;
		var bottom = def.BottomTexture ?? def.SideTexture ?? def.TopTexture;

		if (top == null || side == null || bottom == null)
			return false;

		Image topImage = top.GetImage();
		Image sideImage = side.GetImage();
		Image bottomImage = bottom.GetImage();

		if (topImage == null || sideImage == null || bottomImage == null)
			return false;

		var atlasTexture = BuildBlockAtlasTexture(topImage, sideImage, bottomImage);
		var material = CreateAtlasBlockMaterial(atlasTexture);

			
		if (!meshInstance.HasMeta("original_mesh") && meshInstance.Mesh != null)
			meshInstance.SetMeta("original_mesh", meshInstance.Mesh);
			
		meshInstance.Mesh = CreateAtlasCubeMesh();
		meshInstance.MaterialOverride = material;
		meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;

		return true;
	}

	private MeshInstance3D FindFirstMeshInstance(Node node)
	{
		if (node is MeshInstance3D mesh)
			return mesh;

		foreach (Node child in node.GetChildren())
		{
			var found = FindFirstMeshInstance(child);
			if (found != null)
				return found;
		}

		return null;
	}

	private ImageTexture BuildBlockAtlasTexture(Image top, Image side, Image bottom)
	{
		int tileWidth = top.GetWidth();
		int tileHeight = top.GetHeight();

		var atlas = Image.CreateEmpty(tileWidth * 3, tileHeight * 2, false, top.GetFormat());

		// Layout:
		// [ side ][ top ][ side ]
		// [ side ][ bottom ][ side ]

		atlas.BlitRect(side, new Rect2I(Vector2I.Zero, side.GetSize()), new Vector2I(0, 0));
		atlas.BlitRect(top, new Rect2I(Vector2I.Zero, top.GetSize()), new Vector2I(tileWidth, 0));
		atlas.BlitRect(side, new Rect2I(Vector2I.Zero, side.GetSize()), new Vector2I(tileWidth * 2, 0));

		atlas.BlitRect(side, new Rect2I(Vector2I.Zero, side.GetSize()), new Vector2I(0, tileHeight));
		atlas.BlitRect(bottom, new Rect2I(Vector2I.Zero, bottom.GetSize()), new Vector2I(tileWidth, tileHeight));
		atlas.BlitRect(side, new Rect2I(Vector2I.Zero, side.GetSize()), new Vector2I(tileWidth * 2, tileHeight));

		return ImageTexture.CreateFromImage(atlas);
	}

	private StandardMaterial3D CreateAtlasBlockMaterial(Texture2D atlasTexture)
	{
		var material = new StandardMaterial3D();
		material.AlbedoTexture = atlasTexture;
		material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
		material.TextureRepeat = false;

		material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		material.NoDepthTest = false;


		material.Roughness = 1.0f;
		material.Metallic = 0.0f;
		material.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
		
		return material;
	}

	private ArrayMesh CreateAtlasCubeMesh()
	{
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		float h = 0.5f;

		Rect2 uvTop    = AtlasCell(1, 0);
		Rect2 uvBottom = AtlasCell(1, 1);
		Rect2 uvSideA  = AtlasCell(0, 0);
		Rect2 uvSideB  = AtlasCell(2, 0);
		Rect2 uvSideC  = AtlasCell(0, 1);
		Rect2 uvSideD  = AtlasCell(2, 1);

		AddFace(vertices, normals, uvs, indices,
			new Vector3(-h,  h, -h),
			new Vector3( h,  h, -h),
			new Vector3( h,  h,  h),
			new Vector3(-h,  h,  h),
			new Vector3(0, -1, 0),
			uvTop,
			true);
			
		AddFace(vertices, normals, uvs, indices,
			new Vector3(-h, -h,  h),
			new Vector3( h, -h,  h),
			new Vector3( h, -h, -h),
			new Vector3(-h, -h, -h),
			new Vector3(0, 1, 0),
			uvBottom,
			true);
	
		// Front face
		AddFace(vertices, normals, uvs, indices,
			new Vector3(-h, -h,  h),
			new Vector3( h, -h,  h),
			new Vector3( h,  h,  h),
			new Vector3(-h,  h,  h),
			new Vector3(0, 0, -1), // flipped
			uvSideA,
			false);

		// Back face
		AddFace(vertices, normals, uvs, indices,
			new Vector3( h, -h, -h),
			new Vector3(-h, -h, -h),
			new Vector3(-h,  h, -h),
			new Vector3( h,  h, -h),
			new Vector3(0, 0, 1), // flipped
			uvSideB,
			false);

		// Left face
		AddFace(vertices, normals, uvs, indices,
			new Vector3(-h, -h, -h),
			new Vector3(-h, -h,  h),
			new Vector3(-h,  h,  h),
			new Vector3(-h,  h, -h),
			new Vector3(1, 0, 0), // flipped
			uvSideC,
			false);

		// Right face
		AddFace(vertices, normals, uvs, indices,
			new Vector3( h, -h,  h),
			new Vector3( h, -h, -h),
			new Vector3( h,  h, -h),
			new Vector3( h,  h,  h),
			new Vector3(-1, 0, 0), // flipped
			uvSideD,
			false);

		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private Rect2 AtlasCell(int x, int y)
	{
		float cellW = 1.0f / 3.0f;
		float cellH = 1.0f / 2.0f;
		return new Rect2(x * cellW, y * cellH, cellW, cellH);
	}

	private void AddFace(
		List<Vector3> vertices,
		List<Vector3> normals,
		List<Vector2> uvs,
		List<int> indices,
		Vector3 a, Vector3 b, Vector3 c, Vector3 d,
		Vector3 normal,
		Rect2 uvRect,
		bool flipWinding = false)
	{
		int start = vertices.Count;

		vertices.Add(a);
		vertices.Add(b);
		vertices.Add(c);
		vertices.Add(d);

		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);
		normals.Add(normal);

		uvs.Add(new Vector2(uvRect.Position.X, uvRect.Position.Y + uvRect.Size.Y));
		uvs.Add(new Vector2(uvRect.Position.X + uvRect.Size.X, uvRect.Position.Y + uvRect.Size.Y));
		uvs.Add(new Vector2(uvRect.Position.X + uvRect.Size.X, uvRect.Position.Y));
		uvs.Add(new Vector2(uvRect.Position.X, uvRect.Position.Y));

		if (!flipWinding)
		{
			indices.Add(start + 0);
			indices.Add(start + 1);
			indices.Add(start + 2);

			indices.Add(start + 0);
			indices.Add(start + 2);
			indices.Add(start + 3);
		}
		else
		{
			indices.Add(start + 0);
			indices.Add(start + 2);
			indices.Add(start + 1);

			indices.Add(start + 0);
			indices.Add(start + 3);
			indices.Add(start + 2);
		}
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
	
	public void ApplyHeldBlockAppearance(Node node, ItemDefinition def)
	{
		ApplyBlockAppearance(node, def);
	}
	
	
	private void RestoreOriginalMeshIfNeeded(Node node)
	{
		var meshInstance = FindFirstMeshInstance(node);
		if (meshInstance == null)
			return;

		if (meshInstance.HasMeta("original_mesh"))
		{
			var originalMesh = meshInstance.GetMeta("original_mesh").AsGodotObject() as Mesh;
			if (originalMesh != null)
				meshInstance.Mesh = originalMesh;
		}
	}


}
