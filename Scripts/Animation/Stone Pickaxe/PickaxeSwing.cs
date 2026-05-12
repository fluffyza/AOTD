using System;
using Godot;

public partial class PickaxeSwing : Node2D
{
	[Export] public CanvasItem LeftHand;
	[Export] public CanvasItem RightHand;
	[Export] public CanvasItem PickaxeHand;
	[Export] public CanvasItem IronSwordHandRight;

	[Export] public AnimatedSprite2D PickaxeSwingSprite;

	[Export] public string SwingAnimationName = "stone_pickaxe_swing";
	[Export] public int ImpactFrame = 4;

	public event Action PickaxeImpact;

	private bool _isSwinging = false;
	private bool _isHeld = false;
	private bool _pendingHit = false;

	public bool IsSwinging => _isSwinging;

	public override void _Ready()
	{
		if (PickaxeSwingSprite != null)
		{
			PickaxeSwingSprite.Visible = false;
			PickaxeSwingSprite.AnimationFinished += OnSwingFinished;
			PickaxeSwingSprite.FrameChanged += OnSwingFrameChanged;
		}
	}

	public void StartSwing()
	{
		if (_isSwinging)
			return;

		_isSwinging = true;
		_pendingHit = true;

		if (LeftHand != null) LeftHand.Visible = false;
		if (RightHand != null) RightHand.Visible = false;
		if (PickaxeHand != null) PickaxeHand.Visible = false;
		if (IronSwordHandRight != null) IronSwordHandRight.Visible = false;

		if (PickaxeSwingSprite != null)
		{
			PickaxeSwingSprite.Stop();
			PickaxeSwingSprite.Frame = 0;
			PickaxeSwingSprite.Visible = true;
			PickaxeSwingSprite.Play(SwingAnimationName);
		}
	}

	public void SetHeld(bool held)
	{
		_isHeld = held;
	}

	public void StopSwing()
	{
		_isHeld = false;
		_isSwinging = false;
		_pendingHit = false;

		if (PickaxeSwingSprite != null)
		{
			PickaxeSwingSprite.Stop();
			PickaxeSwingSprite.Visible = false;
		}
	}

	private void OnSwingFrameChanged()
	{
		if (PickaxeSwingSprite == null || !_pendingHit)
			return;

		if (PickaxeSwingSprite.Frame == ImpactFrame)
		{
			_pendingHit = false;
			PickaxeImpact?.Invoke();
		}
	}

	private void OnSwingFinished()
	{
		_isSwinging = false;

		if (_isHeld)
		{
			StartSwing();
			return;
		}

		if (PickaxeSwingSprite != null)
		{
			PickaxeSwingSprite.Stop();
			PickaxeSwingSprite.Visible = false;
		}

		if (PickaxeHand != null) PickaxeHand.Visible = true;
	}
}
