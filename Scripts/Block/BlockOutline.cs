using Godot;

public partial class BlockOutline : Node3D
{
	public override void _Ready()
	{
		CreateOutline();
		Visible = false;
	}

	public void UpdateFromTarget(BlockTargetting targetting)
	{
		if (!targetting.IsLookingAtPlacedItem)
		{
			Visible = false;
			return;
		}

		Visible = true;
		GlobalPosition = GridUtils.CellToWorld(targetting.LookedAtCell);
		GlobalRotation = Vector3.Zero;
		Scale = new Vector3(1.02f, 1.02f, 1.02f);
	}

	private void CreateOutline()
	{
		var material = new StandardMaterial3D();
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.AlbedoColor = new Color(0f, 0f, 0f, 1f);
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.CullMode = BaseMaterial3D.CullModeEnum.Back;
		material.NoDepthTest = false;

		float half = 0.5f;
		float thickness = 0.05f;

		Vector3[] corners =
		{
			new Vector3(-half, -half, -half),
			new Vector3( half, -half, -half),
			new Vector3( half,  half, -half),
			new Vector3(-half,  half, -half),
			new Vector3(-half, -half,  half),
			new Vector3( half, -half,  half),
			new Vector3( half,  half,  half),
			new Vector3(-half,  half,  half),
		};

		int[,] edges =
		{
			{0,1}, {1,2}, {2,3}, {3,0},
			{4,5}, {5,6}, {6,7}, {7,4},
			{0,4}, {1,5}, {2,6}, {3,7}
		};

		for (int i = 0; i < edges.GetLength(0); i++)
		{
			CreateEdge(corners[edges[i, 0]], corners[edges[i, 1]], thickness, material);
		}
	}

	private void CreateEdge(Vector3 start, Vector3 end, float thickness, Material material)
	{
		Vector3 direction = end - start;
		float length = direction.Length();
		Vector3 midpoint = (start + end) / 2f;

		var edge = new MeshInstance3D();
		var box = new BoxMesh();

		if (Mathf.Abs(direction.X) > 0.001f)
			box.Size = new Vector3(length, thickness, thickness);
		else if (Mathf.Abs(direction.Y) > 0.001f)
			box.Size = new Vector3(thickness, length, thickness);
		else
			box.Size = new Vector3(thickness, thickness, length);

		edge.Mesh = box;
		edge.MaterialOverride = material;
		edge.Position = midpoint;
		edge.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		AddChild(edge);
	}
}
