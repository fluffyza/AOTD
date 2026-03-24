using Godot;
using System.Collections.Generic;

public partial class HotbarUI : Control
{
	[Export] public PackedScene HotbarSlotScene;

	private Inventory _inventory;
	private HBoxContainer _container;
	private List<HotbarSlot> _slotUis = new();

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

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
			string displayName = slot.IsEmpty ? "" : slot.Item.DisplayName;
			_slotUis[i].SetSlot(displayName, slot.Count, selected);
		}
	}
}
