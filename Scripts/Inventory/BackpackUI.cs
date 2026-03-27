using Godot;
using System.Collections.Generic;

public partial class BackpackUI : Control
{
	
	private InventorySlotUI.SlotRole _heldSourceRole = InventorySlotUI.SlotRole.Inventory;
	private int _heldSourceCraftIndex = -1;
	private const int CraftOutputSourceMarker = -999;
	
	[Export] public NodePath BackpackCraftingPanelPath;
	[Export] public NodePath BackpackCraftingGridPath;
	[Export] public NodePath BackpackCraftingOutputSlotPath;
	[Export] public NodePath BackpackCraftingContainerPath;

	[Export] public NodePath WorkbenchCraftingPanelPath;
	[Export] public NodePath WorkbenchCraftingGridPath;
	[Export] public NodePath WorkbenchCraftingOutputSlotPath;
	[Export] public NodePath WorkbenchCraftingContainerPath;
	
	private Control _backpackCraftingPanel;
	private GridContainer _backpackCraftingGrid;
	private InventorySlotUI _backpackCraftOutputSlotUi;
	private readonly List<InventorySlotUI> _backpackCraftingSlotUis = new();
	private CraftingContainer _backpackCraftingContainer;

	private Control _workbenchCraftingPanel;
	private GridContainer _workbenchCraftingGrid;
	private InventorySlotUI _workbenchCraftOutputSlotUi;
	private readonly List<InventorySlotUI> _workbenchCraftingSlotUis = new();
	private CraftingContainer _workbenchCraftingContainer;

	private GridContainer _craftingGrid;
	private InventorySlotUI _craftOutputSlotUi;
	private readonly List<InventorySlotUI> _craftingSlotUis = new();
	private CraftingContainer _craftingContainer;

	private InventorySlotUI _hoveredSlotUi = null;
	private InventorySlotUI _pressedSlotUi = null;

	[Export] public NodePath BackpackGridPath;
	[Export] public NodePath HotbarRowPath;
	[Export] public NodePath DraggedItemLabelPath;

	private enum DragMode
	{
		None,
		FullStack,
		HalfStack,
		SingleItem
	}

	private GridContainer _backpackGrid;
	private HBoxContainer _hotbarRow;
	private Label _draggedItemLabel;

	private readonly List<InventorySlotUI> _backpackSlotUis = new();
	private readonly List<InventorySlotUI> _hotbarSlotUis = new();

	private Inventory _inventory;

	private int _pressedSlotIndex = -1;
	private int _hoveredSlotIndex = -1;
	private int _heldSourceSlotIndex = -1;

	private bool _isMouseHeld = false;
	private bool _isDragging = false;

	private Vector2 _pressMousePosition;
	private const float DragThreshold = 12f;

	private ItemDefinition _heldItem = null;
	private int _heldCount = 0;
	private DragMode _dragMode = DragMode.None;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		_backpackGrid = GetNode<GridContainer>(BackpackGridPath);
		_hotbarRow = GetNode<HBoxContainer>(HotbarRowPath);
		_draggedItemLabel = GetNode<Label>(DraggedItemLabelPath);

		_backpackCraftingPanel = GetNode<Control>(BackpackCraftingPanelPath);
		_backpackCraftingGrid = GetNode<GridContainer>(BackpackCraftingGridPath);
		_backpackCraftOutputSlotUi = GetNode<InventorySlotUI>(BackpackCraftingOutputSlotPath);
		_backpackCraftingContainer = GetNode<CraftingContainer>(BackpackCraftingContainerPath);

		_workbenchCraftingPanel = GetNode<Control>(WorkbenchCraftingPanelPath);
		_workbenchCraftingGrid = GetNode<GridContainer>(WorkbenchCraftingGridPath);
		_workbenchCraftOutputSlotUi = GetNode<InventorySlotUI>(WorkbenchCraftingOutputSlotPath);
		_workbenchCraftingContainer = GetNode<CraftingContainer>(WorkbenchCraftingContainerPath);

		CacheSlotReferences();

		OpenBackpackCraftingMode();

		Visible = false;
		_draggedItemLabel.Visible = false;

		if (_backpackCraftingContainer != null)
			_backpackCraftingContainer.CraftingChanged += Refresh;

		if (_workbenchCraftingContainer != null)
			_workbenchCraftingContainer.CraftingChanged += Refresh;
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

		if (_isMouseHeld && !_isDragging && _pressedSlotUi != null)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			if (mousePos.DistanceTo(_pressMousePosition) >= DragThreshold)
				StartDrag();
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
			OpenBackpack();
	}

	public void OpenBackpackCraftingMode()
	{
		_backpackCraftingPanel.Visible = true;
		_workbenchCraftingPanel.Visible = false;

		_craftingGrid = _backpackCraftingGrid;
		_craftOutputSlotUi = _backpackCraftOutputSlotUi;
		_craftingContainer = _backpackCraftingContainer;

		_craftingSlotUis.Clear();
		_craftingSlotUis.AddRange(_backpackCraftingSlotUis);
		Refresh();
	}
	
	public void OpenBackpack()
	{
		OpenUI();
		OpenBackpackCraftingMode();
	}
	
	public void OpenUI()
	{
		Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void OpenWorkbench()
	{
		OpenUI();
		OpenWorkbenchCraftingMode();
	}

	public void OpenWorkbenchCraftingMode()
	{
		_backpackCraftingPanel.Visible = false;
		_workbenchCraftingPanel.Visible = true;

		_craftingGrid = _workbenchCraftingGrid;
		_craftOutputSlotUi = _workbenchCraftOutputSlotUi;
		_craftingContainer = _workbenchCraftingContainer;

		_craftingSlotUis.Clear();
		_craftingSlotUis.AddRange(_workbenchCraftingSlotUis);
		
		Refresh();
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
		CancelDragAndReturnHeldStack();
		ReturnCraftingInputsToInventory();
	}
	
	private void ReturnCraftingInputsToInventory()
	{
		if (_craftingContainer == null || _inventory == null)
			return;

		for (int i = 0; i < _craftingContainer.InputSlots.Length; i++)
		{
			var slot = _craftingContainer.GetInputSlot(i);
			if (slot == null || slot.IsEmpty || slot.Item == null)
				continue;

			_inventory.AddItem(slot.Item, slot.Count);
			slot.Clear();
		}

		_craftingContainer.RefreshOutput();
	}

	private void CacheSlotReferences()
	{
		_backpackSlotUis.Clear();
		_hotbarSlotUis.Clear();
		_backpackCraftingSlotUis.Clear();
		_workbenchCraftingSlotUis.Clear();
		_craftingSlotUis.Clear();

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

		foreach (Node child in _backpackCraftingGrid.GetChildren())
		{
			if (child is InventorySlotUI slotUi)
			{
				slotUi.SlotPressed += OnSlotPressed;
				slotUi.SlotHovered += OnSlotHovered;
				slotUi.SlotUnhovered += OnSlotUnhovered;
				_backpackCraftingSlotUis.Add(slotUi);
			}
		}

		foreach (Node child in _workbenchCraftingGrid.GetChildren())
		{
			if (child is InventorySlotUI slotUi)
			{
				slotUi.SlotPressed += OnSlotPressed;
				slotUi.SlotHovered += OnSlotHovered;
				slotUi.SlotUnhovered += OnSlotUnhovered;
				_workbenchCraftingSlotUis.Add(slotUi);
			}
		}
		

		if (_backpackCraftOutputSlotUi != null)
		{
			_backpackCraftOutputSlotUi.SlotPressed += OnSlotPressed;
			_backpackCraftOutputSlotUi.SlotHovered += OnSlotHovered;
			_backpackCraftOutputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_workbenchCraftOutputSlotUi != null)
		{
			_workbenchCraftOutputSlotUi.SlotPressed += OnSlotPressed;
			_workbenchCraftOutputSlotUi.SlotHovered += OnSlotHovered;
			_workbenchCraftOutputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}
	}

	private void Refresh()
	{
		if (_inventory == null)
			return;

		for (int i = 0; i < _backpackSlotUis.Count; i++)
		{
			var ui = _backpackSlotUis[i];
			int inventoryIndex = Inventory.HotbarSize + i;

			bool highlighted =
				_hoveredSlotUi == ui ||
				(_isDragging &&
				 _heldSourceRole == InventorySlotUI.SlotRole.Inventory &&
				 _heldSourceSlotIndex == ui.SlotIndex);

			ui.Setup(inventoryIndex, _inventory.GetSlot(inventoryIndex), highlighted);
		}

		for (int i = 0; i < _hotbarSlotUis.Count; i++)
		{
			var ui = _hotbarSlotUis[i];

			bool selected = i == _inventory.SelectedIndex;
			bool highlighted =
				_hoveredSlotUi == ui ||
				(_isDragging &&
				 _heldSourceRole == InventorySlotUI.SlotRole.Inventory &&
				 _heldSourceSlotIndex == ui.SlotIndex);

			ui.Setup(i, _inventory.GetSlot(i), selected || highlighted);
		}

		if (_craftingContainer != null)
		{
			for (int i = 0; i < _craftingSlotUis.Count; i++)
			{
				var ui = _craftingSlotUis[i];
				var slot = _craftingContainer.GetInputSlot(ui.CraftingSlotIndex);
				GD.Print($"REFRESH {ui.Name} craftIndex={ui.CraftingSlotIndex} slotNull={slot == null}");

				bool highlighted =
					_hoveredSlotUi == ui ||
					(_isDragging &&
					 _heldSourceRole == InventorySlotUI.SlotRole.CraftingInput &&
					 _heldSourceCraftIndex == ui.CraftingSlotIndex);

				ui.Setup(ui.CraftingSlotIndex, slot, highlighted);
			}

			if (_craftOutputSlotUi != null)
			{
				bool outputHighlighted = _hoveredSlotUi == _craftOutputSlotUi;
				_craftOutputSlotUi.Setup(-1, _craftingContainer.OutputPreviewSlot, outputHighlighted);
			}
		}

		UpdateDraggedLabel();
	}

	private void OnSlotPressed(InventorySlotUI slotUi)
	{
		if (HasHeldStack())
			return;

		_pressedSlotUi = slotUi;
		_pressedSlotIndex = slotUi.SlotIndex;
		_pressMousePosition = GetGlobalMousePosition();
	}

	private void OnSlotHovered(InventorySlotUI slotUi)
	{
		_hoveredSlotUi = slotUi;
		_hoveredSlotIndex = slotUi.SlotIndex;

		if (_isDragging)
			Refresh();
	}

	private void OnSlotUnhovered(InventorySlotUI slotUi)
	{
		if (_hoveredSlotUi == slotUi)
		{
			_hoveredSlotUi = null;
			_hoveredSlotIndex = -1;
		}

		if (_isDragging)
			Refresh();
	}

	private void StartDrag()
	{
		if (_pressedSlotUi == null || HasHeldStack())
			return;

		switch (_pressedSlotUi.Role)
		{
			case InventorySlotUI.SlotRole.Inventory:
				StartDragFromInventorySlot(_pressedSlotUi.SlotIndex);
				break;

			case InventorySlotUI.SlotRole.CraftingInput:
				StartDragFromCraftingInput(_pressedSlotUi.CraftingSlotIndex);
				break;

			case InventorySlotUI.SlotRole.CraftingOutput:
				StartDragFromCraftingOutput();
				break;
		}
	}
	
	private void StartDragFromCraftingInput(int craftIndex)
	{
		if (_craftingContainer == null)
			return;
			
		_heldSourceRole = InventorySlotUI.SlotRole.CraftingInput;
		_heldSourceCraftIndex = craftIndex;

		var slot = _craftingContainer.GetInputSlot(craftIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? DragMode.SingleItem :
					shiftHeld ? DragMode.HalfStack :
					DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == DragMode.HalfStack)
			amountToTake = Mathf.CeilToInt(slot.Count / 2.0f);

		ItemDefinition item = slot.Item;
		int removed = slot.RemoveAmount(amountToTake);

		if (removed <= 0)
			return;

		_heldItem = item;
		_heldCount = removed;
		_heldSourceSlotIndex = craftIndex;

		_craftingContainer.RefreshOutput();

		_pressedSlotIndex = -1;
		_isDragging = true;

		Refresh();
	}

	
	private void StartDragFromInventorySlot(int slotIndex)
	{
		if (_inventory == null)
			return;
			
		_heldSourceRole = InventorySlotUI.SlotRole.Inventory;
		_heldSourceCraftIndex = -1;

		var slot = _inventory.GetSlot(slotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? DragMode.SingleItem :
					shiftHeld ? DragMode.HalfStack :
					DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == DragMode.HalfStack)
			amountToTake = Mathf.CeilToInt(slot.Count / 2.0f);

		ItemDefinition item = slot.Item;
		int removed = slot.RemoveAmount(amountToTake);

		if (removed <= 0)
			return;

		_heldItem = item;
		_heldCount = removed;
		_heldSourceSlotIndex = slotIndex;

		_pressedSlotIndex = -1;
		_isDragging = true;

		Refresh();
	}
	
	private void StartDragFromCraftingOutput()
	{
		if (_craftingContainer == null || !_craftingContainer.HasValidRecipe())
			return;

		var output = _craftingContainer.OutputPreviewSlot;
		if (output == null || output.IsEmpty || output.Item == null)
			return;

		_dragMode = DragMode.FullStack;
		_heldItem = output.Item;
		_heldCount = output.Count;

		_heldSourceRole = InventorySlotUI.SlotRole.CraftingOutput;
		_heldSourceSlotIndex = -1;
		_heldSourceCraftIndex = -1;

		_pressedSlotIndex = -1;
		_isDragging = true;

		Refresh();
	}


	private void FinishDrag()
	{
		if (!_isDragging)
			return;

		if (_hoveredSlotUi == null)
		{
			CancelDragAndReturnHeldStack();
			return;
		}

		switch (_hoveredSlotUi.Role)
		{
			case InventorySlotUI.SlotRole.Inventory:
				TryPlaceHeldIntoInventorySlot(_hoveredSlotUi.SlotIndex);
				break;

			case InventorySlotUI.SlotRole.CraftingInput:
				TryPlaceHeldIntoCraftingInput(_hoveredSlotUi.CraftingSlotIndex);
				break;

			case InventorySlotUI.SlotRole.CraftingOutput:
				CancelDragAndReturnHeldStack();
				break;
		}

		if (_heldCount <= 0)
			ClearHeldStackState();

		_isMouseHeld = false;
		_isDragging = false;
		_pressedSlotIndex = -1;
		_hoveredSlotIndex = -1;
		_pressedSlotUi = null;
		_hoveredSlotUi = null;

		Refresh();
	}
	
	private void TryPlaceHeldIntoCraftingInput(int craftIndex)
	{
		if (!HasHeldStack() || _craftingContainer == null)
			return;

		// crafted output cannot be placed back into crafting grid
		if (IsHoldingCraftOutput())
		{
			CancelCraftOutputDrag();
			return;
		}

		var target = _craftingContainer.GetInputSlot(craftIndex);
		if (target == null)
			return;

		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			_craftingContainer.RefreshOutput();
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			target.Count += _heldCount;
			_heldCount = 0;
			_craftingContainer.RefreshOutput();
			return;
		}

		if (_dragMode == DragMode.FullStack)
		{
			var tempItem = target.Item;
			int tempCount = target.Count;

			target.SetItem(_heldItem, _heldCount);

			// if source was inventory, restore swapped item there
			// if source was crafting input, restore there
			RestoreSwapToSource(tempItem, tempCount);

			_heldCount = 0;
			_craftingContainer.RefreshOutput();
			return;
		}

		ReturnHeldStackToSource();
	}

	private void ReturnHeldStackToSource()
	{
		if (!HasHeldStack())
			return;

		if (_heldSourceRole == InventorySlotUI.SlotRole.Inventory)
		{
			if (_inventory == null || _heldSourceSlotIndex < 0)
				return;

			var source = _inventory.GetSlot(_heldSourceSlotIndex);
			if (source == null)
				return;

			if (source.IsEmpty)
				source.SetItem(_heldItem, _heldCount);
			else if (source.CanStackWith(_heldItem))
				source.Count += _heldCount;
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.CraftingInput)
		{
			if (_craftingContainer == null || _heldSourceCraftIndex < 0)
				return;

			var source = _craftingContainer.GetInputSlot(_heldSourceCraftIndex);
			if (source == null)
				return;

			if (source.IsEmpty)
				source.SetItem(_heldItem, _heldCount);
			else if (source.CanStackWith(_heldItem))
				source.Count += _heldCount;

			_craftingContainer.RefreshOutput();
		}

		ClearHeldStackState();
	}

	private void CancelDragAndReturnHeldStack()
	{
		ReturnHeldStackToSource();

		_pressedSlotIndex = -1;
		_hoveredSlotIndex = -1;
		_isMouseHeld = false;
		_isDragging = false;

		UpdateDraggedLabel();
		Refresh();
	}

	private void ClearPressedState()
	{
		_pressedSlotIndex = -1;
		_pressedSlotUi = null;
	}

	private bool HasHeldStack()
	{
		return _heldItem != null && _heldCount > 0;
	}

	private void ClearHeldStackState()
	{
		_heldItem = null;
		_heldCount = 0;
		_heldSourceSlotIndex = -1;
		_heldSourceCraftIndex = -1;
		_heldSourceRole = InventorySlotUI.SlotRole.Inventory;
		_dragMode = DragMode.None;
	}
	
	private void RestoreSwapToSource(ItemDefinition item, int count)
	{
		if (item == null || count <= 0)
			return;

		if (_heldSourceRole == InventorySlotUI.SlotRole.Inventory)
		{
			var source = _inventory?.GetSlot(_heldSourceSlotIndex);
			if (source != null)
				source.SetItem(item, count);
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.CraftingInput)
		{
			var source = _craftingContainer?.GetInputSlot(_heldSourceCraftIndex);
			if (source != null)
				source.SetItem(item, count);

			_craftingContainer?.RefreshOutput();
		}
	}

	private void UpdateDraggedLabel()
	{
		if (_draggedItemLabel == null)
			return;

		if (!HasHeldStack())
		{
			_draggedItemLabel.Visible = false;
			_draggedItemLabel.Text = "";
			return;
		}

		_draggedItemLabel.Visible = true;
		_draggedItemLabel.Text = $"{_heldItem.DisplayName} x{_heldCount}";
	}
	
	private void TryPlaceHeldIntoInventorySlot(int slotIndex)
	{
		if (!HasHeldStack() || _inventory == null)
			return;

		var target = _inventory.GetSlot(slotIndex);
		if (target == null)
			return;

		// Crafted output: only place into empty inventory slot, no swapping, no stacking.
		if (IsHoldingCraftOutput())
		{
			if (!target.IsEmpty)
			{
				CancelCraftOutputDrag();
				return;
			}

			target.SetItem(_heldItem, _heldCount);

			bool committed = _craftingContainer != null && _craftingContainer.TryCommitCraft();
			if (!committed)
			{
				target.Clear();
				CancelCraftOutputDrag();
				return;
			}

			ClearHeldStackState();
			return;
		}

		// Normal inventory behavior
		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			target.Count += _heldCount;
			_heldCount = 0;
			return;
		}

		if (_dragMode == DragMode.FullStack)
		{
			var tempItem = target.Item;
			int tempCount = target.Count;

			target.SetItem(_heldItem, _heldCount);
			RestoreSwapToSource(tempItem, tempCount);

			_heldCount = 0;
			return;
		}

		ReturnHeldStackToSource();
	}

	private bool IsHoldingCraftOutput()
	{
		return _heldSourceRole == InventorySlotUI.SlotRole.CraftingOutput;
	}

	private void CancelCraftOutputDrag()
	{
		ClearHeldStackState();
		_isDragging = false;
		_isMouseHeld = false;
		Refresh();
	}
	
	private bool IsHeldFromCraftingInput()
	{
		return _heldSourceSlotIndex >= 0 &&
			   _heldSourceSlotIndex < _craftingContainer.InputSlots.Length &&
			   _pressedSlotUi != null &&
			   _pressedSlotUi.Role == InventorySlotUI.SlotRole.CraftingInput;
	}

}
