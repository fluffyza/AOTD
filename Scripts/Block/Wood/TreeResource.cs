using Godot;

public partial class TreeResource : Node3D
{
	[Export] public NodePath SpritePath;
	[Export] public TreeDefinition Definition;

	private int _health;
	private Sprite3D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite3D>(SpritePath);

		if (_sprite == null)
		{
			GD.PrintErr($"{Name}: SpritePath is missing or invalid.");
			return;
		}

		ApplyDefinition();
	}

	private void ApplyDefinition()
	{
		if (Definition == null)
		{
			GD.PrintErr($"{Name}: TreeDefinition is missing.");
			return;
		}

		_health = Definition.MaxHealth;

		if (_sprite != null)
		{
			_sprite.Texture = Definition.SpriteTexture;
			_sprite.Scale = Definition.SpriteScale;
			_sprite.Modulate = Definition.NormalTint;

			if (_sprite.MaterialOverride is StandardMaterial3D material)
			{
				material.AlbedoTexture = Definition.SpriteTexture;
				material.AlbedoColor = Definition.NormalTint;
			}
		}
	}

	//public void SetHighlighted(bool highlighted)
	//{
		//if (_sprite == null || Definition == null)
			//return;
//
		//Color tint = highlighted ? Definition.HighlightTint : Definition.NormalTint;
//
		//_sprite.Modulate = tint;
//
		//if (_sprite.MaterialOverride is StandardMaterial3D material)
			//material.AlbedoColor = tint;
	//}

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
		if (inventory != null && Definition != null)
		{
			if (!string.IsNullOrEmpty(Definition.PrimaryDropItemId))
			{
				int amount = (int)GD.RandRange(Definition.MinPrimaryDrop, Definition.MaxPrimaryDrop);
				inventory.AddItem(Definition.PrimaryDropItemId, amount);
			}

			if (!string.IsNullOrEmpty(Definition.SecondaryDropItemId) &&
				GD.Randf() < Definition.SecondaryDropChance)
			{
				inventory.AddItem(Definition.SecondaryDropItemId, Definition.SecondaryDropAmount);
			}
		}

		QueueFree();
	}
}
