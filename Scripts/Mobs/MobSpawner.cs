using Godot;

public partial class MobSpawner : Node
{
	[Export] public PackedScene MobScene;
	[Export] public int MobCount = 3;

	[Export] public int FloorHalfSize = 20;
	[Export] public int FloorY = 0;

	[Export] public float SpawnHeightOffset = 1.2f;
	[Export] public float EdgePadding = 3f;

	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();
	}

	public void SpawnMobs()
	{
		if (MobScene == null)
		{
			GD.PrintErr("MobSpawner: MobScene not assigned.");
			return;
		}

		for (int i = 0; i < MobCount; i++)
			SpawnOneMob();
	}

	private void SpawnOneMob()
	{
		float min = -FloorHalfSize + EdgePadding;
		float max = FloorHalfSize - EdgePadding;

		float x = _rng.RandfRange(min, max);
		float z = _rng.RandfRange(min, max);

		var mob = MobScene.Instantiate<Node3D>();

		mob.Position = GridUtils.CellToWorld(
			new Vector3I(Mathf.RoundToInt(x), FloorY + 1, Mathf.RoundToInt(z))
		) + new Vector3(0f, SpawnHeightOffset, 0f);

		if (mob is Mob mobScript)
		{
			mobScript.SetWanderBounds(
				-FloorHalfSize + EdgePadding,
				FloorHalfSize - EdgePadding,
				-FloorHalfSize + EdgePadding,
				FloorHalfSize - EdgePadding
			);
		}

		GetTree().CurrentScene.AddChild(mob);
	}
}
