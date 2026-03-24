using Godot;
using System.Collections.Generic;

public partial class BackpackUI : Control
{
	[Export] public NodePath BackpackGridPath;
	[Export] public NodePath HotbarRowPath;
	[Export] public NodePath DraggedItemLabelPath;

	private GridContainer _backpackGrid;
	private HBoxContainer _hotbarRow;
	private Label _draggedItemLabel;

	private readonly List<InventorySlotUI> _backpackSlotUis = new();
	private readonly List<InventorySlotUI> _hotbarSlotUis = new();

	private Inventory _inventory;

	private int _pressedSlotIndex = -1;
	private int _dragSourceSlotIndex = -1;
	private int _hoveredSlotIndex = -1;

	private bool _isMouseHeld = false;
	private bool _isDragging = false;

	private Vector2 _pressMousePosition;
	private const float DragThreshold = 12f;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		_backpackGrid = GetNode<GridContainer>(BackpackGridPath);
		_hotbarRow = GetNode<HBoxContainer>(HotbarRowPath);
		_draggedItemLabel = GetNode<Label>(DraggedItemLabelPath);

		CacheSlotReferences();

		Visible = false;
		_draggedItemLabel.Visible = false;
	}

	public void Initialize(Inventory inventory)
	{
		_inventory = inventory;

		if (_inventory != null)
			_inventory.InventoryChanged += Refresh;

		Refresh();
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		if (_isMouseHeld && !_isDragging && _pressedSlotIndex != -1)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			if (mousePos.DistanceTo(_pressMousePosition) >= DragThreshold)
			{
				StartDrag();
			}
		}

		if (_draggedItemLabel.Visible)
			_draggedItemLabel.Position = GetGlobalMousePosition() + new Vector2(16, 16);
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event.IsActionPressed("toggle_inventory"))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed)
			{
				_isMouseHeld = true;
			}
			else
			{
				_isMouseHeld = false;

				if (_isDragging)
					FinishDrag();
				else
					ClearPressedState();
			}
		}
	}

	public void Toggle()
	{
		if (Visible)
			Close();
		else
			Open();
	}

	public void Open()
	{
		Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Refresh();
	}

	public void Close()
	{
		Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		CancelDrag();
	}

	private void CacheSlotReferences()
	{
		_backpackSlotUis.Clear();
		_hotbarSlotUis.Clear();

		foreach (Node child in _backpackGrid.GetChildren())
		{
			if (child is InventorySlotUI slotUi)
			{
				slotUi.SlotPressed += OnSlotPressed;
				slotUi.SlotHovered += OnSlotHovered;
				slotUi.SlotUnhovered += OnSlotUnhovered;
				_backpackSlotUis.Add(slotUi);
			}
		}

		foreach (Node child in _hotbarRow.GetChildren())
		{
			if (child is InventorySlotUI slotUi)
			{
				slotUi.SlotPressed += OnSlotPressed;
				slotUi.SlotHovered += OnSlotHovered;
				slotUi.SlotUnhovered += OnSlotUnhovered;
				_hotbarSlotUis.Add(slotUi);
			}
		}

		if (_backpackSlotUis.Count != Inventory.BackpackSize)
			GD.PrintErr($"Expected {Inventory.BackpackSize} backpack slots, found {_backpackSlotUis.Count}");

		if (_hotbarSlotUis.Count != Inventory.HotbarSize)
			GD.PrintErr($"Expected {Inventory.HotbarSize} hotbar slots, found {_hotbarSlotUis.Count}");
	}

	private void Refresh()
	{
		if (_inventory == null)
			return;

		for (int i = 0; i < _backpackSlotUis.Count; i++)
		{
			int inventoryIndex = Inventory.HotbarSize + i;
			bool highlighted = inventoryIndex == _dragSourceSlotIndex || inventoryIndex == _hoveredSlotIndex;
			_backpackSlotUis[i].Setup(inventoryIndex, _inventory.GetSlot(inventoryIndex), highlighted);
		}

		for (int i = 0; i < _hotbarSlotUis.Count; i++)
		{
			bool selected = i == _inventory.SelectedIndex;
			bool highlighted = i == _dragSourceSlotIndex || i == _hoveredSlotIndex;
			_hotbarSlotUis[i].Setup(i, _inventory.GetSlot(i), selected || highlighted);
		}

		UpdateDraggedLabel();
	}

	private void OnSlotPressed(int slotIndex)
	{
		if (_inventory == null)
			return;

		var slot = _inventory.GetSlot(slotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		_pressedSlotIndex = slotIndex;
		_pressMousePosition = GetGlobalMousePosition();
	}

	private void OnSlotHovered(int slotIndex)
	{
		_hoveredSlotIndex = slotIndex;

		if (_isDragging)
			Refresh();
	}

	private void OnSlotUnhovered(int slotIndex)
	{
		if (_hoveredSlotIndex == slotIndex)
			_hoveredSlotIndex = -1;

		if (_isDragging)
			Refresh();
	}

	private void StartDrag()
	{
		if (_pressedSlotIndex == -1 || _inventory == null)
			return;

		var slot = _inventory.GetSlot(_pressedSlotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		_dragSourceSlotIndex = _pressedSlotIndex;
		_pressedSlotIndex = -1;
		_isDragging = true;
		Refresh();
	}

	private void FinishDrag()
	{
		if (!_isDragging)
			return;

		if (_dragSourceSlotIndex != -1 &&
			_hoveredSlotIndex != -1 &&
			_dragSourceSlotIndex != _hoveredSlotIndex)
		{
			_inventory.SwapSlots(_dragSourceSlotIndex, _hoveredSlotIndex);
		}

		CancelDrag();
	}

	private void CancelDrag()
	{
		_pressedSlotIndex = -1;
		_dragSourceSlotIndex = -1;
		_hoveredSlotIndex = -1;
		_isMouseHeld = false;
		_isDragging = false;

		UpdateDraggedLabel();
		Refresh();
	}

	private void ClearPressedState()
	{
		_pressedSlotIndex = -1;
	}

	private void UpdateDraggedLabel()
	{
		if (_draggedItemLabel == null)
			return;

		if (!_isDragging || _inventory == null || _dragSourceSlotIndex == -1)
		{
			_draggedItemLabel.Visible = false;
			_draggedItemLabel.Text = "";
			return;
		}

		var slot = _inventory.GetSlot(_dragSourceSlotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
		{
			_draggedItemLabel.Visible = false;
			_draggedItemLabel.Text = "";
			return;
		}

		_draggedItemLabel.Visible = true;
		_draggedItemLabel.Text = $"{slot.Item.DisplayName} x{slot.Count}";
	}
}
