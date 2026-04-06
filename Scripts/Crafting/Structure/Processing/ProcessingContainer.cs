using Godot;

public partial class ProcessingContainer : Node
{
	[Signal]
	public delegate void ProcessingChangedEventHandler();

	[Export] public string StationType = "Furnace";

	public InventorySlot InputSlot { get; private set; }
	public InventorySlot FuelSlot { get; private set; }
	public InventorySlot OutputSlot { get; private set; }

	public float BurnTimeRemaining { get; private set; } = 0f;
	public float CurrentFuelBurnDuration { get; private set; } = 0f;
	public float ProcessProgress { get; private set; } = 0f;
	public float CurrentRecipeDuration { get; private set; } = 0f;

	private ProcessingManager _processingManager;

	public override void _Ready()
	{
		InputSlot = new InventorySlot();
		FuelSlot = new InventorySlot();
		OutputSlot = new InventorySlot();

		_processingManager = GetNodeOrNull<ProcessingManager>("/root/ProcessingManager");
	}

	public void Tick(double delta)
	{
		float dt = (float)delta;
		bool changed = false;

		if (_processingManager == null)
			return;

		bool canProcess = CanProcessCurrentInput(out ProcessingRecipe recipe, out ItemDefinition outputItem);

		if (BurnTimeRemaining > 0f)
		{
			float oldBurn = BurnTimeRemaining;
			BurnTimeRemaining = Mathf.Max(0f, BurnTimeRemaining - dt);

			if (!Mathf.IsEqualApprox(oldBurn, BurnTimeRemaining))
				changed = true;
		}

		bool isBurning = BurnTimeRemaining > 0f;

		// If not currently burning, try to consume one fuel item.
		if (!isBurning && canProcess)
		{
			if (TryConsumeFuel())
			{
				isBurning = true;
				changed = true;
			}
		}

		// Only progress while actively burning.
		if (isBurning && canProcess)
		{
			if (!Mathf.IsEqualApprox(CurrentRecipeDuration, recipe.ProcessTimeSeconds))
			{
				CurrentRecipeDuration = recipe.ProcessTimeSeconds;
				changed = true;
			}

			ProcessProgress += dt;
			changed = true;

			if (ProcessProgress >= CurrentRecipeDuration)
			{
				ProcessProgress -= CurrentRecipeDuration;
				CompleteOneProcess(recipe, outputItem);
				changed = true;

				if (!CanProcessCurrentInput(out _, out _))
				{
					ProcessProgress = 0f;
					CurrentRecipeDuration = 0f;
					changed = true;
				}
			}
		}
		else
		{
			// Reset only if there is no valid input/output recipe.
			// If fuel ran out, keep progress preserved.
			if (!canProcess)
			{
				if (ProcessProgress != 0f || CurrentRecipeDuration != 0f)
					changed = true;

				ProcessProgress = 0f;
				CurrentRecipeDuration = 0f;
			}
		}

		if (changed)
			EmitSignal(SignalName.ProcessingChanged);
	}

	public bool CanAcceptInput(ItemDefinition item)
	{
		if (item == null)
			return false;

		bool result = _processingManager != null &&
					  _processingManager.CanProcessAtStation(StationType, item.ItemId);

		GD.Print($"[{StationType}] CanAcceptInput {item.ItemId}: {result}");
		return result;
	}

	public bool CanAcceptFuel(ItemDefinition item)
	{
		if (item == null)
			return false;

		bool result = _processingManager != null &&
					  _processingManager.IsValidFuel(item.ItemId);

		GD.Print($"[{StationType}] CanAcceptFuel {item.ItemId}: {result}");
		return result;
	}

	public bool CanTakeOutputInto(ItemDefinition heldItem)
	{
		if (OutputSlot == null || OutputSlot.IsEmpty)
			return false;

		if (heldItem == null)
			return true;

		return OutputSlot.CanStackWith(heldItem);
	}

	private bool CanProcessCurrentInput(out ProcessingRecipe recipe, out ItemDefinition outputItem)
	{
		recipe = null;
		outputItem = null;

		if (_processingManager == null)
			return false;

		if (InputSlot == null || InputSlot.IsEmpty || InputSlot.Item == null)
			return false;

		if (!_processingManager.TryGetRecipe(StationType, InputSlot.Item.ItemId, out recipe))
			return false;

		outputItem = _processingManager.GetOutputItem(recipe.OutputItemId);
		if (outputItem == null)
			return false;

		if (OutputSlot.IsEmpty)
			return true;

		if (!OutputSlot.CanStackWith(outputItem))
			return false;

		return OutputSlot.Count < OutputSlot.Item.MaxStackSize;
	}

	private bool TryConsumeFuel()
	{
		if (FuelSlot == null || FuelSlot.IsEmpty || FuelSlot.Item == null || _processingManager == null)
			return false;

		float burnDuration = _processingManager.GetFuelBurnTime(FuelSlot.Item.ItemId);
		if (burnDuration <= 0f)
			return false;

		FuelSlot.RemoveAmount(1);
		BurnTimeRemaining = burnDuration;
		CurrentFuelBurnDuration = burnDuration;
		return true;
	}

	private void CompleteOneProcess(ProcessingRecipe recipe, ItemDefinition outputItem)
	{
		if (InputSlot == null || InputSlot.IsEmpty)
			return;

		InputSlot.RemoveAmount(1);

		if (OutputSlot.IsEmpty)
			OutputSlot.SetItem(outputItem, 1);
		else if (OutputSlot.CanStackWith(outputItem))
			OutputSlot.Count += 1;
	}

	public float GetBurnProgress01()
	{
		if (CurrentFuelBurnDuration <= 0f)
			return 0f;

		return Mathf.Clamp(BurnTimeRemaining / CurrentFuelBurnDuration, 0f, 1f);
	}

	public float GetProcessProgress01()
	{
		if (CurrentRecipeDuration <= 0f)
			return 0f;

		return Mathf.Clamp(ProcessProgress / CurrentRecipeDuration, 0f, 1f);
	}
}
