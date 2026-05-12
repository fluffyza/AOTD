using Godot;

public partial class SurfaceRock : Node3D
{
	[Export] public int HitsNeeded = 3;
	[Export] public string RequiredToolId = "pickaxe";
	[Export] public string DropItemId = "stone";
	[Export] public int DropAmount = 1;

	private int _hits = 0;

	public bool Mine(Player player)
	{
		if (player == null)
			return false;

		if (!player.IsHoldingItem(RequiredToolId))
		{
			GD.Print($"You need a {RequiredToolId} to mine this rock.");
			return false;
		}

		_hits++;

		GD.Print($"Rock hit {_hits}/{HitsNeeded}");

		if (_hits < HitsNeeded)
			return true;

		player.AddItemToInventory(DropItemId, DropAmount);
		QueueFree();
		return true;
	}
}
