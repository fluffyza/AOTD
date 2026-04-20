using Godot;
using System;
using System.Collections.Generic;

public class InventoryUiModeController
{
	private readonly BackpackUI _ui;

	private readonly Control _backpackCraftingPanel;
	private readonly Control _workbenchCraftingPanel;
	private readonly Control _campfirePanel;
	private readonly Control _furnacePanel;
	private readonly GridContainer _chestGrid;

	private readonly GridContainer _backpackCraftingGrid;
	private readonly GridContainer _workbenchCraftingGrid;

	private readonly InventorySlotUI _backpackCraftOutputSlotUi;
	private readonly InventorySlotUI _workbenchCraftOutputSlotUi;

	private readonly CraftingContainer _backpackCraftingContainer;
	private readonly CraftingContainer _workbenchCraftingContainer;

	private readonly List<InventorySlotUI> _backpackCraftingSlotUis;
	private readonly List<InventorySlotUI> _workbenchCraftingSlotUis;
	private readonly List<InventorySlotUI> _craftingSlotUis;

	public InventoryUiModeController(BackpackUI ui)
	{
		_ui = ui;
		
		_backpackCraftingPanel = ui._backpackCraftingPanel;
		_workbenchCraftingPanel = ui._workbenchCraftingPanel;
		_campfirePanel = ui._campfirePanel;
		_furnacePanel = ui._furnacePanel;
		_chestGrid = ui._chestGrid;

		_backpackCraftingGrid = ui._backpackCraftingGrid;
		_workbenchCraftingGrid = ui._workbenchCraftingGrid;

		_backpackCraftOutputSlotUi = ui._backpackCraftOutputSlotUi;
		_workbenchCraftOutputSlotUi = ui._workbenchCraftOutputSlotUi;

		_backpackCraftingContainer = ui._backpackCraftingContainer;
		_workbenchCraftingContainer = ui._workbenchCraftingContainer;

		_backpackCraftingSlotUis = ui._backpackCraftingSlotUis;
		_workbenchCraftingSlotUis = ui._workbenchCraftingSlotUis;
		_craftingSlotUis = ui._craftingSlotUis;
	}

	public void OpenBackpackCraftingMode()
	{
		CloseActiveChestIfAny();
		_ui._currentMode = BackpackUI.UiMode.BackpackCrafting;
		_ui._activeProcessingContainer = null;
		UnsubscribeFromProcessingContainer();

		_backpackCraftingPanel.Visible = true;
		_workbenchCraftingPanel.Visible = false;
		_campfirePanel.Visible = false;
		_furnacePanel.Visible = false;
		UnsubscribeFromStorageContainer();
		_chestGrid.Visible = false;
		
		_ui._craftingGrid = _backpackCraftingGrid;
		_ui._craftOutputSlotUi = _backpackCraftOutputSlotUi;
		_ui._craftingContainer = _backpackCraftingContainer;

		_craftingSlotUis.Clear();
		_craftingSlotUis.AddRange(_backpackCraftingSlotUis);
		_ui.Refresh();
	}

	public void OpenWorkbenchCraftingMode()
	{
		CloseActiveChestIfAny();
		_ui._currentMode = BackpackUI.UiMode.WorkbenchCrafting;
		_ui._activeProcessingContainer = null;
		UnsubscribeFromProcessingContainer();

		_backpackCraftingPanel.Visible = false;
		UnsubscribeFromStorageContainer();
		_chestGrid.Visible = false;
		_workbenchCraftingPanel.Visible = true;
		_campfirePanel.Visible = false;
		_furnacePanel.Visible = false;

		_ui._craftingGrid = _workbenchCraftingGrid;
		_ui._craftOutputSlotUi = _workbenchCraftOutputSlotUi;
		_ui._craftingContainer = _workbenchCraftingContainer;

		_craftingSlotUis.Clear();
		_craftingSlotUis.AddRange(_workbenchCraftingSlotUis);

		_ui.Refresh();
	}
	
	public void OpenBackpack()
	{
		OpenBackpackCraftingMode();
		OpenUI();
	}
	
	public void OpenUI()
	{
		_ui.Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_ui.Refresh();
	}

	public void OpenWorkbench()
	{
		OpenWorkbenchCraftingMode();
		OpenUI();
	}
	public void Close()
	{
		_ui.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_ui._dragController.CancelDragAndReturnHeldStack();
		CloseActiveChestIfAny();
		
		UnsubscribeFromStorageContainer();
		_chestGrid.Visible = false;
		
		if (_ui._currentMode == BackpackUI.UiMode.BackpackCrafting ||
			_ui._currentMode == BackpackUI.UiMode.WorkbenchCrafting)
		{
			_ui.ReturnCraftingInputsToInventory();
		}

		_ui._activeProcessingContainer = null;
		UnsubscribeFromProcessingContainer();
	}
	
	
	public void OpenCampfire(ProcessingContainer container)
	{
		OpenCampfireMode(container);
		OpenUI();
	}

	public void OpenFurnace(ProcessingContainer container)
	{
		OpenFurnaceMode(container);
		OpenUI();
	}

	private void OpenCampfireMode(ProcessingContainer container)
	{
		CloseActiveChestIfAny();
		_ui._currentMode = BackpackUI.UiMode.CampfireProcessing;
		_ui._activeProcessingContainer = container;
		_ui._craftingContainer = null;

		SubscribeToProcessingContainer(container);

		_backpackCraftingPanel.Visible = false;
		_workbenchCraftingPanel.Visible = false;
		_campfirePanel.Visible = true;
		_furnacePanel.Visible = false;
		UnsubscribeFromStorageContainer();
		_chestGrid.Visible = false;

		_ui.Refresh();
	}

	private void OpenFurnaceMode(ProcessingContainer container)
	{
		CloseActiveChestIfAny();
		_ui._currentMode = BackpackUI.UiMode.FurnaceProcessing;
		_ui._activeProcessingContainer = container;
		_ui._craftingContainer = null;

		SubscribeToProcessingContainer(container);
		UnsubscribeFromStorageContainer();

		_chestGrid.Visible = false;
		_backpackCraftingPanel.Visible = false;
		_workbenchCraftingPanel.Visible = false;
		_campfirePanel.Visible = false;
		_furnacePanel.Visible = true;

		_ui.Refresh();
	}
	
	
	private void SubscribeToProcessingContainer(ProcessingContainer container)
	{
		if (_ui._subscribedProcessingContainer != null &&
			GodotObject.IsInstanceValid(_ui._subscribedProcessingContainer))
		{
			_ui._subscribedProcessingContainer.ProcessingChanged -= _ui.Refresh;
		}

		_ui._subscribedProcessingContainer = container;

		if (_ui._subscribedProcessingContainer != null)
			_ui._subscribedProcessingContainer.ProcessingChanged += _ui.Refresh;
	}

	private void UnsubscribeFromProcessingContainer()
	{
		if (_ui._subscribedProcessingContainer != null &&
			GodotObject.IsInstanceValid(_ui._subscribedProcessingContainer))
		{
			_ui._subscribedProcessingContainer.ProcessingChanged -= _ui.Refresh;
		}

		_ui._subscribedProcessingContainer = null;
	}
	
	public void OpenChest(Chest chest, StorageContainer container)
	{
		if (container == null || chest == null)
			return;

		UnsubscribeFromProcessingContainer();
		UnsubscribeFromStorageContainer();

		_ui._currentMode = BackpackUI.UiMode.ChestStorage;
		_ui._activeProcessingContainer = null;
		_ui._craftingContainer = null;

		_ui._activeChest = chest;
		_ui._activeStorageContainer = container;

		_chestGrid.Visible = true;
		_backpackCraftingPanel.Visible = false;
		_workbenchCraftingPanel.Visible = false;
		_campfirePanel.Visible = false;
		_furnacePanel.Visible = false;

		_ui._activeStorageContainer.StorageChanged += _ui.Refresh;

		OpenUI();
	}
	
	private void UnsubscribeFromStorageContainer()
	{
		if (_ui._activeStorageContainer != null &&
			GodotObject.IsInstanceValid(_ui._activeStorageContainer))
		{
			_ui._activeStorageContainer.StorageChanged -= _ui.Refresh;
		}

		_ui._activeStorageContainer = null;
	}
	
	
	private void CloseActiveChestIfAny()
	{
		if (_ui._activeChest != null && GodotObject.IsInstanceValid(_ui._activeChest))
		{
			_ui._activeChest.CloseChestVisual();
			_ui._activeChest = null;
		}
	}
	
	
}
