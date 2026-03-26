using Godot;

public partial class Player : CharacterBody3D
{
	private Inventory _inventory;
	private WorldManager _worldManager;
	private TreeResource _highlightedTree;
	
	private PlayerMovement _movement;
	private PlayerLook _look;
	private BlockTargetting _targetting;
	private PlacementPreview _placementPreview;
	private BlockOutline _blockOutline;
	private BackpackUI _backpackUi;
	
	
	[Export] public PackedScene PlacementPreviewScene;
	[Export] public PackedScene BlockOutlineScene;

	public override void _Ready()
	{
		_inventory = GetNode<Inventory>("Inventory");
		_worldManager = GetNode<WorldManager>("WorldManager");

		_movement = GetNode<PlayerMovement>("PlayerMovement");
		_look = GetNode<PlayerLook>("PlayerLook");
		_targetting = GetNode<BlockTargetting>("BlockTargetting");

		_placementPreview = PlacementPreviewScene.Instantiate<PlacementPreview>();
		GetTree().CurrentScene.CallDeferred("add_child", _placementPreview);
		_placementPreview.Visible = false;

		_blockOutline = BlockOutlineScene.Instantiate<BlockOutline>();
		GetTree().CurrentScene.CallDeferred("add_child", _blockOutline);
		_blockOutline.Visible = false;
		
		_backpackUi = GetNode<BackpackUI>("../CanvasLayer/BackpackUI (Control)");
		_backpackUi.Initialize(_inventory);

		_inventory.AddItem("stone", 32);
		_inventory.AddItem("torch", 16);
	}

	public override void _UnhandledInput(InputEvent @event)
	{	
		if (@event.IsActionPressed("toggle_inventory"))
		{
			_backpackUi.Toggle();
			return;
		}

		if (_backpackUi != null && _backpackUi.IsOpen)
			return;

		_look.HandleInput(@event, this);

		if (@event.IsActionPressed("slot_1")) SelectHotbarSlot(0);
		if (@event.IsActionPressed("slot_2")) SelectHotbarSlot(1);
		if (@event.IsActionPressed("slot_3")) SelectHotbarSlot(2);
		if (@event.IsActionPressed("slot_4")) SelectHotbarSlot(3);
		if (@event.IsActionPressed("slot_5")) SelectHotbarSlot(4);
		if (@event.IsActionPressed("slot_6")) SelectHotbarSlot(5);
		if (@event.IsActionPressed("slot_7")) SelectHotbarSlot(6);
		if (@event.IsActionPressed("slot_8")) SelectHotbarSlot(7);
		if (@event.IsActionPressed("slot_9")) SelectHotbarSlot(8);

		if (@event is InputEventMouseButton wheelEvent && wheelEvent.Pressed)
		{
			if (wheelEvent.ButtonIndex == MouseButton.WheelUp)
				CycleHotbar(-1);

			if (wheelEvent.ButtonIndex == MouseButton.WheelDown)
				CycleHotbar(1);
		}

		if (@event.IsActionPressed("place_item"))
			TryPlaceItem();

		if (@event.IsActionPressed("remove_item"))
			TryRemoveItem();
			
		if (@event.IsActionPressed("craft_world_structure"))
			TryCraftWorldStructure();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_backpackUi != null && _backpackUi.IsOpen)
		{
			if (_highlightedTree != null && IsInstanceValid(_highlightedTree))
			{
				_highlightedTree.SetHighlighted(false);
				_highlightedTree = null;
			}
			return;
		}

		if (_movement == null || _targetting == null)
			return;

		_movement.HandlePhysics(this, delta);
		_targetting.UpdateTarget();
		
		UpdateTreeHighlight();

		if (_placementPreview != null)
			_placementPreview.UpdateFromTarget(_targetting);

		if (_blockOutline != null)
			_blockOutline.UpdateFromTarget(_targetting);
	}

	private void TryPlaceItem()
	{
		if (!_targetting.HasValidPlacementTarget)
			return;

		var selectedSlot = _inventory.GetSelectedSlot();
		if (selectedSlot == null || selectedSlot.IsEmpty)
		{
			GD.Print("Selected slot is empty.");
			return;
		}

		bool placed = _worldManager.TryPlaceInventoryItem(_targetting.TargetCell, selectedSlot.Item.ItemId);
		if (!placed)
		{
			GD.Print("Cell occupied or item could not be placed.");
			return;
		}
		
		if (selectedSlot.Item.ItemId == "acorn")
		{
			GD.Print("You can't place the acorn yet.");
			return;
		}

		_inventory.ConsumeSelectedItem(1);

		var slot = _inventory.GetSelectedSlot();
		string label = slot.IsEmpty ? "Empty" : $"{slot.Item.ItemId} x{slot.Count}";
		GD.Print($"Placed item. Slot now: {label}");
	}

	private void TryRemoveItem()
	{
		var lookedAtNode = _targetting.LookedAtNode;
		if (lookedAtNode != null)
		{
			var tree = FindTreeResource(lookedAtNode);
			if (tree != null)
			{
				tree.Mine(this);
				return;
			}
		}

		if (!_targetting.IsLookingAtPlacedItem)
			return;

		if (!_worldManager.TryRemoveBreakableBlock(_targetting.LookedAtCell, out string itemId))
		{
			GD.Print("This block cannot be mined.");
			return;
		}

		_inventory.AddItem(itemId, 1);
		GD.Print($"Picked up: {itemId}");
	}

	private void SelectHotbarSlot(int index)
	{
		_inventory.SelectSlot(index);
		GD.Print($"Selected slot {index + 1}: {_inventory.GetSelectedSlotLabel()}");
	}

	private void CycleHotbar(int direction)
	{
		_inventory.CycleSelection(direction);
		GD.Print($"Selected slot {_inventory.SelectedIndex + 1}: {_inventory.GetSelectedSlotLabel()}");
	}
	
	private TreeResource FindTreeResource(Node node)
	{
		while (node != null)
		{
			if (node is TreeResource tree)
				return tree;

			node = node.GetParent();
		}

		return null;
	}
	
	private void UpdateTreeHighlight()
	{
		TreeResource newTree = null;

		if (_targetting != null && _targetting.LookedAtNode != null)
		{
			newTree = FindTreeResource(_targetting.LookedAtNode);
		}

		if (_highlightedTree != newTree)
		{
			if (_highlightedTree != null && IsInstanceValid(_highlightedTree))
				_highlightedTree.SetHighlighted(false);

			_highlightedTree = newTree;

			if (_highlightedTree != null)
				_highlightedTree.SetHighlighted(true);
		}
	}
	
	private void TryCraftWorldStructure()
	{
		if (!_targetting.IsLookingAtPlacedItem)
			return;

		bool crafted = _worldManager.TryCraftWorldStructureFromCell(_targetting.LookedAtCell);

		if (!crafted)
			GD.Print("No valid world crafting recipe found.");
	}
	
}
