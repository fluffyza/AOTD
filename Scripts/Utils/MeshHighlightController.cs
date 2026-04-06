using Godot;
using System.Collections.Generic;

public partial class MeshHighlightController : Node
{
	[Export] public Color EmissionColor = new Color(1.0f, 0.6f, 0.2f);
	[Export] public float EmissionEnergy = 0.08f;

	private readonly List<MeshInstance3D> _meshes = new();
	private readonly Dictionary<MeshInstance3D, Material> _originalOverrides = new();

	private bool _collected = false;
	private bool _highlighted = false;

	public override void _Ready()
	{
		CollectMeshesFromParent();
	}

	private void CollectMeshesFromParent()
	{
		_meshes.Clear();

		Node root = GetParent();
		if (root == null)
			return;

		CollectMeshesRecursive(root);
		_collected = true;
	}

	private void CollectMeshesRecursive(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child == this)
				continue;

			if (child is MeshInstance3D mesh)
				_meshes.Add(mesh);

			CollectMeshesRecursive(child);
		}
	}

	public void SetHighlighted(bool highlighted)
	{
		if (!_collected)
			CollectMeshesFromParent();

		if (_highlighted == highlighted)
			return;

		_highlighted = highlighted;

		foreach (var mesh in _meshes)
		{
			if (mesh == null)
				continue;

			if (!_originalOverrides.ContainsKey(mesh))
				_originalOverrides[mesh] = mesh.MaterialOverride;

			if (highlighted)
			{
				Material baseMaterial = mesh.MaterialOverride;

				if (baseMaterial == null && mesh.Mesh != null && mesh.Mesh.GetSurfaceCount() > 0)
					baseMaterial = mesh.Mesh.SurfaceGetMaterial(0);

				StandardMaterial3D glowMat;

				if (baseMaterial is StandardMaterial3D std)
					glowMat = (StandardMaterial3D)std.Duplicate();
				else
					glowMat = new StandardMaterial3D();

				glowMat.EmissionEnabled = true;
				glowMat.Emission = EmissionColor;
				glowMat.EmissionEnergyMultiplier = EmissionEnergy;

				mesh.MaterialOverride = glowMat;
			}
			else
			{
				mesh.MaterialOverride = _originalOverrides[mesh];
			}
		}
	}
}
