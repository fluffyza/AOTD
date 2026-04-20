using Godot;

public partial class Player : CharacterBody3D
{
	private Inventory _inventory;
	private WorldManager _worldManager;
	private TreeResource _highlightedTree;
	private CraftedStructure _highlightedStructure;
	
	private PlayerMovement _movement;
	private PlayerLook _look;
	private BlockTargetting _targetting;
	private PlacementPreview _placementPreview;
	private BlockOutline _blockOutline;
	private BackpackUI _backpackUi;
	private WorldCraftPreview _worldCraftPreview;
	
	private BlockManager _blockManager;
	private string _lastHeldBlockItemId = "";
	
	private float _currentHandBrightness = 1.0f;
	private float _targetHandBrightness = 1.0f;

	[Export] public float HandBrightnessLerpSpeed = 5.0f;
	[Export] public float DarkHandBrightness = 0.1f;
	[Export] public float LitHandBrightness = 1.0f;
	[Export] public float HandLightDetectionRange = .01f;
	
	private Vector3 _currentHeldItemRotation = Vector3.Zero;
	
	[Export] public Control _handRoot;

	[Export] public TextureRect _handsLeftUi;
	[Export] public TextureRect _handsRightUi;
	[Export] public TextureRect _pickaxeHandUi;

	[Export] public Node3D _heldItemRoot;
	[Export] public Node3D _heldTorchRoot;
	[Export] public Node3D _heldBlockRoot;
	[Export] public OmniLight3D _heldTorchLight;
	
	private bool _isHeldItemHitting = false;
	private bool _isHeldItemHeld = false;
	private bool _heldItemHitApplied = false;
	private float _heldItemHitTimer = 0f;
	private bool _pendingHeldItemVisualRefresh = false;
	

	[Export] public float HeldItemHitDuration = 0.18f;
	[Export] public float HeldItemHitReturnDuration = 0.12f;
	[Export] public Vector3 HeldItemHitPositionOffset = new Vector3(0.08f, -0.08f, -0.10f);
	[Export] public Vector3 HeldItemHitRotationOffsetDegrees = new Vector3(-18f, 12f, 8f);
	
	[Export] public FistPunch _fistPunchController;
	private bool _pendingHeldVisualRefresh = false;
	
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
		
		_backpackUi = GetNode<BackpackUI>("../../../../CanvasLayer/BackpackUI (Control)");
		_backpackUi.CallDeferred(nameof(BackpackUI.Initialize), _inventory);
		
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
			
		if (_fistPunchController != null)
			_fistPunchController.PunchImpact += OnFistPunchImpact;
			
		if (_isHeldItemHitting)
		{
			_pendingHeldItemVisualRefresh = true;
		}
		else
		{
			UpdateHeldVisual();
		}
	}
	
	private void OnFistPunchImpact()
	{
		if (!IsEmptyHanded())
			return;

		TryRemoveItem();
	}
	
	private bool IsEmptyHanded()
	{
		if (_inventory == null)
			return true;

		var selectedSlot = _inventory.GetSelectedSlot();
		return selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.Item == null;
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
		
		if (@event.IsActionPressed("place_item"))
			TryPlaceItem();

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

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed)
			{
				if (IsEmptyHanded())
				{
					_fistPunchController?.SetHeld(true);
					_fistPunchController?.StartPunch();
					return;
				}
				if (IsUsing3DHeldItem())
				{
					_isHeldItemHeld = true;

					if (!_isHeldItemHitting)
						StartHeldItemHit();

					return;
				}

				TryRemoveItem();
				
				if (_isHeldItemHitting)
				{
					_pendingHeldItemVisualRefresh = true;
				}
				else
				{
					UpdateHeldVisual();
				}
				return;
			}
			else
			{
				if (IsEmptyHanded())
				{
					_fistPunchController?.SetHeld(false);
					return;
				}
				
				if (IsUsing3DHeldItem())
				{
					_isHeldItemHeld = false;
					return;
				}
			}
		}
			
		if (@event.IsActionPressed("craft_world_structure"))
			TryCraftOrDeconstructWorldStructure();
	}
	
	private void TryCraftOrDeconstructWorldStructure()
	{
		if (!_targetting.IsLookingAtPlacedItem)
			return;

		if (_targetting.LookedAtNode != null)
		{
			var structure = FindCraftedStructure(_targetting.LookedAtNode);
			if (structure != null)
			{
				bool deconstructed = _worldManager.TryDeconstructStructure(structure);
				if (!deconstructed)
					GD.Print("Could not deconstruct structure.");
				return;
			}
		}

		bool crafted = _worldManager.TryCraftWorldStructureFromCell(_targetting.LookedAtCell);

		if (!crafted)
			GD.Print("No valid world crafting recipe found.");
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

			if (current is Campfire campfire)
			{
				_backpackUi.OpenCampfire(campfire.GetProcessingContainer());
				Input.MouseMode = Input.MouseModeEnum.Visible;
				return;
			}

			if (current is Furnace furnace)
			{
				_backpackUi.OpenFurnace(furnace.GetProcessingContainer());
				Input.MouseMode = Input.MouseModeEnum.Visible;
				return;
			}
			
			if (current is Chest chest)
			{
				chest.Interact(_backpackUi);
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
			
			if (_highlightedStructure != null && IsInstanceValid(_highlightedStructure))
			{
				_highlightedStructure.SetHighlighted(false);
				_highlightedStructure = null;
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
		
		UpdateCraftedStructureHighlight();
		UpdateTreeHighlight();

		if (_placementPreview != null)
			_placementPreview.UpdateFromTarget(_targetting);

		if (_blockOutline != null)
		{
			bool isWorkbench = false;

			if (_targetting != null && _targetting.HitItem != null)
			{
				Node current = _targetting.HitItem;
				while (current != null)
				{
					if (current is Workbench)
					{
						isWorkbench = true;
						break;
					}
					current = current.GetParent();
				}
			}

			if (isWorkbench)
				_blockOutline.Visible = false;
			else
				_blockOutline.UpdateFromTarget(_targetting);
		}
		
		if (_pendingHeldVisualRefresh &&
			(_fistPunchController == null || !_fistPunchController.IsPunching))
		{
			_pendingHeldVisualRefresh = false;
			
			if (_isHeldItemHitting)
			{
				_pendingHeldItemVisualRefresh = true;
			}
			else
			{
				UpdateHeldVisual();
			}
		}
		
		UpdateWorldCraftPreview();
		UpdateFakeHandLighting(delta);
		UpdateHandsViewmodel(delta);
		UpdateHeldItemHitAnimation(delta);
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
		var selectedSlot = _inventory.GetSelectedSlot();
		if (selectedSlot == null || selectedSlot.IsEmpty)
		{
			GD.Print("Selected slot is empty.");
			return;
		}

		Vector3I placementCell = _targetting.TargetCell;

		if (_targetting.IsLookingAtPlacedItem &&
			_worldManager.TryGetBlock(_targetting.LookedAtCell, out Node3D lookedAtBlock) &&
			lookedAtBlock != null &&
			lookedAtBlock.HasMeta("is_ground_tile") &&
			(bool)lookedAtBlock.GetMeta("is_ground_tile"))
		{
			placementCell = _targetting.LookedAtCell + Vector3I.Up;
		}
		else if (!_targetting.HasValidPlacementTarget)
		{
			return;
		}

		if (selectedSlot.Item.ItemId == "acorn")
		{
			GD.Print("You can't place the acorn yet.");
			return;
		}
		
		Vector3 playerForward = -GlobalTransform.Basis.Z;
		bool placed = _worldManager.TryPlaceInventoryItem(placementCell, selectedSlot.Item.ItemId, playerForward);
		if (!placed)
		{
			GD.Print("Cell occupied or item could not be placed.");
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

				if (!IsEmptyHanded())
					_fistPunchController?.SetHeld(false);

				if (_fistPunchController != null && _fistPunchController.IsPunching)
				{
					_pendingHeldVisualRefresh = true;
				}
				else
				{
					if (_isHeldItemHitting)
					{
						_pendingHeldItemVisualRefresh = true;
					}
					else
					{
						UpdateHeldVisual();
					}
				}

				return;
			}

			var structure = FindCraftedStructure(lookedAtNode);
			if (structure != null)
			{
				GD.Print("Press F to deconstruct the structure.");
				return;
			}
		}

		if (!_targetting.IsLookingAtPlacedItem)
			return;

		// Check item before removing it, so we can enforce tool requirements.
		if (!_worldManager.TryGetBlock(_targetting.LookedAtCell, out Node3D placedNode) || placedNode == null)
			return;

		string lookedAtItemId = _blockManager.GetDroppedItemId(placedNode);

		if (lookedAtItemId == "iron_ore" && !IsHoldingPickaxe())
		{
			GD.Print("You need a pickaxe to mine iron ore.");
			return;
		}

		if (!_worldManager.TryRemoveBreakableBlock(_targetting.LookedAtCell, out string itemId))
		{
			GD.Print("This block cannot be mined.");
			return;
		}

		_inventory.AddItem(itemId, 1);
		
		if (!IsEmptyHanded())
			_fistPunchController?.SetHeld(false);

		if (_fistPunchController != null && _fistPunchController.IsPunching)
		{
			_pendingHeldVisualRefresh = true;
		}
		else
		{
			if (_isHeldItemHitting)
			{
				_pendingHeldItemVisualRefresh = true;
			}
			else
			{
				UpdateHeldVisual();
			}
		}

		GD.Print($"Picked up: {itemId}");;
		
	}

	private bool IsHoldingPickaxe()
	{
		if (_inventory == null)
			return false;

		var selectedSlot = _inventory.GetSelectedSlot();
		return selectedSlot != null &&
			   !selectedSlot.IsEmpty &&
			   selectedSlot.Item != null &&
			   selectedSlot.Item.ItemId == "pickaxe";
	}

	private void SelectHotbarSlot(int index)
	{
		_inventory.SelectSlot(index);
		
		if (_isHeldItemHitting)
		{
			_pendingHeldItemVisualRefresh = true;
		}
		else
		{
			UpdateHeldVisual();
		}
		GD.Print($"Selected slot {index + 1}: {_inventory.GetSelectedSlotLabel()}");
	}

	private void CycleHotbar(int direction)
	{
		_inventory.CycleSelection(direction);
		
		if (_isHeldItemHitting)
		{
			_pendingHeldItemVisualRefresh = true;
		}
		else
		{
			UpdateHeldVisual();
		}
		
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
	
	private void HideAllHeldVisuals()
	{
		_isHeldItemHitting = false;
		_isHeldItemHeld = false;
		_heldItemHitApplied = false;
		_heldItemHitTimer = 0f;

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
			
		_fistPunchController?.StopPunch();
		
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
	
	private Workbench FindWorkbench(Node node)
	{
		while (node != null)
		{
			if (node is Workbench bench)
				return bench;

			node = node.GetParent();
		}

		return null;
	}

	private void UpdateCraftedStructureHighlight()
	{
		CraftedStructure newStructure = null;

		if (_targetting != null && _targetting.LookedAtNode != null)
			newStructure = FindCraftedStructure(_targetting.LookedAtNode);

		if (_highlightedStructure != newStructure)
		{
			if (_highlightedStructure != null && IsInstanceValid(_highlightedStructure))
				_highlightedStructure.SetHighlighted(false);

			_highlightedStructure = newStructure;

			if (_highlightedStructure != null)
				_highlightedStructure.SetHighlighted(true);
		}
		
	}

	private CraftedStructure FindCraftedStructure(Node node)
	{
		while (node != null)
		{
			if (node is CraftedStructure structure)
				return structure;

			node = node.GetParent();
		}

		return null;
	}
	
	private void UpdateFakeHandLighting(double delta)
	{
		float lightFactor = GetNearestWorldLightFactor(20.0f);

		var selectedSlot = _inventory?.GetSelectedSlot();
		bool holdingTorch =
			selectedSlot != null &&
			!selectedSlot.IsEmpty &&
			selectedSlot.Item != null &&
			selectedSlot.Item.ItemId == "torch";

		if (holdingTorch)
			lightFactor = 1.0f;

		_targetHandBrightness = Mathf.Lerp(
			DarkHandBrightness,
			LitHandBrightness,
			lightFactor
		);

		_currentHandBrightness = Mathf.Lerp(
			_currentHandBrightness,
			_targetHandBrightness,
			(float)delta * HandBrightnessLerpSpeed
		);

		Color handTint = new Color(
			_currentHandBrightness,
			_currentHandBrightness,
			_currentHandBrightness,
			1f
		);

		if (_handsLeftUi != null)
			_handsLeftUi.Modulate = handTint;

		if (_handsRightUi != null)
			_handsRightUi.Modulate = handTint;

		if (_pickaxeHandUi != null)
			_pickaxeHandUi.Modulate = handTint;

		if (_fistPunchController != null)
		{
			if (_fistPunchController.RightPunch != null)
				_fistPunchController.RightPunch.Modulate = handTint;

			if (_fistPunchController.LeftPunch != null)
				_fistPunchController.LeftPunch.Modulate = handTint;
		}

	}
	
	private float GetNearestWorldLightFactor(float maxDistance)
	{
		var lights = GetTree().GetNodesInGroup("world_light");
		float bestFactor = 0.0f;

		foreach (Node node in lights)
		{
			if (node is not OmniLight3D omniLight)
				continue;

			if (!omniLight.Visible)
				continue;

			// Ignore the player's own held torch light
			if (omniLight == _heldTorchLight)
				continue;

			// Ignore any light that lives under the player hierarchy
			if (IsDescendantOfPlayer(omniLight))
				continue;

			float distance = GlobalPosition.DistanceTo(omniLight.GlobalPosition);
			if (distance > maxDistance)
				continue;

			float normalized = 1.0f - Mathf.Clamp(distance / maxDistance, 0.0f, 1.0f);
			float factor = normalized * normalized * normalized;
			bestFactor = Mathf.Max(bestFactor, factor);
		}

		return bestFactor;
	}

	private bool IsDescendantOfPlayer(Node node)
	{
		Node current = node;
		while (current != null)
		{
			if (current == this)
				return true;

			current = current.GetParent();
		}

		return false;
	}
	
	private bool IsUsing3DHeldItem()
	{
		return (_heldTorchRoot != null && _heldTorchRoot.Visible) ||
			   (_heldBlockRoot != null && _heldBlockRoot.Visible);
	}
	
	private void StartHeldItemHit()
	{
		if (_isHeldItemHitting)
			return;

		if (!IsUsing3DHeldItem())
			return;

		_isHeldItemHitting = true;
		_heldItemHitApplied = false;
		_heldItemHitTimer = 0f;
	}
	
	private void UpdateHeldItemHitAnimation(double delta)
	{
		if (_heldItemRoot == null || !IsUsing3DHeldItem())
			return;

		if (!_isHeldItemHitting)
			return;

		_heldItemHitTimer += (float)delta;

		float totalDuration = HeldItemHitDuration + HeldItemHitReturnDuration;

		Vector3 hitOffset = Vector3.Zero;
		Vector3 hitRotation = Vector3.Zero;

		if (_heldItemHitTimer <= HeldItemHitDuration)
		{
			float forwardT = _heldItemHitTimer / HeldItemHitDuration;
			float eased = 1f - Mathf.Pow(1f - forwardT, 3f);

			hitOffset = HeldItemHitPositionOffset * eased;
			hitRotation = new Vector3(
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.X),
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.Y),
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.Z)
			) * eased;

			// Apply one hit near the end of the forward motion
			if (!_heldItemHitApplied && forwardT >= 0.85f)
			{
				_heldItemHitApplied = true;
				TryRemoveItem();
			}
		}
		else
		{
			float returnT = (_heldItemHitTimer - HeldItemHitDuration) / HeldItemHitReturnDuration;
			returnT = Mathf.Clamp(returnT, 0f, 1f);

			float eased = 1f - returnT;

			hitOffset = HeldItemHitPositionOffset * eased;
			hitRotation = new Vector3(
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.X),
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.Y),
				Mathf.DegToRad(HeldItemHitRotationOffsetDegrees.Z)
			) * eased;
		}

		_heldItemRoot.Position += hitOffset;
		_heldItemRoot.Rotation += hitRotation;

		if (_heldItemHitTimer >= totalDuration)
		{
			_isHeldItemHitting = false;
			_heldItemHitTimer = 0f;
			_heldItemHitApplied = false;

			if (_pendingHeldItemVisualRefresh)
			{
				_pendingHeldItemVisualRefresh = false;

				// Only refresh if the selected held visual actually changed.
				// If you're still holding the same item, don't kill the hold loop.
				bool selectedSlotStillSameVisual = !IsEmptyHanded();

				if (selectedSlotStillSameVisual)
				{
					if (_isHeldItemHeld && IsUsing3DHeldItem())
						StartHeldItemHit();

					return;
				}
				
				if (_isHeldItemHitting)
				{
					_pendingHeldItemVisualRefresh = true;
				}
				else
				{
					UpdateHeldVisual();
				}
				
				return;
			}

			if (_isHeldItemHeld && IsUsing3DHeldItem())
				StartHeldItemHit();
		}
	}

	
}
