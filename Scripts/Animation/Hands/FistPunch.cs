using System;
using Godot;

public partial class FistPunch : Node2D
{
	[Export] public CanvasItem LeftHand;
	[Export] public CanvasItem RightHand;
	[Export] public CanvasItem PickaxeHand;

	[Export] public AnimatedSprite2D RightPunch;
	[Export] public AnimatedSprite2D LeftPunch;

	public event Action PunchImpact;

	private bool _isPunching = false;
	private bool _isHeld = false;
	private bool _pendingHit = false;

	public bool IsPunching => _isPunching;

	public override void _Ready()
	{
		if (RightPunch != null)
		{
			RightPunch.Visible = false;
			RightPunch.AnimationFinished += OnRightPunchFinished;
			RightPunch.FrameChanged += OnRightPunchFrameChanged;
		}

		if (LeftPunch != null)
			LeftPunch.Visible = false;
	}

	public void StartPunch()
	{
		if (_isPunching)
			return;

		_isPunching = true;
		_pendingHit = true;

		if (LeftHand != null) LeftHand.Visible = false;
		if (RightHand != null) RightHand.Visible = false;
		if (PickaxeHand != null) PickaxeHand.Visible = false;

		if (RightPunch != null)
		{
			RightPunch.Stop();
			RightPunch.Frame = 0;
			RightPunch.Visible = true;
			RightPunch.Play("fist punch");
		}

		if (LeftPunch != null)
		{
			LeftPunch.Stop();
			LeftPunch.Frame = 0;
			LeftPunch.Visible = true;
			LeftPunch.Play("fist punch left hand");
		}
	}

	public void SetHeld(bool held)
	{
		_isHeld = held;
	}

	public void StopPunch()
	{
		_isHeld = false;
		_isPunching = false;
		_pendingHit = false;

		if (RightPunch != null)
		{
			RightPunch.Stop();
			RightPunch.Visible = false;
		}

		if (LeftPunch != null)
		{
			LeftPunch.Stop();
			LeftPunch.Visible = false;
		}
	}

	private void OnRightPunchFrameChanged()
	{
		if (RightPunch == null || !_pendingHit)
			return;

		if (RightPunch.Frame == 3)
		{
			_pendingHit = false;
			PunchImpact?.Invoke();
		}
	}

	private void OnRightPunchFinished()
	{
		_isPunching = false;

		if (_isHeld)
		{
			StartPunch();
			return;
		}

		if (RightPunch != null)
		{
			RightPunch.Stop();
			RightPunch.Visible = false;
		}

		if (LeftPunch != null)
		{
			LeftPunch.Stop();
			LeftPunch.Visible = false;
		}

		if (LeftHand != null) LeftHand.Visible = true;
		if (RightHand != null) RightHand.Visible = true;
	}
}
