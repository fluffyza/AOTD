using Godot;

public partial class BlockTargetting : Node
{
	[Export] public NodePath CameraPath;
	[Export] public float ReachDistance = 8.0f;

	private Camera3D _camera;
	private CharacterBody3D _playerBody;
	private WorldManager _worldManager;

	public bool HasHit { get; private set; }
	public bool HasValidPlacementTarget { get; private set; }
	public bool IsLookingAtPlacedItem { get; private set; }

	public Vector3 HitPosition { get; private set; }
	public Vector3 HitNormal { get; private set; }

	public Vector3I TargetCell { get; private set; }
	public Vector3I RemoveCell { get; private set; }
	public Vector3I LookedAtCell { get; private set; }

	public Node3D HitItem { get; private set; }

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>(CameraPath);
		_playerBody = GetParent<CharacterBody3D>();
		_worldManager = GetNode<WorldManager>("../WorldManager");
	}

	public void UpdateTarget()
	{
		ResetState();

		var space = _playerBody.GetWorld3D().DirectSpaceState;

		Vector3 from = _camera.GlobalPosition;
		Vector3 to = from + (-_camera.GlobalTransform.Basis.Z * ReachDistance);

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.Exclude = new Godot.Collections.Array<Rid> { _playerBody.GetRid() };

		var result = space.IntersectRay(query);

		if (result.Count == 0)
			return;

		HasHit = true;

		HitPosition = (Vector3)result["position"];
		HitNormal = (Vector3)result["normal"];

		TargetCell = GridUtils.WorldToCell(HitPosition + HitNormal * 0.01f);
		HasValidPlacementTarget = true;

		if (_worldManager.TryGetLookedAtPlacedItem(result, out Vector3I lookedAtCell, out Node3D item))
		{
			IsLookingAtPlacedItem = true;
			LookedAtCell = lookedAtCell;
			RemoveCell = lookedAtCell;
			HitItem = item;
		}
		else
		{
			IsLookingAtPlacedItem = false;
			RemoveCell = GridUtils.WorldToCell(HitPosition - HitNormal * 0.01f);
			HitItem = null;
		}
	}

	private void ResetState()
	{
		HasHit = false;
		HasValidPlacementTarget = false;
		IsLookingAtPlacedItem = false;

		HitPosition = Vector3.Zero;
		HitNormal = Vector3.Zero;

		TargetCell = Vector3I.Zero;
		RemoveCell = Vector3I.Zero;
		LookedAtCell = Vector3I.Zero;

		HitItem = null;
	}
}
