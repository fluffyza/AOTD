using Godot;
using System.Collections.Generic;

public partial class HotbarUI : Control
{
	[Export] public PackedScene HotbarSlotScene;

	private Inventory _inventory;
	private HBoxContainer _container;
	private List<HotbarSlot> _slotUis = new();
	private readonly Dictionary<string, Texture2D> _itemIcons = new();

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_container = GetNode<HBoxContainer>("MarginContainer/HBoxContainer");

		Node root = GetTree().Root;
		Player player = root.FindChild("Player (CharacterBody3D)", true, false) as Player;

		if (player == null)
		{
			GD.PrintErr("HotbarUI: Could not find Player anywhere in the scene tree.");
			return;
		}

		_inventory = player.GetNodeOrNull<Inventory>("Inventory");

		if (_inventory == null)
		{
			GD.PrintErr("HotbarUI: Could not find Inventory under Player.");
			return;
		}

		BuildSlots();

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
				icon = slot.Item.Icon;
			}

			_slotUis[i].SetSlot(displayName, count, selected, icon);
		}
	}
}
