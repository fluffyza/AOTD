using Godot;

public partial class InventorySlotUI : Button
{
	[Signal]
	public delegate void SlotPressedEventHandler(int slotIndex);

	[Signal]
	public delegate void SlotHoveredEventHandler(int slotIndex);

	[Signal]
	public delegate void SlotUnhoveredEventHandler(int slotIndex);

	[Export] public NodePath ItemLabelPath;

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
		EmitSignal(SignalName.SlotPressed, SlotIndex);
	}

	private void OnMouseEnteredSlot()
	{
		EmitSignal(SignalName.SlotHovered, SlotIndex);
	}

	private void OnMouseExitedSlot()
	{
		EmitSignal(SignalName.SlotUnhovered, SlotIndex);
	}
}
