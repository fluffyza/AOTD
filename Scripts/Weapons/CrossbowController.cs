using System;
using Godot;

public partial class CrossbowController : Node2D
{
	[Export] public TextureRect CrossbowIdle;
	[Export] public AnimatedSprite2D CrossbowAnimations;
	[Export] public PackedScene ArrowProjectileScene;

	public event Action RequestAmmoConsume;

	private enum State
	{
		Ready,
		Firing,
		Reloading
	}

	private State _state = State.Ready;
	private Player _player;
	private string _loadedAmmoId = "";

	public bool IsBusy => _state != State.Ready;

	public override void _Ready()
	{
		if (CrossbowAnimations != null)
		{
			CrossbowAnimations.Visible = false;
			CrossbowAnimations.AnimationFinished += OnAnimationFinished;
		}
	}

	public void TryFire(Player player, string ammoId)
	{
		if (_state != State.Ready)
			return;

		if (string.IsNullOrEmpty(ammoId))
		{
			GD.Print("Crossbow has no valid ammo above it.");
			return;
		}

		_player = player;
		_loadedAmmoId = ammoId;

		RequestAmmoConsume?.Invoke();

		SpawnProjectile();

		_state = State.Firing;

		if (CrossbowIdle != null)
			CrossbowIdle.Visible = false;

		if (CrossbowAnimations != null)
		{
			CrossbowAnimations.Visible = true;
			CrossbowAnimations.Stop();
			CrossbowAnimations.Frame = 0;
			CrossbowAnimations.Play("crossbow_shoot");
		}
	}
	
	public void ResetVisual()
	{
		_state = State.Ready;
		_loadedAmmoId = "";

		if (CrossbowAnimations != null)
		{
			CrossbowAnimations.Stop();
			CrossbowAnimations.Visible = false;
			CrossbowAnimations.Frame = 0;
		}

		if (CrossbowIdle != null)
			CrossbowIdle.Visible = false;
	}

	private void OnAnimationFinished()
	{
		if (_state == State.Firing)
		{
			if (_player != null && _player.HasValidCrossbowAmmoAboveSelectedSlot())
			{
				_state = State.Reloading;

				if (CrossbowAnimations != null)
				{
					CrossbowAnimations.Stop();
					CrossbowAnimations.Frame = 0;
					CrossbowAnimations.Play("crossbow_reload");
				}

				return;
			}

			Finish();
			return;
		}

		if (_state == State.Reloading)
			Finish();
	}

	private void Finish()
	{
		_state = State.Ready;
		_loadedAmmoId = "";

		if (CrossbowAnimations != null)
		{
			CrossbowAnimations.Stop();
			CrossbowAnimations.Visible = false;
			CrossbowAnimations.Frame = 0;
		}

		if (CrossbowIdle != null)
			CrossbowIdle.Visible = true;
	}

	private void SpawnProjectile()
	{
		if (_player == null || ArrowProjectileScene == null)
			return;

		var arrow = ArrowProjectileScene.Instantiate<ArrowProjectile>();
		_player.GetTree().CurrentScene.AddChild(arrow);

		Vector3 aimPoint = _player.GetCrosshairAimPoint();

		Vector3 spawnPos =
			_player.GlobalPosition +
			(-_player.GlobalTransform.Basis.Z * 1.2f) +
			Vector3.Up * 1.25f;

		Vector3 fireDirection = (aimPoint - spawnPos).Normalized();

		int damage = 3;
		float speed = 17f;
		float gravity = 8f;
		bool isFireArrow = false;

		if (_loadedAmmoId == "iron_arrow")
		{
			damage = 5;
			speed = 22f;
			gravity = 4f;
		}
		else if (_loadedAmmoId == "fire_stone_arrow")
		{
			damage = 6;
			speed = 17f;
			gravity = 8f;
			isFireArrow = true;
		}

		arrow.GlobalPosition = spawnPos;
		arrow.Setup(fireDirection, damage, speed, gravity, isFireArrow);
	}
}
