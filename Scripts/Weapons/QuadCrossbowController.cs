using System;
using Godot;

public partial class QuadCrossbowController : Node2D
{
	[Export] public TextureRect QuadCrossbowIdle;
	[Export] public AnimatedSprite2D QuadCrossbowAnimations;
	[Export] public PackedScene ArrowProjectileScene;

	public event Action RequestAmmoConsume;

	private enum State
	{
		Ready,
		Shooting,
		SpinningAfterShot,
		ReloadingBolt,
		SpinningReloadBolt
	}

	private State _state = State.Ready;
	private Player _player;
	private string _loadedAmmoId = "";
	private int _shotsRemaining = 4;
	private int _reloadSpinCount = 0;

	public bool IsBusy => _state != State.Ready;

	public override void _Ready()
	{
		if (QuadCrossbowAnimations != null)
		{
			QuadCrossbowAnimations.Stop();
			QuadCrossbowAnimations.Visible = false;
			QuadCrossbowAnimations.Frame = 0;
			QuadCrossbowAnimations.AnimationFinished += OnAnimationFinished;
		}
	}

	public void TryFire(Player player, string ammoId)
	{
		if (_state != State.Ready)
			return;

		if (_shotsRemaining <= 0)
			return;

		if (string.IsNullOrEmpty(ammoId))
		{
			GD.Print("Quad crossbow has no valid ammo above it.");
			return;
		}

		_player = player;
		_loadedAmmoId = ammoId;

		RequestAmmoConsume?.Invoke();

		SpawnProjectile();

		_shotsRemaining--;

		_state = State.Shooting;
		PlayAnimation("shoot");
	}

	private void OnAnimationFinished()
	{
		if (_state == State.Shooting)
		{
			_state = State.SpinningAfterShot;
			PlayAnimation("spin");
			return;
		}

		if (_state == State.SpinningAfterShot)
		{
			if (_shotsRemaining <= 0)
			{
				_reloadSpinCount = 0;
				_state = State.ReloadingBolt;
				PlayAnimation("reload");
				return;
			}

			FinishToIdle();
			return;
		}

		if (_state == State.ReloadingBolt)
		{
			_state = State.SpinningReloadBolt;
			PlayAnimation("spin");
			return;
		}

		if (_state == State.SpinningReloadBolt)
		{
			_reloadSpinCount++;

			if (_reloadSpinCount < 4)
			{
				_state = State.ReloadingBolt;
				PlayAnimation("reload");
				return;
			}

			_shotsRemaining = 4;
			FinishToIdle();
		}
	}

	private void PlayAnimation(string animationName)
	{
		if (QuadCrossbowIdle != null)
			QuadCrossbowIdle.Visible = false;

		if (QuadCrossbowAnimations == null)
			return;

		QuadCrossbowAnimations.Visible = true;
		QuadCrossbowAnimations.Stop();
		QuadCrossbowAnimations.Frame = 0;
		QuadCrossbowAnimations.Play(animationName);
	}

	private void FinishToIdle()
	{
		_state = State.Ready;
		_loadedAmmoId = "";

		if (QuadCrossbowAnimations != null)
		{
			QuadCrossbowAnimations.Stop();
			QuadCrossbowAnimations.Visible = false;
			QuadCrossbowAnimations.Frame = 0;
		}

		if (QuadCrossbowIdle != null)
			QuadCrossbowIdle.Visible = true;
	}

	public void ResetVisual()
	{
		if (QuadCrossbowAnimations != null)
		{
			QuadCrossbowAnimations.Stop();
			QuadCrossbowAnimations.Visible = false;
			QuadCrossbowAnimations.Frame = 0;
		}

		if (QuadCrossbowIdle != null)
			QuadCrossbowIdle.Visible = false;
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
