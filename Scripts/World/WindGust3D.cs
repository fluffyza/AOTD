using Godot;
using System;
using System.Collections.Generic;

public partial class WindGust3D : Node3D
{
	[Export] public NodePath MeshInstancePath;

	[ExportGroup("Lifetime")]
	[Export] public float Lifetime = 8f;
	[Export] public float FadeInTime = 1.18f;
	[Export] public float FadeOutTime = 1.28f;

	[ExportGroup("Shape")]
	[Export] public int PointCount = 18;
	[Export] public float StepDistance = 0.55f;
	[Export] public float RibbonWidth = 0.055f;
	[Export] public float VerticalOffset = 0.0f;

	[ExportGroup("Motion")]
	[Export] public float Speed = 5.5f;
	[Export] public float Strength = 1.0f;
	[Export] public float MaxTravelDistance = 12.0f;

	[ExportGroup("Wave")]
	[Export] public float WobbleAmplitude = 0.1f;
	[Export] public float WobbleFrequency = 2.2f;
	[Export] public float ScrollSpeed = 2.80f;

	[ExportGroup("Collision")]
	[Export] public uint CollisionMask = 1;
	[Export] public float CollisionProbeRadius = 0.08f;
	[Export] public float WallSlideFactor = 0.72f;
	[Export] public float CollisionStrengthLoss = 0.18f;
	[Export] public int MaxCollisionBends = 3;

	[ExportGroup("Visual")]
	[Export] public Color GustColor = new Color(1f, 1f, 1f, 0.55f);
	
	[ExportGroup("Layers")]
	[Export] public int RibbonLayers = 3;
	[Export] public float LayerSideOffset = 1.8f;
	[Export] public float LayerVerticalOffset = 0.3f;
	[Export] public float LayerForwardOffset = 1.4f;
	[Export] public float LayerAlphaMultiplier = 0.78f;
	[Export] public float LayerWidthVariance = 0.08f;

	private MeshInstance3D _meshInstance;
	private ArrayMesh _mesh;
	private StandardMaterial3D _material;

	private Vector3 _origin;
	private Vector3 _forward = Vector3.Forward;
	private float _age = 0f;
	private float _distanceTravelled = 0f;
	private float _seed;
	private bool _initialized = false;

	private readonly List<Vector3> _points = new();
	private readonly List<float> _pointStrengths = new();

	public Vector3 GustDirection => _forward;
	public float CurrentStrength => Strength * GetLifetimeAlpha();

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
			VertexColorUseAsAlbedo = true,
			NoDepthTest = false,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		_meshInstance.MaterialOverride = _material;

		_seed = GD.Randf() * 1000f;

		if (!_initialized)
			Initialize(GlobalPosition, -GlobalBasis.Z, Strength);
	}

	public void Initialize(Vector3 worldOrigin, Vector3 direction, float strength = 1.0f)
	{
		GlobalPosition = worldOrigin;
		_origin = Vector3.Zero;
		_forward = direction.Normalized();
		Strength = strength;
		_age = 0f;
		_distanceTravelled = 0f;
		_initialized = true;

		_seed = GD.Randf() * 1000f;
		WobbleAmplitude *= Mathf.Lerp(0.85f, 1.15f, GD.Randf());
		WobbleFrequency *= Mathf.Lerp(0.9f, 1.1f, GD.Randf());
		RibbonWidth *= Mathf.Lerp(0.9f, 1.1f, GD.Randf());
	}

	public override void _Process(double delta)
	{
		if (!_initialized || _meshInstance == null)
			return;

		float dt = (float)delta;
		_age += dt;

		if (_age >= Lifetime)
		{
			QueueFree();
			return;
		}

		_origin += _forward * Speed * dt;
		_distanceTravelled += Speed * dt;

		RebuildPointChain();
		RebuildMesh();
	}

	private void RebuildPointChain()
	{
		_points.Clear();
		_pointStrengths.Clear();

		var spaceState = GetWorld3D().DirectSpaceState;

		Vector3 current = _origin + Vector3.Up * VerticalOffset;
		Vector3 dir = _forward;
		float remainingStrength = Strength;
		int bendsUsed = 0;

		for (int i = 0; i < PointCount; i++)
		{
			_points.Add(current);
			_pointStrengths.Add(Mathf.Max(0f, remainingStrength));

			if ((i * StepDistance) > MaxTravelDistance)
				break;

			Vector3 target = current + dir * StepDistance;

			Vector3 worldCurrent = ToGlobal(current);
			Vector3 worldTarget = ToGlobal(target);

			var query = PhysicsRayQueryParameters3D.Create(worldCurrent, worldTarget);
			query.CollisionMask = CollisionMask;
			query.CollideWithAreas = false;
			query.CollideWithBodies = true;
			query.HitFromInside = false;

			var hit = spaceState.IntersectRay(query);

			if (hit.Count > 0)
			{
				Vector3 hitPosition = ToLocal((Vector3)hit["position"]);
				Vector3 hitNormal = ((Vector3)hit["normal"]).Normalized();

				current = hitPosition - dir * 0.03f;

				Vector3 slideDir = (dir - hitNormal * dir.Dot(hitNormal)).Normalized();

				if (slideDir.LengthSquared() < 0.001f)
				{
					remainingStrength *= 0.65f;
					break;
				}

				dir = dir.Slerp(slideDir, WallSlideFactor).Normalized();
				remainingStrength -= CollisionStrengthLoss;
				bendsUsed++;

				if (remainingStrength <= 0.05f || bendsUsed > MaxCollisionBends)
					break;
			}
			else
			{
				current = target;
			}
		}
	}

	private void RebuildMesh()
	{
		if (_points.Count < 2)
		{
			_mesh.ClearSurfaces();
			return;
		}

		var camera = GetViewport().GetCamera3D();
		if (camera == null)
			return;

		SurfaceTool st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);

		float lifetimeAlpha = GetLifetimeAlpha();
		float time = _age * ScrollSpeed + _seed;
		float fadeInFactor = GetFadeInFactor();
		
		for (int layer = 0; layer < RibbonLayers; layer++)
		{
			float layerCenter = RibbonLayers <= 1 ? 0f : ((float)layer / (RibbonLayers - 1)) * 2f - 1f;
			float layerAlpha = Mathf.Lerp(1f, LayerAlphaMultiplier, Mathf.Abs(layerCenter));
			float layerPhase = _seed + (layer * 1.37f);

			float sideOffsetAmount = layerCenter * LayerSideOffset;
			float verticalOffsetAmount = Mathf.Sin(layer * 1.9f + _seed) * LayerVerticalOffset;
			float forwardOffsetAmount = layerCenter * LayerForwardOffset;
			float widthScale = 1f - (Mathf.Abs(layerCenter) * LayerWidthVariance);

			for (int i = 0; i < _points.Count - 1; i++)
			{
				Vector3 p0 = _points[i];
				Vector3 p1 = _points[i + 1];

				float t0 = (float)i / (_points.Count - 1);
				float t1 = (float)(i + 1) / (_points.Count - 1);

				if (t0 > fadeInFactor)
					continue;

				Vector3 tangent0 = GetPointTangent(i);
				Vector3 tangent1 = GetPointTangent(i + 1);

				Vector3 worldP0 = ToGlobal(p0);
				Vector3 worldP1 = ToGlobal(p1);

				Vector3 camDir0 = (camera.GlobalPosition - worldP0).Normalized();
				Vector3 camDir1 = (camera.GlobalPosition - worldP1).Normalized();

				Vector3 side0 = tangent0.Cross(camDir0).Normalized();
				Vector3 side1 = tangent1.Cross(camDir1).Normalized();
				
				Vector3 stableSide0 = GetStableSide(tangent0);
				Vector3 stableSide1 = GetStableSide(tangent1);

				if (side0.LengthSquared() < 0.001f)
					side0 = Vector3.Right;
				if (side1.LengthSquared() < 0.001f)
					side1 = Vector3.Right;
					
				float layerWobbleScale = 1f + (layerCenter * 0.25f);
				
				float wave0 = Mathf.Sin((t0 * WobbleFrequency * Mathf.Pi * 2f) + time + layerPhase) * WobbleAmplitude * layerWobbleScale;
				float wave1 = Mathf.Sin((t1 * WobbleFrequency * Mathf.Pi * 2f) + time + layerPhase) * WobbleAmplitude * layerWobbleScale;
				
				Vector3 wobbleAxis0 = GetStableWobbleAxis(tangent0);
				Vector3 wobbleAxis1 = GetStableWobbleAxis(tangent1);

				Vector3 wobbleDir0 = (wobbleAxis0 + Vector3.Up * 0.35f).Normalized();
				Vector3 wobbleDir1 = (wobbleAxis1 + Vector3.Up * 0.35f).Normalized();

				p0 += wobbleDir0 * wave0;
				p1 += wobbleDir1 * wave1;

				Vector3 upOffset = Vector3.Up * verticalOffsetAmount;
				Vector3 forwardOffset0 = tangent0 * forwardOffsetAmount;
				Vector3 forwardOffset1 = tangent1 * forwardOffsetAmount;

				p0 += stableSide0 * sideOffsetAmount + upOffset + forwardOffset0;
				p1 += stableSide1 * sideOffsetAmount + upOffset + forwardOffset1;

				float width0 = RibbonWidth * widthScale * fadeInFactor;
				float width1 = RibbonWidth * widthScale * fadeInFactor;

				Vector3 left0 = p0 - side0 * width0;
				Vector3 right0 = p0 + side0 * width0;
				Vector3 left1 = p1 - side1 * width1;
				Vector3 right1 = p1 + side1 * width1;

				float segmentFade0 = t0 <= fadeInFactor ? 1f : 0f;
				float segmentFade1 = t1 <= fadeInFactor ? 1f : 0f;

				float alpha0 = GustColor.A * lifetimeAlpha * GetEdgeFade(t0) * _pointStrengths[i] * segmentFade0 * layerAlpha;
				float alpha1 = GustColor.A * lifetimeAlpha * GetEdgeFade(t1) * _pointStrengths[i + 1] * segmentFade1 * layerAlpha;

				Color c0 = new Color(GustColor.R, GustColor.G, GustColor.B, alpha0);
				Color c1 = new Color(GustColor.R, GustColor.G, GustColor.B, alpha1);

				AddTri(st, left0, right0, left1, c0, c0, c1);
				AddTri(st, right0, right1, left1, c0, c1, c1);
			}
		}

		st.GenerateNormals();

		ArrayMesh newMesh = st.Commit();
		_meshInstance.Mesh = newMesh;
	}

	private void AddTri(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
	{
		st.SetColor(ca);
		st.AddVertex(a);

		st.SetColor(cb);
		st.AddVertex(b);

		st.SetColor(cc);
		st.AddVertex(c);
	}

	private Vector3 GetPointTangent(int index)
	{
		if (_points.Count == 1)
			return _forward;

		if (index == 0)
			return (_points[1] - _points[0]).Normalized();

		if (index == _points.Count - 1)
			return (_points[index] - _points[index - 1]).Normalized();

		return (_points[index + 1] - _points[index - 1]).Normalized();
	}

	private float GetLifetimeAlpha()
	{
		float fadeIn = FadeInTime <= 0.001f ? 1f : Mathf.Clamp(_age / FadeInTime, 0f, 1f);
		float fadeOutStart = Lifetime - FadeOutTime;
		float fadeOut = FadeOutTime <= 0.001f
			? 1f
			: Mathf.Clamp((Lifetime - _age) / FadeOutTime, 0f, 1f);

		if (_age < FadeInTime)
			return Smooth01(fadeIn);

		if (_age > fadeOutStart)
			return Smooth01(fadeOut);

		return 1f;
	}

	private float GetEdgeFade(float t)
	{
		float headTail = Mathf.Sin(t * Mathf.Pi);
		return Mathf.Lerp(0.2f, 1f, Mathf.Clamp(headTail, 0f, 1f));
	}

	private float Smooth01(float x)
	{
		x = Mathf.Clamp(x, 0f, 1f);
		return x * x * (3f - 2f * x);
	}
	
	private float GetFadeInFactor()
	{
		if (FadeInTime <= 0.001f)
			return 1f;

		return Smooth01(Mathf.Clamp(_age / FadeInTime, 0f, 1f));
	}
	
	private Vector3 GetStableSide(Vector3 tangent)
	{
		Vector3 side = Vector3.Up.Cross(tangent).Normalized();

		if (side.LengthSquared() < 0.001f)
			side = Vector3.Right;

		return side;
	}
	
	private Vector3 GetStableWobbleAxis(Vector3 tangent)
	{
		Vector3 axis = Vector3.Up;

		if (Mathf.Abs(tangent.Dot(axis)) > 0.95f)
			axis = Vector3.Right;

		Vector3 wobbleAxis = tangent.Cross(axis).Normalized();

		if (wobbleAxis.LengthSquared() < 0.001f)
			wobbleAxis = Vector3.Forward;

		return wobbleAxis;
	}
}
