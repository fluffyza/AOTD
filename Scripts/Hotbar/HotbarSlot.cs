using Godot;

public partial class HotbarSlot : Panel
{
	private Label _label;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_label = GetNode<Label>("Label");
		_label.MouseFilter = MouseFilterEnum.Ignore;
	}

	public void SetSlot(string displayName, int count, bool selected)
	{
		if (string.IsNullOrEmpty(displayName) || count <= 0)
			_label.Text = "";
		else
			_label.Text = $"{displayName}\nx{count}";

		var style = new StyleBoxFlat();
		style.BgColor = selected ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.15f, 0.15f, 0.15f);
		style.BorderColor = selected ? Colors.Yellow : new Color(0.4f, 0.4f, 0.4f);
		style.BorderWidthLeft = 3;
		style.BorderWidthTop = 3;
		style.BorderWidthRight = 3;
		style.BorderWidthBottom = 3;

		AddThemeStyleboxOverride("panel", style);
	}
}
