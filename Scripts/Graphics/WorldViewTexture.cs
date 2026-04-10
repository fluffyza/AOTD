using Godot;

public partial class WorldViewTexture : TextureRect
{
	[Export] public NodePath SubViewportPath;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var viewport = GetNodeOrNull<SubViewport>(SubViewportPath);
		if (viewport == null)
		{
			GD.PrintErr("WorldViewTexture: SubViewport not found.");
			return;
		}

		Texture = viewport.GetTexture();
	}
}
