using Godot;

public partial class CraftingContainer : Node
{
	[Signal]
	public delegate void CraftingChangedEventHandler();

	[Export] public string StationType = "Backpack";
	[Export] public int InputSlotCount = 4;

	public InventorySlot[] InputSlots { get; private set; }
	public InventorySlot OutputPreviewSlot { get; private set; }

	public CraftingRecipe CurrentRecipe { get; private set; }
	public int CurrentCraftCount { get; private set; } = 0;

	private CraftingManager _craftingManager;

	public override void _Ready()
	{
		InputSlots = new InventorySlot[InputSlotCount];
		for (int i = 0; i < InputSlotCount; i++)
			InputSlots[i] = new InventorySlot();

		OutputPreviewSlot = new InventorySlot();
		_craftingManager = GetNodeOrNull<CraftingManager>("/root/CraftingManager");
	}

	public void SetInputSlot(int index, InventorySlot slotData)
	{
		if (index < 0 || index >= InputSlots.Length)
			return;

		InputSlots[index] = slotData ?? new InventorySlot();
		RefreshOutput();
	}

	public InventorySlot GetInputSlot(int index)
	{
		if (index < 0 || index >= InputSlots.Length)
			return null;

		return InputSlots[index];
	}

	public void RefreshOutput()
	{
		ClearRecipeState();

		if (_craftingManager == null)
		{
			EmitSignal(SignalName.CraftingChanged);
			return;
		}

		if (_craftingManager.TryGetCraftingResult(
			StationType,
			InputSlots,
			out CraftingRecipe recipe,
			out int craftCount,
			out ItemDefinition outputItem,
			out int outputAmount))
		{
			CurrentRecipe = recipe;
			CurrentCraftCount = craftCount;
			OutputPreviewSlot.SetItem(outputItem, outputAmount);
		}

		EmitSignal(SignalName.CraftingChanged);
	}

	public bool HasValidRecipe()
	{
		return CurrentRecipe != null &&
			   OutputPreviewSlot != null &&
			   !OutputPreviewSlot.IsEmpty &&
			   OutputPreviewSlot.Item != null &&
			   OutputPreviewSlot.Count > 0;
	}

	public bool TryCommitCraft()
	{
		if (!HasValidRecipe())
			return false;

		if (!_craftingManager.TryConsumeRecipeInputs(CurrentRecipe, CurrentCraftCount, InputSlots))
			return false;

		RefreshOutput();
		return true;
	}

	public void ClearAllInputs()
	{
		for (int i = 0; i < InputSlots.Length; i++)
			InputSlots[i] = new InventorySlot();

		RefreshOutput();
	}

	public void ClearOutputPreview()
	{
		OutputPreviewSlot = new InventorySlot();
		EmitSignal(SignalName.CraftingChanged);
	}

	private void ClearRecipeState()
	{
		CurrentRecipe = null;
		CurrentCraftCount = 0;
		OutputPreviewSlot = new InventorySlot();
	}
}
