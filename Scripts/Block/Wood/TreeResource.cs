using Godot;

public partial class TreeResource : Node3D
{
	[Export] public int MaxHealth = 3;
	[Export] public int MinWoodDrop = 5;
	[Export] public int MaxWoodDrop = 8;
	[Export] public float AcornDropChance = 0.25f;

	private int _health;

	public override void _Ready()
	{
		_health = MaxHealth;
	}

	public void Mine(Player player)
	{
		if (player == null)
			return;

		_health--;

		GD.Print($"Tree hit. Health remaining: {_health}");

		if (_health <= 0)
			Harvest(player);
	}

	private void Harvest(Player player)
	{
		var inventory = player.GetNodeOrNull<Inventory>("Inventory");
		if (inventory != null)
		{
			int woodAmount = (int)GD.RandRange(MinWoodDrop, MaxWoodDrop);
			inventory.AddItem("wood", woodAmount);

			if (GD.Randf() < AcornDropChance)
				inventory.AddItem("acorn", 1);
		}

		QueueFree();
	}
}
