using Godot;
using System;

public class InventoryDragController
{
	
	public InventorySlotUI.SlotRole _heldSourceRole = InventorySlotUI.SlotRole.Inventory;
	public int _heldSourceCraftIndex = -1;
	public const int CraftOutputSourceMarker = -999;
	
	public InventorySlotUI _hoveredSlotUi = null;
	public InventorySlotUI _pressedSlotUi = null;
	
	public int _pressedSlotIndex = -1;
	public int _hoveredSlotIndex = -1;
	public int _heldSourceSlotIndex = -1;
	
	public bool _isMouseHeld = false;
	public bool _isDragging = false;

	public Vector2 _pressMousePosition;
	public const float DragThreshold = 12f;

	public ItemDefinition _heldItem = null;
	public int _heldCount = 0;
	public BackpackUI.DragMode _dragMode = BackpackUI.DragMode.None;
	
	public readonly BackpackUI _ui;
	public readonly Label _draggedItemLabel;
	public readonly Control _draggedItemPreview;
	public readonly TextureRect _draggedItemIcon;
	public readonly Label _draggedItemCountLabel;
	
	public InventorySlotUI HoveredSlotUi => _hoveredSlotUi;
	public bool IsDragging => _isDragging;
	public InventorySlotUI.SlotRole HeldSourceRole => _heldSourceRole;
	public int HeldSourceSlotIndex => _heldSourceSlotIndex;
	public int HeldSourceCraftIndex => _heldSourceCraftIndex;
	
	public InventoryDragController(BackpackUI ui)
	{
		_ui = ui;	
		_draggedItemLabel = ui._draggedItemLabel;
		_draggedItemPreview = ui._draggedItemPreview;
		_draggedItemIcon = ui._draggedItemIcon;
		_draggedItemCountLabel = ui._draggedItemCountLabel;
	}
	
	public void Process(double delta)
	{
		if (_isMouseHeld && !_isDragging && _pressedSlotUi != null)
		{
			Vector2 mousePos = _ui.GetGlobalMousePosition();
			if (mousePos.DistanceTo(_pressMousePosition) >= DragThreshold)
				StartDrag();
		}

		Vector2 dragPos = _ui.GetGlobalMousePosition() + new Vector2(16, 16);

		if (_draggedItemLabel.Visible)
			_draggedItemLabel.Position = dragPos;

		if (_draggedItemPreview.Visible)
			_draggedItemPreview.Position = dragPos;
	}

	public void HandleInput(InputEvent @event)
	{
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
	
	
	public void OnSlotPressed(InventorySlotUI slotUi)
	{
		if (HasHeldStack())
			return;

		_pressedSlotUi = slotUi;
		_pressedSlotIndex = slotUi.SlotIndex;
		_pressMousePosition = _ui.GetGlobalMousePosition();
	}

	public void OnSlotHovered(InventorySlotUI slotUi)
	{
		_hoveredSlotUi = slotUi;
		_hoveredSlotIndex = slotUi.SlotIndex;

		if (_isDragging)
			_ui.Refresh();
	}

	public void OnSlotUnhovered(InventorySlotUI slotUi)
	{
		if (_hoveredSlotUi == slotUi)
		{
			_hoveredSlotUi = null;
			_hoveredSlotIndex = -1;
		}

		if (_isDragging)
			_ui.Refresh();
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
				
			case InventorySlotUI.SlotRole.ChestStorage:
				StartDragFromChestSlot(_pressedSlotUi.SlotIndex);
				break;
				
			case InventorySlotUI.SlotRole.ProcessingInput:
				StartDragFromProcessingSlot(_ui._activeProcessingContainer?.InputSlot, InventorySlotUI.SlotRole.ProcessingInput);
				break;

			case InventorySlotUI.SlotRole.ProcessingFuel:
				StartDragFromProcessingSlot(_ui._activeProcessingContainer?.FuelSlot, InventorySlotUI.SlotRole.ProcessingFuel);
				break;

			case InventorySlotUI.SlotRole.ProcessingOutput:
				StartDragFromProcessingSlot(_ui._activeProcessingContainer?.OutputSlot, InventorySlotUI.SlotRole.ProcessingOutput);
				break;
		}
	}
	
	private void StartDragFromCraftingInput(int craftIndex)
	{
		if (_ui._craftingContainer == null)
			return;
			
		_heldSourceRole = InventorySlotUI.SlotRole.CraftingInput;
		_heldSourceCraftIndex = craftIndex;

		var slot = _ui._craftingContainer.GetInputSlot(craftIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? BackpackUI.DragMode.SingleItem :
					shiftHeld ? BackpackUI.DragMode.HalfStack :
					BackpackUI.DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == BackpackUI.DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == BackpackUI.DragMode.HalfStack)
			amountToTake = Mathf.CeilToInt(slot.Count / 2.0f);

		ItemDefinition item = slot.Item;
		int removed = slot.RemoveAmount(amountToTake);

		if (removed <= 0)
			return;

		_heldItem = item;
		_heldCount = removed;
		_heldSourceSlotIndex = craftIndex;

		_ui._craftingContainer.RefreshOutput();

		_pressedSlotIndex = -1;
		_isDragging = true;

		_ui.Refresh();
	}

	
	private void StartDragFromInventorySlot(int slotIndex)
	{
		if (_ui._inventory == null)
			return;
			
		_heldSourceRole = InventorySlotUI.SlotRole.Inventory;
		_heldSourceCraftIndex = -1;

		var slot = _ui._inventory.GetSlot(slotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? BackpackUI.DragMode.SingleItem :
					shiftHeld ? BackpackUI.DragMode.HalfStack :
					BackpackUI.DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == BackpackUI.DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == BackpackUI.DragMode.HalfStack)
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

		_ui.Refresh();
	}
	
	private void StartDragFromCraftingOutput()
	{
		if (_ui._craftingContainer == null || !_ui._craftingContainer.HasValidRecipe())
			return;

		var output = _ui._craftingContainer.OutputPreviewSlot;
		if (output == null || output.IsEmpty || output.Item == null)
			return;

		_dragMode = BackpackUI.DragMode.FullStack;
		_heldItem = output.Item;
		_heldCount = output.Count;

		_heldSourceRole = InventorySlotUI.SlotRole.CraftingOutput;
		_heldSourceSlotIndex = -1;
		_heldSourceCraftIndex = -1;

		_pressedSlotIndex = -1;
		_isDragging = true;

		_ui.Refresh();
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
				
			case InventorySlotUI.SlotRole.ChestStorage:
				TryPlaceHeldIntoChestSlot(_hoveredSlotUi.SlotIndex);
				break;

			case InventorySlotUI.SlotRole.CraftingOutput:
				CancelDragAndReturnHeldStack();
				break;

			case InventorySlotUI.SlotRole.ProcessingInput:
				TryPlaceHeldIntoProcessingInput();
				break;

			case InventorySlotUI.SlotRole.ProcessingFuel:
				TryPlaceHeldIntoProcessingFuel();
				break;

			case InventorySlotUI.SlotRole.ProcessingOutput:
				TryTakeProcessingOutput();
				break;

			default:
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

		_ui.Refresh();
	}
	
	private void TryPlaceHeldIntoCraftingInput(int craftIndex)
	{
		if (!HasHeldStack() || _ui._craftingContainer == null)
			return;

		// crafted output cannot be placed back into crafting grid
		if (IsHoldingCraftOutput())
		{
			CancelCraftOutputDrag();
			return;
		}

		var target = _ui._craftingContainer.GetInputSlot(craftIndex);
		if (target == null)
			return;

		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			_ui._craftingContainer.RefreshOutput();
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			int maxStack = target.Item.MaxStackSize;
			int spaceLeft = maxStack - target.Count;
			int toMove = Mathf.Min(spaceLeft, _heldCount);

			if (toMove > 0)
			{
				target.Count += toMove;
				_heldCount -= toMove;
			}

			_ui._craftingContainer.RefreshOutput();

			if (_heldCount <= 0)
				return;
		}

		if (_dragMode == BackpackUI.DragMode.FullStack)
		{
			var tempItem = target.Item;
			int tempCount = target.Count;

			target.SetItem(_heldItem, _heldCount);

			// if source was inventory, restore swapped item there
			// if source was crafting input, restore there
			RestoreSwapToSource(tempItem, tempCount);

			_heldCount = 0;
			_ui._craftingContainer.RefreshOutput();
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
			if (_ui._inventory == null || _heldSourceSlotIndex < 0)
				return;

			var source = _ui._inventory.GetSlot(_heldSourceSlotIndex);
			if (source == null)
				return;

			if (source.IsEmpty)
				source.SetItem(_heldItem, _heldCount);
			else if (source.CanStackWith(_heldItem))
				source.Count += _heldCount;
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.CraftingInput)
		{
			if (_ui._craftingContainer == null || _heldSourceCraftIndex < 0)
				return;

			var source = _ui._craftingContainer.GetInputSlot(_heldSourceCraftIndex);
			if (source == null)
				return;

			if (source.IsEmpty)
				source.SetItem(_heldItem, _heldCount);
			else if (source.CanStackWith(_heldItem))
				source.Count += _heldCount;

			_ui._craftingContainer.RefreshOutput();
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.ProcessingInput)
		{
			var source = _ui._activeProcessingContainer?.InputSlot;
			if (source != null)
			{
				if (source.IsEmpty)
					source.SetItem(_heldItem, _heldCount);
				else if (source.CanStackWith(_heldItem))
					source.Count += _heldCount;
			}
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.ProcessingFuel)
		{
			var source = _ui._activeProcessingContainer?.FuelSlot;
			if (source != null)
			{
				if (source.IsEmpty)
					source.SetItem(_heldItem, _heldCount);
				else if (source.CanStackWith(_heldItem))
					source.Count += _heldCount;
			}
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.ProcessingOutput)
		{
			var source = _ui._activeProcessingContainer?.OutputSlot;
			if (source != null)
			{
				if (source.IsEmpty)
					source.SetItem(_heldItem, _heldCount);
				else if (source.CanStackWith(_heldItem))
					source.Count += _heldCount;
			}
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.ChestStorage)
		{
			if (_ui._activeStorageContainer == null || _heldSourceSlotIndex < 0)
				return;

			var source = _ui._activeStorageContainer.GetSlot(_heldSourceSlotIndex);
			if (source == null)
				return;

			if (source.IsEmpty)
				source.SetItem(_heldItem, _heldCount);
			else if (source.CanStackWith(_heldItem))
				source.Count += _heldCount;

			_ui._activeStorageContainer.EmitSignal(StorageContainer.SignalName.StorageChanged);
		}

		ClearHeldStackState();
	}

	public void CancelDragAndReturnHeldStack()
	{
		ReturnHeldStackToSource();

		_pressedSlotIndex = -1;
		_hoveredSlotIndex = -1;
		_isMouseHeld = false;
		_isDragging = false;

		UpdateDraggedLabel();
		_ui.Refresh();
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
		_dragMode = BackpackUI.DragMode.None;
	}
	
	public void UpdateDraggedLabel()
	{
		if (_draggedItemLabel == null || _draggedItemPreview == null || _draggedItemIcon == null || _draggedItemCountLabel == null)
			return;

		if (!HasHeldStack())
		{
			_draggedItemPreview.Visible = false;
			_draggedItemLabel.Visible = false;

			_draggedItemLabel.Text = "";
			_draggedItemIcon.Texture = null;
			_draggedItemCountLabel.Text = "";
			return;
		}

		Texture2D icon = _heldItem.Icon;
		bool hasIcon = icon != null;

		if (hasIcon)
		{
			_draggedItemPreview.Visible = true;
			_draggedItemLabel.Visible = false;

			_draggedItemIcon.Texture = icon;
			_draggedItemCountLabel.Text = _heldCount > 1 ? $"x{_heldCount}" : "";
		}
		else
		{
			_draggedItemPreview.Visible = false;
			_draggedItemLabel.Visible = true;
			_draggedItemLabel.Text = $"{_heldItem.DisplayName} x{_heldCount}";
		}
	}
	
	private void TryPlaceHeldIntoInventorySlot(int slotIndex)
	{
		if (!HasHeldStack() || _ui._inventory == null)
			return;

		var target = _ui._inventory.GetSlot(slotIndex);
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

			bool committed = _ui._craftingContainer != null && _ui._craftingContainer.TryCommitCraft();
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
			int maxStack = target.Item.MaxStackSize;
			int spaceLeft = maxStack - target.Count;
			int toMove = Mathf.Min(spaceLeft, _heldCount);

			if (toMove > 0)
			{
				target.Count += toMove;
				_heldCount -= toMove;
			}

			if (_heldCount <= 0)
				return;
		}

		if (_dragMode == BackpackUI.DragMode.FullStack)
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
		_ui.Refresh();
	}
	
	
	private void StartDragFromProcessingSlot(InventorySlot slot, InventorySlotUI.SlotRole sourceRole)
	{
		if (_ui._activeProcessingContainer == null || slot == null || slot.IsEmpty || slot.Item == null)
			return;

		_heldSourceRole = sourceRole;
		_heldSourceCraftIndex = -1;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? BackpackUI.DragMode.SingleItem :
					shiftHeld ? BackpackUI.DragMode.HalfStack :
					BackpackUI.DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == BackpackUI.DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == BackpackUI.DragMode.HalfStack)
			amountToTake = Mathf.CeilToInt(slot.Count / 2.0f);

		ItemDefinition item = slot.Item;
		int removed = slot.RemoveAmount(amountToTake);

		if (removed <= 0)
			return;

		_heldItem = item;
		_heldCount = removed;
		_heldSourceSlotIndex = -1;
		_pressedSlotIndex = -1;
		_isDragging = true;

		_ui.Refresh();
	}
	
	private void TryPlaceHeldIntoProcessingInput()
	{
		if (!HasHeldStack() || _ui._activeProcessingContainer == null || _heldItem == null)
			return;

		// Must be valid for this station.
		if (!_ui._activeProcessingContainer.CanAcceptInput(_heldItem))
		{
			ReturnHeldStackToSource();
			return;
		}

		var target = _ui._activeProcessingContainer.InputSlot;
		if (target == null)
		{
			ReturnHeldStackToSource();
			return;
		}

		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			int maxStack = target.Item.MaxStackSize;
			int spaceLeft = maxStack - target.Count;
			int toMove = Mathf.Min(spaceLeft, _heldCount);

			if (toMove > 0)
			{
				target.Count += toMove;
				_heldCount -= toMove;
			}

			if (_heldCount <= 0)
				return;
		}

		ReturnHeldStackToSource();
	}

	private void TryPlaceHeldIntoProcessingFuel()
	{
		if (!HasHeldStack() || _ui._activeProcessingContainer == null || _heldItem == null)
			return;

		if (!_ui._activeProcessingContainer.CanAcceptFuel(_heldItem))
		{
			ReturnHeldStackToSource();
			return;
		}

		var target = _ui._activeProcessingContainer.FuelSlot;
		if (target == null)
		{
			ReturnHeldStackToSource();
			return;
		}

		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			int maxStack = target.Item.MaxStackSize;
			int spaceLeft = maxStack - target.Count;
			int toMove = Mathf.Min(spaceLeft, _heldCount);

			if (toMove > 0)
			{
				target.Count += toMove;
				_heldCount -= toMove;
			}

			if (_heldCount <= 0)
				return;
		}

		ReturnHeldStackToSource();
	}


	private void TryPlaceHeldIntoChestSlot(int slotIndex)
	{
		if (!HasHeldStack() || _ui._activeStorageContainer == null)
			return;

		if (IsHoldingCraftOutput())
		{
			CancelCraftOutputDrag();
			return;
		}

		var target = _ui._activeStorageContainer.GetSlot(slotIndex);
		if (target == null)
			return;

		if (target.IsEmpty)
		{
			target.SetItem(_heldItem, _heldCount);
			_heldCount = 0;
			_ui._activeStorageContainer.EmitSignal(StorageContainer.SignalName.StorageChanged);
			return;
		}

		if (target.CanStackWith(_heldItem))
		{
			int maxStack = target.Item.MaxStackSize;
			int spaceLeft = maxStack - target.Count;
			int toMove = Mathf.Min(spaceLeft, _heldCount);

			if (toMove > 0)
			{
				target.Count += toMove;
				_heldCount -= toMove;
			}

			_ui._activeStorageContainer.EmitSignal(StorageContainer.SignalName.StorageChanged);

			if (_heldCount <= 0)
				return;
		}

		if (_dragMode == BackpackUI.DragMode.FullStack)
		{
			var tempItem = target.Item;
			int tempCount = target.Count;

			target.SetItem(_heldItem, _heldCount);
			RestoreSwapToSource(tempItem, tempCount);

			_heldCount = 0;
			_ui._activeStorageContainer.EmitSignal(StorageContainer.SignalName.StorageChanged);
			return;
		}

		ReturnHeldStackToSource();
	}
	
	
	private void StartDragFromChestSlot(int slotIndex)
	{
		if (_ui._activeStorageContainer == null)
			return;

		_heldSourceRole = InventorySlotUI.SlotRole.ChestStorage;
		_heldSourceCraftIndex = -1;

		var slot = _ui._activeStorageContainer.GetSlot(slotIndex);
		if (slot == null || slot.IsEmpty || slot.Item == null)
			return;

		bool shiftHeld = Input.IsKeyPressed(Key.Shift);
		bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

		_dragMode = ctrlHeld ? BackpackUI.DragMode.SingleItem :
					shiftHeld ? BackpackUI.DragMode.HalfStack :
					BackpackUI.DragMode.FullStack;

		int amountToTake = slot.Count;

		if (_dragMode == BackpackUI.DragMode.SingleItem)
			amountToTake = 1;
		else if (_dragMode == BackpackUI.DragMode.HalfStack)
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

		_ui.Refresh();
	}
	
	private void RestoreSwapToSource(ItemDefinition item, int count)
	{
		if (item == null || count <= 0)
			return;

		if (_heldSourceRole == InventorySlotUI.SlotRole.Inventory)
		{
			var source = _ui._inventory?.GetSlot(_heldSourceSlotIndex);
			if (source != null)
				source.SetItem(item, count);
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.CraftingInput)
		{
			var source = _ui._craftingContainer?.GetInputSlot(_heldSourceCraftIndex);
			if (source != null)
				source.SetItem(item, count);

			_ui._craftingContainer?.RefreshOutput();
		}
		else if (_heldSourceRole == InventorySlotUI.SlotRole.ChestStorage)
		{
			var source = _ui._activeStorageContainer?.GetSlot(_heldSourceSlotIndex);
			if (source != null)
			{
				source.SetItem(item, count);
				_ui._activeStorageContainer.EmitSignal(StorageContainer.SignalName.StorageChanged);
			}
		}
	}
	
	private void TryTakeProcessingOutput()
	{
		if (_ui._activeProcessingContainer == null)
		{
			CancelDragAndReturnHeldStack();
			return;
		}

		// Never allow dropping held items into output.
		if (HasHeldStack())
		{
			CancelDragAndReturnHeldStack();
			return;
		}

		if (_ui._activeProcessingContainer.OutputSlot == null || _ui._activeProcessingContainer.OutputSlot.IsEmpty)
			return;

		StartDragFromProcessingSlot(
			_ui._activeProcessingContainer.OutputSlot,
			InventorySlotUI.SlotRole.ProcessingOutput
		);
	}
	
}
