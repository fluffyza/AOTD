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
	private WorldCraftPreview _worldCraftPreview;
	
	private BlockManager _blockManager;
	private string _lastHeldBlockItemId = "";

	
	private Vector3 _currentHeldItemRotation = Vector3.Zero;
	
	[Export] public Control _handRoot;

	[Export] public TextureRect _handsLeftUi;
	[Export] public TextureRect _handsRightUi;
	[Export] public TextureRect _pickaxeHandUi;

	[Export] public Node3D _heldItemRoot;
	[Export] public Node3D _heldTorchRoot;
	[Export] public Node3D _heldBlockRoot;
	[Export] public OmniLight3D _heldTorchLight;

	private Vector2 _handRootBasePosition;
	private Vector3 _heldItemRootBasePosition;
	

	
	private float _bobTime = 0f;
	private Vector2 _lookSway = Vector2.Zero;
	private Vector2 _currentHandsOffset = Vector2.Zero;
	private Vector3 _currentHeldItemOffset = Vector3.Zero;
	

	[Export] public float HandsBobSpeed = 10f;
	[Export] public float HandsBobAmountX = 8f;
	[Export] public float HandsBobAmountY = 10f;
	[Export] public float HandsLookSwayAmount = 6f;
	[Export] public float HandsSwayLerpSpeed = 10f;
	
	[Export] public PackedScene WorldCraftPreviewScene;
	[Export] public PackedScene PlacementPreviewScene;
	[Export] public PackedScene BlockOutlineScene;

	public override void _Ready()
	{
		_inventory = GetNode<Inventory>("Inventory");
		_worldManager = GetNode<WorldManager>("WorldManager");
		
		_blockManager = GetNode<BlockManager>("BlockManager");

		_movement = GetNode<PlayerMovement>("PlayerMovement");
		_look = GetNode<PlayerLook>("PlayerLook");
		_targetting = GetNode<BlockTargetting>("BlockTargetting");

		_placementPreview = PlacementPreviewScene.Instantiate<PlacementPreview>();
		GetTree().CurrentScene.CallDeferred("add_child", _placementPreview);
		_placementPreview.Visible = false;

		_blockOutline = BlockOutlineScene.Instantiate<BlockOutline>();
		GetTree().CurrentScene.CallDeferred("add_child", _blockOutline);
		_blockOutline.Visible = false;
		
		_worldCraftPreview = WorldCraftPreviewScene.Instantiate<WorldCraftPreview>();
		GetTree().CurrentScene.CallDeferred("add_child", _worldCraftPreview);
		_worldCraftPreview.Visible = false;
		
		_backpackUi = GetNode<BackpackUI>("../CanvasLayer/BackpackUI (Control)");
		_backpackUi.Initialize(_inventory);

		_inventory.AddItem("stone", 32);
		_inventory.AddItem("torch", 16);
		
		if (_handRoot != null)
			_handRootBasePosition = _handRoot.Position;

		if (_heldItemRoot != null)
			_heldItemRootBasePosition = _heldItemRoot.Position;

		if (_heldTorchRoot != null)
			_heldTorchRoot.Visible = false;

		if (_heldBlockRoot != null)
			_heldBlockRoot.Visible = false;

		if (_heldTorchLight != null)
			_heldTorchLight.Visible = false;
		
		UpdateHeldVisual();
	}
	
	private void UpdateHeldBlockVisual(ItemDefinition item)
	{
		if (_heldBlockRoot == null || item == null || !item.IsBlock || _blockManager == null)
			return;

		Node targetNode = _heldBlockRoot;

		if (_heldBlockRoot.GetChildCount() > 0)
			targetNode = _heldBlockRoot.GetChild(0);

		_blockManager.ApplyHeldBlockAppearance(targetNode, item);
	}

	public override void _UnhandledInput(InputEvent @event)
	{	
		if (@event.IsActionPressed("toggle_inventory"))
		{
			_backpackUi.Toggle();
			return;
		}
		
		if (@event is InputEventMouseMotion mouseMotion)
		{
			_lookSway = new Vector2(-mouseMotion.Relative.X, -mouseMotion.Relative.Y) * 0.02f;
		}

		if (@event.IsActionPressed("Interact"))
		{
			TryInteract();
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
	
	private void UpdateHandsViewmodel(double delta)
	{
		bool any2DVisible =
			(_handsLeftUi != null && _handsLeftUi.Visible) ||
			(_handsRightUi != null && _handsRightUi.Visible) ||
			(_pickaxeHandUi != null && _pickaxeHandUi.Visible);

		bool any3DVisible =
			(_heldTorchRoot != null && _heldTorchRoot.Visible) ||
			(_heldBlockRoot != null && _heldBlockRoot.Visible);

		if (!any2DVisible && !any3DVisible)
			return;

		Vector2 targetOffset2D = Vector2.Zero;
		Vector3 targetOffset3D = Vector3.Zero;

		Vector3 horizontalVelocity = Velocity;
		horizontalVelocity.Y = 0f;
		bool isMoving = horizontalVelocity.Length() > 0.1f && IsOnFloor();

		if (isMoving)
		{
			_bobTime += (float)delta * HandsBobSpeed;

			float bobX = Mathf.Sin(_bobTime) * HandsBobAmountX;
			float bobY = Mathf.Abs(Mathf.Cos(_bobTime)) * HandsBobAmountY;

			targetOffset2D += new Vector2(bobX, bobY);

			targetOffset3D += new Vector3(
				bobX * 0.02f,
				-bobY * 0.02f,
				0f
			);
		}
		else
		{
			_bobTime = 0f;
		}

		targetOffset2D += _lookSway * HandsLookSwayAmount;

		targetOffset3D += new Vector3(
			-_lookSway.X * 0.05f,
			_lookSway.Y * 0.05f,
			0f
		);

		_currentHandsOffset = _currentHandsOffset.Lerp(targetOffset2D, (float)delta * HandsSwayLerpSpeed);
		_currentHeldItemOffset = _currentHeldItemOffset.Lerp(targetOffset3D, (float)delta * HandsSwayLerpSpeed);
		_lookSway = _lookSway.Lerp(Vector2.Zero, (float)delta * 8f);

		Vector3 targetRotation = new Vector3(
			-_lookSway.Y * 0.08f,
			-_lookSway.X * 0.12f,
			-_lookSway.X * 0.05f
		);

		_currentHeldItemRotation = _currentHeldItemRotation.Lerp(targetRotation, (float)delta * HandsSwayLerpSpeed);

		if (_handRoot != null)
			_handRoot.Position = _handRootBasePosition + _currentHandsOffset;

		if (_heldItemRoot != null)
		{
			_heldItemRoot.Position = _heldItemRootBasePosition + _currentHeldItemOffset;
			_heldItemRoot.Rotation = _currentHeldItemRotation;
		}
	}

	private void TryInteract()
	{
		if (_backpackUi == null || _targetting == null)
			return;

		if (!_targetting.IsLookingAtPlacedItem || _targetting.HitItem == null)
			return;

		Node current = _targetting.HitItem;

		while (current != null)
		{
			if (current is Workbench)
			{
				_backpackUi.OpenWorkbench();
				Input.MouseMode = Input.MouseModeEnum.Visible;
				return;
			}

			current = current.GetParent();
		}
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
			
			if (_heldItemRoot != null)
			{
				_heldItemRoot.Position = _heldItemRootBasePosition;
				_heldItemRoot.Rotation = Vector3.Zero;
			}
			
			if (_handRoot != null)
				_handRoot.Position = _handRootBasePosition;
			
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
			
		UpdateWorldCraftPreview();
		
		UpdateHandsViewmodel(delta);
	}
	
	private void UpdateWorldCraftPreview()
	{
		if (_worldCraftPreview == null || _targetting == null || _worldManager == null)
			return;

		if (!_targetting.IsLookingAtPlacedItem)
		{
			_worldCraftPreview.HidePreview();
			return;
		}

		if (_worldManager.TryGetWorldCraftPreviewCells(_targetting.LookedAtCell, out var previewCells))
		{
			_worldCraftPreview.ShowMatch(previewCells);
		}
		else
		{
			_worldCraftPreview.HidePreview();
		}
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
		UpdateHeldVisual();

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
		UpdateHeldVisual();
		GD.Print($"Selected slot {index + 1}: {_inventory.GetSelectedSlotLabel()}");
	}

	private void CycleHotbar(int direction)
	{
		_inventory.CycleSelection(direction);
		UpdateHeldVisual();
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
	
	private void HideAllHeldVisuals()
	{
		if (_handsLeftUi != null)
			_handsLeftUi.Visible = false;

		if (_handsRightUi != null)
			_handsRightUi.Visible = false;

		if (_pickaxeHandUi != null)
			_pickaxeHandUi.Visible = false;

		if (_heldTorchRoot != null)
			_heldTorchRoot.Visible = false;

		if (_heldBlockRoot != null)
			_heldBlockRoot.Visible = false;

		if (_heldTorchLight != null)
			_heldTorchLight.Visible = false;
	}
	
	private void UpdateHeldVisual()
	{
		if (_inventory == null)
			return;

		HideAllHeldVisuals();
		_lastHeldBlockItemId = "";
		var selectedSlot = _inventory.GetSelectedSlot();
		if (selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.Item == null)
		{
			if (_handsLeftUi != null)
				_handsLeftUi.Visible = true;

			if (_handsRightUi != null)
				_handsRightUi.Visible = true;

			return;
		}

		string itemId = selectedSlot.Item.ItemId;

		if (itemId == "pickaxe")
		{
			if (_pickaxeHandUi != null)
				_pickaxeHandUi.Visible = true;

			return;
		}

		if (itemId == "torch")
		{
			if (_heldTorchRoot != null)
				_heldTorchRoot.Visible = true;

			if (_heldTorchLight != null)
				_heldTorchLight.Visible = true;

			return;
		}

		if (selectedSlot.Item.IsBlock)
		{
			if (_heldBlockRoot != null)
				_heldBlockRoot.Visible = true;

			if (_lastHeldBlockItemId != selectedSlot.Item.ItemId)
			{
				UpdateHeldBlockVisual(selectedSlot.Item);
				_lastHeldBlockItemId = selectedSlot.Item.ItemId;
			}

			return;
		}
	}

	
}
