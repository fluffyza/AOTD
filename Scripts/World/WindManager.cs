using Godot;

public partial class WindManager : Node3D
{
	[Export] public NodePath PlayerPath;

	[ExportGroup("Timing")]
	[Export] public float MinTimeBetweenGusts = 6.0f;
	[Export] public float MaxTimeBetweenGusts = 14.0f;

	[ExportGroup("Spawn Area")]
	[Export] public float MinSpawnRadiusAroundPlayer = 2.0f;
	[Export] public float MaxSpawnRadiusAroundPlayer = 7.0f;
	[Export] public float SpawnHeightOffset = 1f;

	[ExportGroup("Strength")]
	[Export] public float MinWindStrength = 0.35f;
	[Export] public float MaxWindStrength = 1.0f;

	[ExportGroup("Visuals")]
	[Export] public PackedScene WindGustScene;

	[ExportGroup("Direction")]
	[Export] public bool UseRandomDirection = true;
	[Export] public Vector3 DefaultWindDirection = Vector3.Forward;

	private Node3D _player;
	private float _waitTimer = 0.0f;

	private readonly RandomNumberGenerator _rng = new();

	private WindGust3D _activeGust;
	private Vector3 _lastGustOrigin = Vector3.Zero;
	private Vector3 _lastGustDirection = Vector3.Forward;
	private float _lastGustStrength = 0.0f;

	public override void _Ready()
	{
		_rng.Randomize();

		if (PlayerPath != null && !PlayerPath.IsEmpty)
			_player = GetNodeOrNull<Node3D>(PlayerPath);

		ScheduleNextGust();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (IsInstanceValid(_activeGust))
			return;

		_waitTimer -= dt;
		if (_waitTimer <= 0.0f)
			StartRandomGust();
	}

	private void ScheduleNextGust()
	{
		_waitTimer = _rng.RandfRange(MinTimeBetweenGusts, MaxTimeBetweenGusts);
	}

	private void StartRandomGust()
	{
		if (_player == null)
		{
			ScheduleNextGust();
			return;
		}

		Vector3 origin = GetRandomSpawnPositionAroundPlayer();
		Vector3 direction = GetWindDirection();
		float strength = _rng.RandfRange(MinWindStrength, MaxWindStrength);

		SpawnWindGust(origin, direction, strength);

		_lastGustOrigin = origin;
		_lastGustDirection = direction;
		_lastGustStrength = strength;

		GD.Print($"Wind gust started at {origin}, dir={direction}, strength={strength:0.00}");

		ScheduleNextGust();
	}

	private Vector3 GetRandomSpawnPositionAroundPlayer()
	{
		Vector2 offset2D = Vector2.FromAngle(_rng.RandfRange(0.0f, Mathf.Tau))
			* _rng.RandfRange(MinSpawnRadiusAroundPlayer, MaxSpawnRadiusAroundPlayer);

		Vector3 basePos = _player.GlobalPosition;
		return new Vector3(
			basePos.X + offset2D.X,
			basePos.Y + SpawnHeightOffset,
			basePos.Z + offset2D.Y
		);
	}

	private Vector3 GetWindDirection()
	{
		if (UseRandomDirection)
		{
			float angle = _rng.RandfRange(0.0f, Mathf.Tau);
			return new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)).Normalized();
		}

		Vector3 dir = DefaultWindDirection;
		if (dir.LengthSquared() < 0.001f)
			dir = Vector3.Forward;

		return dir.Normalized();
	}

	private void SpawnWindGust(Vector3 position, Vector3 direction, float strength)
	{
		if (WindGustScene == null)
		{
			GD.PrintErr("WindManager: WindGustScene is not assigned.");
			return;
		}

		var gust = WindGustScene.Instantiate<WindGust3D>();
		GetTree().CurrentScene.AddChild(gust);

		gust.Initialize(position, direction, strength);
		_activeGust = gust;
	}

	public bool HasActiveGust()
	{
		return IsInstanceValid(_activeGust);
	}

	public Vector3 GetWindDirectionVector()
	{
		if (IsInstanceValid(_activeGust))
			return _activeGust.GustDirection;

		return _lastGustDirection;
	}

	public float GetCurrentWindStrength()
	{
		if (IsInstanceValid(_activeGust))
			return _activeGust.CurrentStrength;

		return 0.0f;
	}

	public Vector3 GetLastGustOrigin()
	{
		return _lastGustOrigin;
	}
}
