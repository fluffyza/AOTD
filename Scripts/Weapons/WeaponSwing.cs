using System;
using Godot;

public partial class WeaponSwing : Node2D
{
	[Export] public Control WeaponHand;
	[Export] public string RequiredItemId = "iron_sword";

	[Export] public float SwingDuration = 0.28f;
	[Export] public Vector2 SwingOffset = new Vector2(35f, 10f);
	[Export] public float SwingRotationDegrees = -35f;

	public event Action Impact;

	private bool _isSwinging = false;
	private bool _isHeld = false;
	private bool _hitApplied = false;
	private float _timer = 0f;

	private Vector2 _basePosition;
	private float _baseRotation;

	public bool IsSwinging => _isSwinging;

	public override void _Ready()
	{
		SetProcess(true);

		if (WeaponHand != null)
		{
			_basePosition = WeaponHand.Position;
			_baseRotation = WeaponHand.Rotation;
		}
	}

	public override void _Process(double delta)
	{
		if (!_isSwinging || WeaponHand == null)
			return;

		_timer += (float)delta;

		float t = Mathf.Clamp(_timer / SwingDuration, 0f, 1f);
		float swing = Mathf.Sin(t * Mathf.Pi);

		WeaponHand.Position = _basePosition + SwingOffset * swing;
		WeaponHand.Rotation = _baseRotation + Mathf.DegToRad(SwingRotationDegrees) * swing;

		if (!_hitApplied && t >= 0.5f)
		{
			_hitApplied = true;
			Impact?.Invoke();
		}

		if (t >= 1f)
		{
			_isSwinging = false;
			_hitApplied = false;
			_timer = 0f;

			WeaponHand.Position = _basePosition;
			WeaponHand.Rotation = _baseRotation;

			if (_isHeld)
				StartSwing();
		}
	}

	public void StartSwing()
	{
		if (_isSwinging)
			return;

		_isSwinging = true;
		_hitApplied = false;
		_timer = 0f;
	}

	public void SetHeld(bool held)
	{
		_isHeld = held;
	}
}
