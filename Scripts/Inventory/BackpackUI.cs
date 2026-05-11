using Godot;
using System.Collections.Generic;

public partial class BackpackUI : Control
{
	
	private bool _adminMode = false;

	[Export] public NodePath AdminItemPanelPath;
	[Export] public NodePath AdminItemGridPath;
	[Export] public PackedScene InventorySlotScene;
	[Export] public NodePath ItemDatabasePath;
	[Export] public int AdminColumns = 3;
	[Export] public int AdminPanelPadding = 12;
	[Export] public int AdminSlotSpacing = 6;
	[Export] public int AdminBottomMargin = 120; // keeps it above hotbar
	[Export] public int AdminTopMargin = 20;

	private Control _adminItemPanel;
	private GridContainer _adminItemGrid;
	public ItemDatabase _itemDatabase;
	
	[Export] public NodePath DraggedItemPreviewPath;
	[Export] public NodePath DraggedItemIconPath;
	[Export] public NodePath DraggedItemCountLabelPath;
	
	public Control _draggedItemPreview;
	public TextureRect _draggedItemIcon;
	public Label _draggedItemCountLabel;

	[Export] public NodePath BackpackCraftingPanelPath;
	[Export] public NodePath BackpackCraftingGridPath;
	[Export] public NodePath BackpackCraftingOutputSlotPath;
	[Export] public NodePath BackpackCraftingContainerPath;

	[Export] public NodePath WorkbenchCraftingPanelPath;
	[Export] public NodePath WorkbenchCraftingGridPath;
	[Export] public NodePath WorkbenchCraftingOutputSlotPath;
	[Export] public NodePath WorkbenchCraftingContainerPath;
	
	[Export] public NodePath CampfirePanelPath;
	[Export] public NodePath CampfireInputSlotPath;
	[Export] public NodePath CampfireFuelSlotPath;
	[Export] public NodePath CampfireOutputSlotPath;

	[Export] public NodePath FurnacePanelPath;
	[Export] public NodePath FurnaceInputSlotPath;
	[Export] public NodePath FurnaceFuelSlotPath;
	[Export] public NodePath FurnaceOutputSlotPath;
	
	public Control _backpackCraftingPanel;
	public GridContainer _backpackCraftingGrid;
	public InventorySlotUI _backpackCraftOutputSlotUi;
	public readonly List<InventorySlotUI> _backpackCraftingSlotUis = new();
	public CraftingContainer _backpackCraftingContainer;

	public Control _workbenchCraftingPanel;
	public GridContainer _workbenchCraftingGrid;
	public InventorySlotUI _workbenchCraftOutputSlotUi;
	public readonly List<InventorySlotUI> _workbenchCraftingSlotUis = new();
	public CraftingContainer _workbenchCraftingContainer;

	public GridContainer _craftingGrid;
	public InventorySlotUI _craftOutputSlotUi;
	public readonly List<InventorySlotUI> _craftingSlotUis = new();
	public CraftingContainer _craftingContainer;
	public ProcessingContainer _activeProcessingContainer;
	
	public Control _campfirePanel;
	public InventorySlotUI _campfireInputSlotUi;
	public InventorySlotUI _campfireFuelSlotUi;
	public InventorySlotUI _campfireOutputSlotUi;

	public Control _furnacePanel;
	public InventorySlotUI _furnaceInputSlotUi;
	public InventorySlotUI _furnaceFuelSlotUi;
	public InventorySlotUI _furnaceOutputSlotUi;
	
	public ProcessingContainer _subscribedProcessingContainer;


	[Export] public NodePath BackpackGridPath;
	[Export] public NodePath HotbarRowPath;
	[Export] public NodePath DraggedItemLabelPath;

	public enum DragMode
	{
		None,
		FullStack,
		HalfStack,
		SingleItem
	}
	
	public enum UiMode
	{
		BackpackCrafting,
		WorkbenchCrafting,
		CampfireProcessing,
		FurnaceProcessing,
		ChestStorage
	}
	
	public InventoryDragController _dragController;
	public InventoryUiModeController _modeController;

	public UiMode _currentMode = UiMode.BackpackCrafting;

	
	public readonly Dictionary<string, Texture2D> _itemIcons = new();

	public GridContainer _backpackGrid;
	public HBoxContainer _hotbarRow;
	public Label _draggedItemLabel;

	public readonly List<InventorySlotUI> _backpackSlotUis = new();
	public readonly List<InventorySlotUI> _hotbarSlotUis = new();

	public Inventory _inventory;

	public bool IsOpen => Visible;
	
	[Export] public NodePath ChestGridPath;
	public GridContainer _chestGrid;
	public readonly List<InventorySlotUI> _chestSlotUis = new();
	public StorageContainer _activeStorageContainer;
	public Chest _activeChest;
	

	public override void _Ready()
	{
		_adminItemPanel = GetNode<Control>(AdminItemPanelPath);
		_adminItemGrid = GetNode<GridContainer>(AdminItemGridPath);
		_itemDatabase = GetNode<ItemDatabase>(ItemDatabasePath);
		_adminItemGrid.Columns = AdminColumns;
		_adminItemGrid.AddThemeConstantOverride("h_separation", AdminSlotSpacing);
		_adminItemGrid.AddThemeConstantOverride("v_separation", AdminSlotSpacing);
		_adminItemPanel.Visible = false;
			
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
		
		_campfirePanel = GetNode<Control>(CampfirePanelPath);
		_campfireInputSlotUi = GetNode<InventorySlotUI>(CampfireInputSlotPath);
		_campfireFuelSlotUi = GetNode<InventorySlotUI>(CampfireFuelSlotPath);
		_campfireOutputSlotUi = GetNode<InventorySlotUI>(CampfireOutputSlotPath);

		_furnacePanel = GetNode<Control>(FurnacePanelPath);
		_furnaceInputSlotUi = GetNode<InventorySlotUI>(FurnaceInputSlotPath);
		_furnaceFuelSlotUi = GetNode<InventorySlotUI>(FurnaceFuelSlotPath);
		
		_furnaceOutputSlotUi = GetNode<InventorySlotUI>(FurnaceOutputSlotPath);
		_draggedItemPreview = GetNode<Control>(DraggedItemPreviewPath);
		_draggedItemIcon = GetNode<TextureRect>(DraggedItemIconPath);
		_draggedItemCountLabel = GetNode<Label>(DraggedItemCountLabelPath);
		
		_chestGrid = GetNode<GridContainer>(ChestGridPath);

		_draggedItemPreview.Visible = false;
		_draggedItemLabel.Visible = false;
		
		_chestGrid.Visible = false;
		_campfirePanel.Visible = false;
		_furnacePanel.Visible = false;
		
		_draggedItemIcon.MouseFilter = MouseFilterEnum.Ignore;
		_draggedItemCountLabel.MouseFilter = MouseFilterEnum.Ignore;
		_draggedItemLabel.MouseFilter = MouseFilterEnum.Ignore;
		_draggedItemPreview.MouseFilter = MouseFilterEnum.Ignore;

		_dragController = new InventoryDragController(this);
		_modeController = new InventoryUiModeController(this);

		CacheSlotReferences();
		_modeController.OpenBackpackCraftingMode();
		
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

		if (!IsNodeReady())
		{
			CallDeferred(nameof(Initialize), inventory);
			return;
		}

		Refresh();
	}

	private void SetupSlotVisual(InventorySlotUI ui, int slotIndex, InventorySlot slot, bool highlighted)
	{
		if (ui == null)
			return;

		ui.SetSlotIndex(slotIndex);

		string itemId = "";
		int count = 0;
		Texture2D icon = null;

		if (slot != null && !slot.IsEmpty && slot.Item != null)
		{
			itemId = slot.Item.ItemId;
			count = slot.Count;
			icon = slot.Item.Icon;
		}

		ui.SetItemVisual(itemId, count, icon);
		
		//if (!string.IsNullOrEmpty(itemId))
			//GD.Print($"slot={slotIndex}, item={itemId}, iconFound={icon != null}, uiName={ui.Name}");
		// Keep your highlight/selection visuals
		ui.ButtonPressed = highlighted;
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		_dragController.Process(delta);
	
	}
	
	private void UpdateAdminPanelVisibility()
	{
		bool shouldShow =
			_adminMode &&
			Visible &&
			_currentMode == UiMode.BackpackCrafting;

		if (_adminItemPanel != null)
			_adminItemPanel.Visible = shouldShow;

		if (shouldShow && _adminItemGrid.GetChildCount() == 0)
			RefreshAdminItemPanel();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("admin_toggle"))
		{
			_adminMode = !_adminMode;

			GD.Print(_adminMode ? "Admin enabled" : "Admin disabled");

			UpdateAdminPanelVisibility();

			GetViewport().SetInputAsHandled();
			return;
		}

		if (!Visible)
			return;

		if (@event.IsActionPressed("toggle_inventory"))
		{
			_modeController.Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		_dragController.HandleInput(@event);
	}

	public void Toggle()
	{
		if (Visible)
			_modeController.Close();
		else
			_modeController.OpenBackpack();
			
		UpdateAdminPanelVisibility();
	}

	
	public void ReturnCraftingInputsToInventory()
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
		_chestSlotUis.Clear();
		
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
		
		foreach (Node child in _chestGrid.GetChildren())
		{
			if (child is InventorySlotUI slotUi)
			{
				slotUi.SlotPressed += OnSlotPressed;
				slotUi.SlotHovered += OnSlotHovered;
				slotUi.SlotUnhovered += OnSlotUnhovered;
				_chestSlotUis.Add(slotUi);
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
		
		if (_campfireInputSlotUi != null)
		{
			_campfireInputSlotUi.SlotPressed += OnSlotPressed;
			_campfireInputSlotUi.SlotHovered += OnSlotHovered;
			_campfireInputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_campfireFuelSlotUi != null)
		{
			_campfireFuelSlotUi.SlotPressed += OnSlotPressed;
			_campfireFuelSlotUi.SlotHovered += OnSlotHovered;
			_campfireFuelSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_campfireOutputSlotUi != null)
		{
			_campfireOutputSlotUi.SlotPressed += OnSlotPressed;
			_campfireOutputSlotUi.SlotHovered += OnSlotHovered;
			_campfireOutputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_furnaceInputSlotUi != null)
		{
			_furnaceInputSlotUi.SlotPressed += OnSlotPressed;
			_furnaceInputSlotUi.SlotHovered += OnSlotHovered;
			_furnaceInputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_furnaceFuelSlotUi != null)
		{
			_furnaceFuelSlotUi.SlotPressed += OnSlotPressed;
			_furnaceFuelSlotUi.SlotHovered += OnSlotHovered;
			_furnaceFuelSlotUi.SlotUnhovered += OnSlotUnhovered;
		}

		if (_furnaceOutputSlotUi != null)
		{
			_furnaceOutputSlotUi.SlotPressed += OnSlotPressed;
			_furnaceOutputSlotUi.SlotHovered += OnSlotHovered;
			_furnaceOutputSlotUi.SlotUnhovered += OnSlotUnhovered;
		}
	}

	public void Refresh()
	{
		
		if (_inventory == null)
			return;

		for (int i = 0; i < _backpackSlotUis.Count; i++)
		{
			var ui = _backpackSlotUis[i];
			int inventoryIndex = Inventory.HotbarSize + i;

			bool highlighted =
				_dragController.HoveredSlotUi == ui ||
				(_dragController.IsDragging &&
				 _dragController.HeldSourceRole == InventorySlotUI.SlotRole.Inventory &&
				 _dragController.HeldSourceSlotIndex == ui.SlotIndex);

			SetupSlotVisual(ui, inventoryIndex, _inventory.GetSlot(inventoryIndex), highlighted);
		}

		for (int i = 0; i < _hotbarSlotUis.Count; i++)
		{
			var ui = _hotbarSlotUis[i];

			bool selected = i == _inventory.SelectedIndex;
			bool highlighted =
				_dragController.HoveredSlotUi == ui ||
				(_dragController.IsDragging &&
				 _dragController.HeldSourceRole == InventorySlotUI.SlotRole.Inventory &&
				_dragController.HeldSourceSlotIndex == ui.SlotIndex);

			SetupSlotVisual(ui, i, _inventory.GetSlot(i), selected || highlighted);
		}

		if (_craftingContainer != null)
		{
			for (int i = 0; i < _craftingSlotUis.Count; i++)
			{
				var ui = _craftingSlotUis[i];
				var slot = _craftingContainer.GetInputSlot(ui.CraftingSlotIndex);

				bool highlighted =
					_dragController.HoveredSlotUi == ui ||
					(_dragController.IsDragging &&
					 _dragController.HeldSourceRole == InventorySlotUI.SlotRole.CraftingInput &&
					 _dragController.HeldSourceCraftIndex == ui.CraftingSlotIndex);

				SetupSlotVisual(ui, ui.CraftingSlotIndex, slot, highlighted);
			}

			if (_craftOutputSlotUi != null)
			{
				bool outputHighlighted = _dragController.HoveredSlotUi == _craftOutputSlotUi;

				InventorySlot outputSlotToShow = _craftingContainer.OutputPreviewSlot;

				// While dragging crafted output, hide the preview visually so it
				// doesn't look duplicated.
				if (_dragController.IsDragging &&
					_dragController.HeldSourceRole == InventorySlotUI.SlotRole.CraftingOutput)
				{
					outputSlotToShow = null;
				}

				SetupSlotVisual(_craftOutputSlotUi, -1, outputSlotToShow, outputHighlighted);
			}
		}
		
		if (_activeStorageContainer != null && _chestGrid.Visible)
		{
			for (int i = 0; i < _chestSlotUis.Count; i++)
			{
				var ui = _chestSlotUis[i];
				ui.SetSlotIndex(i);

				var slot = _activeStorageContainer.GetSlot(i);

				bool highlighted =
					_dragController.HoveredSlotUi == ui ||
					(_dragController.IsDragging &&
					 _dragController.HeldSourceRole == InventorySlotUI.SlotRole.ChestStorage &&
					 _dragController.HeldSourceSlotIndex == i);

				SetupSlotVisual(ui, i, slot, highlighted);
			}
		}
		
		if (_activeProcessingContainer != null)
		{
			if (_campfirePanel.Visible)
			{
				SetupSlotVisual(
					_campfireInputSlotUi,
					0,
					_activeProcessingContainer.InputSlot,
					_dragController.HoveredSlotUi == _campfireInputSlotUi);

				SetupSlotVisual(
					_campfireFuelSlotUi,
					1,
					_activeProcessingContainer.FuelSlot,
					_dragController.HoveredSlotUi == _campfireFuelSlotUi);

				SetupSlotVisual(
					_campfireOutputSlotUi,
					2,
					_activeProcessingContainer.OutputSlot,
					_dragController.HoveredSlotUi == _campfireOutputSlotUi);
			}

			if (_furnacePanel.Visible)
			{
				SetupSlotVisual(
					_furnaceInputSlotUi,
					0,
					_activeProcessingContainer.InputSlot,
					_dragController.HoveredSlotUi == _furnaceInputSlotUi);

				SetupSlotVisual(
					_furnaceFuelSlotUi,
					1,
					_activeProcessingContainer.FuelSlot,
					_dragController.HoveredSlotUi == _furnaceFuelSlotUi);

				SetupSlotVisual(
					_furnaceOutputSlotUi,
					2,
					_activeProcessingContainer.OutputSlot,
					_dragController.HoveredSlotUi == _furnaceOutputSlotUi);
			}
		}

		_dragController.UpdateDraggedLabel();
	}


	private void OnSlotPressed(InventorySlotUI slotUi)
	{
		_dragController.OnSlotPressed(slotUi);
	}

	private void OnSlotHovered(InventorySlotUI slotUi)
	{
		_dragController.OnSlotHovered(slotUi);
	}

	private void OnSlotUnhovered(InventorySlotUI slotUi)
	{
		_dragController.OnSlotUnhovered(slotUi);
	}

	public Texture2D GetItemIcon(string itemId)
	{
		var item = _inventory?.GetNodeOrNull<ItemDatabase>("/root/ItemDatabase")?.GetItem(itemId);
		return item?.Icon;
	}

	public void OpenBackpack()
	{
		_modeController.OpenBackpack();
	}

	public void OpenWorkbench()
	{
		_modeController.OpenWorkbench();
		UpdateAdminPanelVisibility();
	}

	public void OpenCampfire(ProcessingContainer container)
	{
		_modeController.OpenCampfire(container);
		UpdateAdminPanelVisibility();
	}

	public void OpenFurnace(ProcessingContainer container)
	{
		_modeController.OpenFurnace(container);
		UpdateAdminPanelVisibility();
	}

	public void OpenChest(Chest chest, StorageContainer container)
	{
		_modeController.OpenChest(chest, container);
		UpdateAdminPanelVisibility();
	}

	public void CloseBackpack()
	{
		_modeController.Close();
	}
	
	private void RefreshAdminItemPanel()
	{
		if (_adminItemGrid == null || _itemDatabase == null || InventorySlotScene == null)
			return;

		foreach (Node child in _adminItemGrid.GetChildren())
			child.QueueFree();

		var items = new List<ItemDefinition>(_itemDatabase.GetAllItems());

		items.Sort((a, b) =>
			string.Compare(a.ItemId, b.ItemId, System.StringComparison.OrdinalIgnoreCase)
		);

		foreach (var item in items)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
				continue;

			var slot = InventorySlotScene.Instantiate<InventorySlotUI>();

			int amount = Mathf.Max(1, item.MaxStackSize);

			slot.Role = InventorySlotUI.SlotRole.None;
			slot.SetMeta("admin_item_id", item.ItemId);
			slot.SetMeta("admin_item_amount", amount);

			slot.SlotPressed += OnSlotPressed;
			slot.SlotHovered += OnSlotHovered;
			slot.SlotUnhovered += OnSlotUnhovered;

			_adminItemGrid.AddChild(slot);

			slot.CallDeferred(
				nameof(InventorySlotUI.SetItemVisual),
				item.ItemId,
				amount,
				item.Icon
			);
		}
		ResizeAdminPanelToContent(items.Count);
	}
	
	private void ResizeAdminPanelToContent(int itemCount)
	{
		if (_adminItemPanel == null || _adminItemGrid == null)
			return;

		_adminItemGrid.Columns = AdminColumns;

		Vector2 slotSize = new Vector2(64, 64);

		if (_adminItemGrid.GetChildCount() > 0 &&
			_adminItemGrid.GetChild(0) is Control firstSlot)
		{
			slotSize = firstSlot.Size;

			if (slotSize.X <= 0 || slotSize.Y <= 0)
				slotSize = firstSlot.CustomMinimumSize;

			if (slotSize.X <= 0 || slotSize.Y <= 0)
				slotSize = new Vector2(64, 64);
		}

		int rows = Mathf.CeilToInt(itemCount / (float)AdminColumns);

		float width =
			AdminPanelPadding * 2 +
			(AdminColumns * slotSize.X) +
			((AdminColumns - 1) * AdminSlotSpacing);

		float contentHeight =
			AdminPanelPadding * 2 +
			(rows * slotSize.Y) +
			(Mathf.Max(0, rows - 1) * AdminSlotSpacing);

		float maxHeight = GetViewportRect().Size.Y - AdminTopMargin - AdminBottomMargin;
		float finalHeight = Mathf.Min(contentHeight, maxHeight);

		_adminItemPanel.CustomMinimumSize = new Vector2(width, finalHeight);
		_adminItemPanel.Size = new Vector2(width, finalHeight);
	}
	
}
