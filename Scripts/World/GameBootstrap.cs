using Godot;

public partial class GameBootstrap : Node
{
	[Export] public int FloorSize = 15;
	[Export] public int FloorY = 0;
	[Export] public Vector3I MineStartCell = new Vector3I(0, 0, -5);

	private WorldManager _worldManager;
	private MineManager _mineManager;
	private MobSpawner _mobSpawner;

	public override void _Ready()
	{
		_worldManager = GetNode<WorldManager>("../WorldManager");
		_mineManager = GetNode<MineManager>("../MineManager");
		_mobSpawner = GetNode<MobSpawner>("../MobSpawner");

		CallDeferred(nameof(Bootstrap));
	}

	private void Bootstrap()
	{
		_mineManager.GenerateMine(MineStartCell);
		_worldManager.GenerateSurfaceFloor(FloorSize, FloorY);
		_mobSpawner.SpawnMobs();
	}
}
