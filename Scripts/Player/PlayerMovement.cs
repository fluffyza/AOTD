using Godot;

public partial class PlayerMovement : Node
{
	[ExportGroup("References")]
	[Export] public NodePath HeadPath;
	[Export] public NodePath CollisionShapePath;

	[ExportGroup("Movement")]
	[Export] public float WalkSpeed = 6.0f;
	[Export] public float SprintSpeed = 9.0f;
	[Export] public float CrouchSpeed = 3.0f;
	[Export] public float JumpVelocity = 4.5f;

	[ExportGroup("Crouch")]
	[Export] public float StandingHeight = 1.8f;
	[Export] public float CrouchingHeight = 1.15f;
	[Export] public float HeadStandingY = 1.6f;
	[Export] public float HeadCrouchingY = 1.15f;
	[Export] public float HeadLerpSpeed = 10.0f;

	[ExportGroup("Crouch Edge Protection")]
	[Export] public bool PreventFallingOffEdgesWhileCrouching = true;
	[Export] public float EdgeCheckForwardDistance = 0.45f;
	[Export] public float EdgeCheckDownDistance = 1.6f;
	[Export] public float EdgeCheckSideOffset = 0.22f;

	private float _gravity;
	private Node3D _head;
	private CollisionShape3D _collisionShape;
	private CapsuleShape3D _capsuleShape;

	private bool _isCrouching = false;

	public override void _Ready()
	{
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

		if (HeadPath != null && !HeadPath.IsEmpty)
			_head = GetNode<Node3D>(HeadPath);

		if (CollisionShapePath != null && !CollisionShapePath.IsEmpty)
		{
			_collisionShape = GetNode<CollisionShape3D>(CollisionShapePath);
			_capsuleShape = _collisionShape.Shape as CapsuleShape3D;
		}

		if (_capsuleShape != null)
			_capsuleShape.Height = StandingHeight;

		if (_head != null)
		{
			Vector3 pos = _head.Position;
			pos.Y = HeadStandingY;
			_head.Position = pos;
		}
	}

	public void HandlePhysics(CharacterBody3D body, double delta)
	{
		float dt = (float)delta;
		Vector3 velocity = body.Velocity;

		UpdateCrouchState(body, dt);

		if (!body.IsOnFloor())
			velocity.Y -= _gravity * dt;

		if (Input.IsActionJustPressed("jump") && body.IsOnFloor() && !_isCrouching)
			velocity.Y = JumpVelocity;

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (body.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		float currentSpeed = GetCurrentSpeed();

		if (direction != Vector3.Zero)
		{
			float moveX = direction.X * currentSpeed;
			float moveZ = direction.Z * currentSpeed;

			if (_isCrouching && PreventFallingOffEdgesWhileCrouching && body.IsOnFloor())
			{
				Vector3 proposedHorizontal = new Vector3(moveX, 0f, moveZ);

				if (!HasGroundSupportAhead(body, proposedHorizontal))
				{
					moveX = 0f;
					moveZ = 0f;
				}
			}

			velocity.X = moveX;
			velocity.Z = moveZ;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, currentSpeed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, currentSpeed);
		}

		body.Velocity = velocity;
		body.MoveAndSlide();
	}

	private float GetCurrentSpeed()
	{
		if (_isCrouching)
			return CrouchSpeed;

		if (Input.IsActionPressed("sprint"))
			return SprintSpeed;

		return WalkSpeed;
	}

	private void UpdateCrouchState(CharacterBody3D body, float dt)
	{
		bool wantsCrouch = Input.IsActionPressed("crouch");
		bool canStand = CanStandUp(body);

		if (wantsCrouch)
			_isCrouching = true;
		else if (canStand)
			_isCrouching = false;

		float targetHeight = _isCrouching ? CrouchingHeight : StandingHeight;
		float targetHeadY = _isCrouching ? HeadCrouchingY : HeadStandingY;

		if (_capsuleShape != null)
			_capsuleShape.Height = targetHeight;

		if (_head != null)
		{
			Vector3 pos = _head.Position;
			pos.Y = Mathf.Lerp(pos.Y, targetHeadY, HeadLerpSpeed * dt);
			_head.Position = pos;
		}
	}

	private bool CanStandUp(CharacterBody3D body)
	{
		if (!_isCrouching)
			return true;

		if (_collisionShape == null || _capsuleShape == null)
			return true;

		var spaceState = body.GetWorld3D().DirectSpaceState;

		float currentHeight = _capsuleShape.Height;
		float neededExtra = StandingHeight - currentHeight;

		if (neededExtra <= 0.01f)
			return true;

		Vector3 origin = body.GlobalPosition + Vector3.Up * (currentHeight * 0.5f + 0.05f);
		Vector3 end = origin + Vector3.Up * (neededExtra + 0.05f);

		var query = PhysicsRayQueryParameters3D.Create(origin, end);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };

		var result = spaceState.IntersectRay(query);
		return result.Count == 0;
	}

	private bool HasGroundSupportAhead(CharacterBody3D body, Vector3 proposedHorizontalMove)
	{
		if (proposedHorizontalMove.LengthSquared() < 0.0001f)
			return true;

		Vector3 moveDir = proposedHorizontalMove.Normalized();
		Vector3 right = body.GlobalTransform.Basis.X;
		right.Y = 0f;
		right = right.Normalized();

		Vector3 bodyPos = body.GlobalPosition;
		Vector3 ahead = moveDir * EdgeCheckForwardDistance;

		Vector3[] probePoints =
		{
			bodyPos + ahead,
			bodyPos + ahead + right * EdgeCheckSideOffset,
			bodyPos + ahead - right * EdgeCheckSideOffset
		};

		var spaceState = body.GetWorld3D().DirectSpaceState;

		foreach (Vector3 point in probePoints)
		{
			Vector3 rayStart = point + Vector3.Up * 0.2f;
			Vector3 rayEnd = rayStart + Vector3.Down * EdgeCheckDownDistance;

			var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd);
			query.CollideWithBodies = true;
			query.CollideWithAreas = false;
			query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };

			var result = spaceState.IntersectRay(query);

			if (result.Count > 0)
				return true;
		}

		return false;
	}
}
