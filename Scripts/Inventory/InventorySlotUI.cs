using Godot;

public partial class InventorySlotUI : Button
{
	[Signal]
	public delegate void SlotPressedEventHandler(InventorySlotUI slotUi);

	[Signal]
	public delegate void SlotHoveredEventHandler(InventorySlotUI slotUi);

	[Signal]
	public delegate void SlotUnhoveredEventHandler(InventorySlotUI slotUi);

	public enum SlotRole
	{
		Inventory,
		CraftingInput,
		CraftingOutput
	}

	[Export] public NodePath ItemLabelPath;
	[Export] public NodePath CountLabelPath;
	[Export] public NodePath ItemIconPath;

	[Export] public SlotRole Role = SlotRole.Inventory;
	[Export] public int CraftingSlotIndex = -1;

	private Label _itemLabel;
	private Label _countLabel;
	private TextureRect _itemIcon;

	public int SlotIndex { get; private set; } = -1;

	public override void _Ready()
	{
		_itemLabel = GetNodeOrNull<Label>(ItemLabelPath);
		_countLabel = GetNodeOrNull<Label>(CountLabelPath);
		_itemIcon = GetNodeOrNull<TextureRect>(ItemIconPath);

		if (_itemLabel == null)
			GD.PrintErr($"{Name}: ItemLabelPath is missing.");

		if (_countLabel == null)
			GD.PrintErr($"{Name}: CountLabelPath is missing.");

		if (_itemLabel != null)
			_itemLabel.MouseFilter = MouseFilterEnum.Ignore;

		if (_countLabel != null)
			_countLabel.MouseFilter = MouseFilterEnum.Ignore;

		if (_itemIcon != null)
			_itemIcon.MouseFilter = MouseFilterEnum.Ignore;
			
		ButtonDown += () => EmitSignal(SignalName.SlotPressed, this);
		MouseEntered += () => EmitSignal(SignalName.SlotHovered, this);
		MouseExited += () => EmitSignal(SignalName.SlotUnhovered, this);
	}

	public void SetSlotIndex(int index)
	{
		SlotIndex = index;
	}

	public void Setup(int slotIndex, InventorySlot slot, bool highlighted)
	{
		SetSlotIndex(slotIndex);

		string itemId = "";
		int count = 0;

		if (slot != null && !slot.IsEmpty && slot.Item != null)
		{
			itemId = slot.Item.ItemId;
			count = slot.Count;
		}

		SetItemVisual(itemId, count, null);
		ButtonPressed = highlighted;
	}

	public void SetItemVisual(string itemId, int count, Texture2D icon)
	{
		bool hasItem = !string.IsNullOrEmpty(itemId);
		bool hasIconNode = _itemIcon != null;
		bool hasIcon = hasIconNode && icon != null;

		if (!hasItem)
		{
			if (_itemLabel != null)
			{
				_itemLabel.Text = "";
				_itemLabel.Visible = false;
			}

			if (_countLabel != null)
				_countLabel.Text = "";

			if (_itemIcon != null)
			{
				_itemIcon.Texture = null;
				_itemIcon.Visible = false;
			}

			return;
		}

		if (_itemIcon != null)
		{
			_itemIcon.Texture = icon;
			_itemIcon.Visible = hasIcon;
		}

		if (_itemLabel != null)
		{
			_itemLabel.Text = itemId;
			_itemLabel.Visible = !hasIcon;
		}

		if (_countLabel != null)
			_countLabel.Text = count > 1 ? $"x{count}" : "";
	}
}
