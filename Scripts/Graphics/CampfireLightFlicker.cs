using Godot;

public partial class CampfireLightFlicker : OmniLight3D
{
	[Export] public float BaseEnergy = 2.2f;
	[Export] public float FlickerAmount = 0.35f;
	[Export] public float FlickerSpeed = 5.0f;
	[Export] public Color BaseColor = new Color(1.0f, 1.0f, 0.694f);

	private float _time;

	public override void _Process(double delta)
	{
		_time += (float)delta;

		float noise =
			Mathf.Sin(_time * FlickerSpeed) * 0.5f +
			Mathf.Sin(_time * FlickerSpeed * 1.7f) * 0.3f +
			Mathf.Sin(_time * FlickerSpeed * 2.3f) * 0.2f;

		LightEnergy = BaseEnergy + noise * FlickerAmount;

		float colorShift = Mathf.Sin(_time * 3.0f) * 0.05f;
		LightColor = new Color(
			BaseColor.R,
			BaseColor.G - colorShift * 0.3f,
			BaseColor.B - colorShift * 0.2f
		);
	}
}
