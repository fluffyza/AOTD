using Godot;

public partial class InventorySlotUI : Button
{
	public enum SlotRole
	{
		Inventory,
		CraftingInput,
		CraftingOutput
	}

	[Signal]
	public delegate void SlotPressedEventHandler(InventorySlotUI slotUi);

	[Signal]
	public delegate void SlotHoveredEventHandler(InventorySlotUI slotUi);

	[Signal]
	public delegate void SlotUnhoveredEventHandler(InventorySlotUI slotUi);

	[Export] public NodePath ItemLabelPath;
	[Export] public SlotRole Role = SlotRole.Inventory;
	[Export] public int CraftingSlotIndex = -1;

	private Label _itemLabel;

	public int SlotIndex { get; private set; } = -1;

	public override void _Ready()
	{
		_itemLabel = GetNodeOrNull<Label>(ItemLabelPath);

		if (_itemLabel == null)
			GD.PrintErr($"{Name}: ItemLabelPath is missing.");
		else
			_itemLabel.MouseFilter = MouseFilterEnum.Ignore;

		FocusMode = FocusModeEnum.None;

		ButtonDown += OnButtonDown;
		MouseEntered += OnMouseEnteredSlot;
		MouseExited += OnMouseExitedSlot;
	}
	
	public InventorySlotUI GetSelf()
	{
		return this;
	}


	public void Setup(int slotIndex, InventorySlot slot, bool highlighted = false)
	{
		SlotIndex = slotIndex;

		if (_itemLabel != null)
		{
			if (slot == null || slot.IsEmpty || slot.Item == null)
				_itemLabel.Text = "";
			else
				_itemLabel.Text = $"{slot.Item.DisplayName}\nx{slot.Count}";
		}

		Text = "";
		Modulate = highlighted ? new Color(1f, 1f, 0.7f) : Colors.White;
	}

	private void OnButtonDown()
	{
		EmitSignal(SignalName.SlotPressed, this);
	}

	private void OnMouseEnteredSlot()
	{
		EmitSignal(SignalName.SlotHovered, this);
	}

	private void OnMouseExitedSlot()
	{
		EmitSignal(SignalName.SlotUnhovered, this);
	}
}
