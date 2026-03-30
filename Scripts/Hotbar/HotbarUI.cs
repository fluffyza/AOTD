using Godot;
using System.Collections.Generic;

public partial class HotbarUI : Control
{
	[Export] public PackedScene HotbarSlotScene;

	private Inventory _inventory;
	private HBoxContainer _container;
	private List<HotbarSlot> _slotUis = new();
	private readonly Dictionary<string, Texture2D> _itemIcons = new();
	
	private void LoadItemIcons()
	{
		_itemIcons["acorn"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-ACORN.png");
		_itemIcons["coal"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-BLOCK-COAL.png");
		_itemIcons["dirt"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-BLOCK-DIRT.png");
		_itemIcons["iron"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-BLOCK-IRON.png");
		_itemIcons["stone"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-BLOCK-STONE.png");
		_itemIcons["wood"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-BLOCK-WOOD.png");
		_itemIcons["torch"] = GD.Load<Texture2D>("res://Materials/Backpack/ICON-TORCH.png");
		_itemIcons["pickaxe"] = GD.Load<Texture2D>("res://Materials/Backpack/Equip/ICON-PICKAXE.png");
	}
	
	private Texture2D GetItemIcon(string itemId)
	{
		if (string.IsNullOrWhiteSpace(itemId))
			return null;

		itemId = itemId.Trim().ToLower();
		return _itemIcons.TryGetValue(itemId, out var icon) ? icon : null;
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		LoadItemIcons();

		_container = GetNode<HBoxContainer>("MarginContainer/HBoxContainer");

		var player = GetTree().CurrentScene.GetNode<Player>("Player (CharacterBody3D)");
		_inventory = player.GetNode<Inventory>("Inventory");

		BuildSlots();
		
		if (_inventory != null)
			_inventory.InventoryChanged += Refresh;

		Refresh();
	}

	private void BuildSlots()
	{
		for (int i = 0; i < Inventory.HotbarSize; i++)
		{
			var slotInstance = HotbarSlotScene.Instantiate<HotbarSlot>();
			_container.AddChild(slotInstance);
			_slotUis.Add(slotInstance);
		}
	}

	public override void _Process(double delta)
	{
		Refresh();
	}

	public void Refresh()
	{
		if (_inventory == null)
			return;

		for (int i = 0; i < Inventory.HotbarSize; i++)
		{
			var slot = _inventory.GetSlot(i);
			bool selected = i == _inventory.SelectedIndex;

			string displayName = "";
			Texture2D icon = null;
			int count = 0;

			if (slot != null && !slot.IsEmpty && slot.Item != null)
			{
				displayName = slot.Item.DisplayName;
				count = slot.Count;
				icon = GetItemIcon(slot.Item.ItemId);
			}

			_slotUis[i].SetSlot(displayName, count, selected, icon);
		}
	}
}
