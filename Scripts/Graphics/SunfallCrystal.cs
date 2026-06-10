using Godot;

public partial class SunfallCrystal : MeshInstance3D
{
	[Export] public float MinEmission = 1.0f;
	[Export] public float MaxEmission = 6.0f;
	[Export] public float PulseSpeed = 2.0f;

	[Export] public OmniLight3D CrystalLight;
	[Export] public float MinLightEnergy = 0.4f;
	[Export] public float MaxLightEnergy = 2.5f;
	[Export] public float LightRange = 5.0f;

	private StandardMaterial3D _mat;

	public override void _Ready()
	{
		_mat = GetActiveMaterial(0).Duplicate() as StandardMaterial3D;
		SetSurfaceOverrideMaterial(0, _mat);

		_mat.EmissionEnabled = true;
		_mat.Emission = new Color(0.2f, 0.9f, 1.0f);

		if (CrystalLight != null)
		{
			CrystalLight.LightColor = new Color(0.2f, 0.9f, 1.0f);
			CrystalLight.OmniRange = LightRange;
		}
	}

	public override void _Process(double delta)
	{
		float t = (Mathf.Sin(Time.GetTicksMsec() * 0.001f * PulseSpeed) + 1.0f) * 0.5f;

		_mat.EmissionEnergyMultiplier = Mathf.Lerp(MinEmission, MaxEmission, t);

		if (CrystalLight != null)
			CrystalLight.LightEnergy = Mathf.Lerp(MinLightEnergy, MaxLightEnergy, t);
	}
}
