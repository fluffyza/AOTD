using Godot;

public partial class TorchFlicker : Node3D
{
	[Export] public NodePath LightPath;

	[Export] public float BaseEnergy = 1.4f;
	[Export] public float EnergyFlickerAmount = 0.18f;

	[Export] public float BaseRange = 4.5f;
	[Export] public float RangeFlickerAmount = 0.12f;

	[Export] public float FlickerSpeed = 2.8f;

	private OmniLight3D _light;
	private float _timeOffset;
	private float _noiseOffset;

	public override void _Ready()
	{
		_light = GetNodeOrNull<OmniLight3D>(LightPath);

		_timeOffset = GD.Randf() * 100f;
		_noiseOffset = GD.Randf() * 100f;

		if (_light != null)
		{
			_light.LightEnergy = BaseEnergy;
			_light.OmniRange = BaseRange;
		}
	}

	public override void _Process(double delta)
	{
		if (_light == null)
			return;

		float t = (float)Time.GetTicksMsec() / 1000f;

		float wave1 = Mathf.Sin((t + _timeOffset) * FlickerSpeed);
		float wave2 = Mathf.Sin((t + _timeOffset * 0.73f) * (FlickerSpeed * 1.91f));
		float wave3 = Mathf.Sin((t + _noiseOffset * 1.37f) * (FlickerSpeed * 0.63f));

		float combined = (wave1 * 0.5f) + (wave2 * 0.35f) + (wave3 * 0.15f);

		float normalized = (combined + 1f) * 0.5f;

		_light.LightEnergy = BaseEnergy + ((normalized - 0.5f) * 2f * EnergyFlickerAmount);
		_light.OmniRange = BaseRange + ((normalized - 0.5f) * 2f * RangeFlickerAmount);
	}
}
