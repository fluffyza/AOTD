using Godot;
using System;
using System.Collections.Generic;

public partial class WorldCraftPreview : Node3D
{
	[Export] public NodePath MeshInstancePath;

	[ExportGroup("Visual")]
	[Export] public Color GlowColor = new Color(0.9f, 0.75f, 0.35f, 0.35f);
	[Export] public float PulseSpeed = 2.2f;
	[Export] public float MinAlpha = 0.20f;
	[Export] public float MaxAlpha = 0.55f;
	[Export] public float FaceInset = 0.002f;
	[Export] public float FaceExpand = 0.04f;
	[Export] public float BlockSize = 1.0f;

	private MeshInstance3D _meshInstance;
	private ArrayMesh _mesh;
	private StandardMaterial3D _material;
	private float _time = 0f;

	private readonly HashSet<Vector3I> _cells = new();

	private static readonly Vector3I[] NeighborDirs =
	{
		Vector3I.Right,
		Vector3I.Left,
		Vector3I.Up,
		Vector3I.Down,
		new Vector3I(0, 0, 1),
		new Vector3I(0, 0, -1)
	};

	public override void _Ready()
	{
		_meshInstance = GetNodeOrNull<MeshInstance3D>(MeshInstancePath);
		if (_meshInstance == null)
		{
			GD.PrintErr($"{Name}: MeshInstancePath is not assigned.");
			SetProcess(false);
			return;
		}

		_mesh = new ArrayMesh();
		_meshInstance.Mesh = _mesh;

		_material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			VertexColorUseAsAlbedo = true,
			EmissionEnabled = true,
			Emission = new Color(GlowColor.R, GlowColor.G, GlowColor.B),
			EmissionEnergyMultiplier = 1.6f
		};

		_meshInstance.MaterialOverride = _material;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible || _material == null)
			return;

		_time += (float)delta;
		float pulse = (Mathf.Sin(_time * PulseSpeed * Mathf.Tau) + 1f) * 0.5f;
		float alpha = Mathf.Lerp(MinAlpha, MaxAlpha, pulse);

		Color c = GlowColor;
		c.A = alpha;
		_material.AlbedoColor = c;
		_material.Emission = new Color(c.R, c.G, c.B);
	}

	public void ShowMatch(IEnumerable<Vector3I> occupiedCells)
	{
		_cells.Clear();

		foreach (var cell in occupiedCells)
			_cells.Add(cell);

		if (_cells.Count == 0)
		{
			HidePreview();
			return;
		}

		RebuildMesh();
		Visible = true;
	}

	public void HidePreview()
	{
		Visible = false;
		_cells.Clear();

		if (_mesh != null)
			_mesh.ClearSurfaces();
	}

	private void RebuildMesh()
	{
		if (_mesh == null)
			return;

		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		foreach (var cell in _cells)
		{
			foreach (var dir in NeighborDirs)
			{
				Vector3I neighbor = cell + dir;
				if (_cells.Contains(neighbor))
					continue;

				AddFaceQuad(st, cell, dir);
			}
		}

		st.GenerateNormals();

		_mesh.ClearSurfaces();
		ArrayMesh built = st.Commit();
		_meshInstance.Mesh = built;
	}

	private void AddFaceQuad(SurfaceTool st, Vector3I cell, Vector3I dir)
	{
		float h = BlockSize * 0.5f;
		float e = FaceExpand;
		float inset = FaceInset;

		Vector3 center = CellToLocalCenter(cell);
		Color c = GlowColor;

		Vector3 a, b, d, e2;

		// Note:
		// a-b-d and b-e2-d form the two triangles.
		if (dir == Vector3I.Right)
		{
			float x = center.X + h + inset;
			a  = new Vector3(x, center.Y - h - e, center.Z - h - e);
			b  = new Vector3(x, center.Y + h + e, center.Z - h - e);
			d  = new Vector3(x, center.Y - h - e, center.Z + h + e);
			e2 = new Vector3(x, center.Y + h + e, center.Z + h + e);
		}
		else if (dir == Vector3I.Left)
		{
			float x = center.X - h - inset;
			a  = new Vector3(x, center.Y - h - e, center.Z + h + e);
			b  = new Vector3(x, center.Y + h + e, center.Z + h + e);
			d  = new Vector3(x, center.Y - h - e, center.Z - h - e);
			e2 = new Vector3(x, center.Y + h + e, center.Z - h - e);
		}
		else if (dir == Vector3I.Up)
		{
			float y = center.Y + h + inset;
			a  = new Vector3(center.X - h - e, y, center.Z - h - e);
			b  = new Vector3(center.X + h + e, y, center.Z - h - e);
			d  = new Vector3(center.X - h - e, y, center.Z + h + e);
			e2 = new Vector3(center.X + h + e, y, center.Z + h + e);
		}
		else if (dir == Vector3I.Down)
		{
			float y = center.Y - h - inset;
			a  = new Vector3(center.X - h - e, y, center.Z + h + e);
			b  = new Vector3(center.X + h + e, y, center.Z + h + e);
			d  = new Vector3(center.X - h - e, y, center.Z - h - e);
			e2 = new Vector3(center.X + h + e, y, center.Z - h - e);
		}
		else if (dir == new Vector3I(0, 0, 1))
		{
			float z = center.Z + h + inset;
			a  = new Vector3(center.X - h - e, center.Y - h - e, z);
			b  = new Vector3(center.X + h + e, center.Y - h - e, z);
			d  = new Vector3(center.X - h - e, center.Y + h + e, z);
			e2 = new Vector3(center.X + h + e, center.Y + h + e, z);
		}
		else
		{
			float z = center.Z - h - inset;
			a  = new Vector3(center.X + h + e, center.Y - h - e, z);
			b  = new Vector3(center.X - h - e, center.Y - h - e, z);
			d  = new Vector3(center.X + h + e, center.Y + h + e, z);
			e2 = new Vector3(center.X - h - e, center.Y + h + e, z);
		}

		AddTri(st, a, b, d, c);
		AddTri(st, b, e2, d, c);
	}

	private void AddTri(SurfaceTool st, Vector3 v0, Vector3 v1, Vector3 v2, Color c)
	{
		st.SetColor(c);
		st.AddVertex(v0);
		st.SetColor(c);
		st.AddVertex(v1);
		st.SetColor(c);
		st.AddVertex(v2);
	}

	private Vector3 CellToLocalCenter(Vector3I cell)
	{
		return GridUtils.CellToWorld(cell);
	}
}
