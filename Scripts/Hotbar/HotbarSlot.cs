using Godot;

public partial class HotbarSlot : Panel
{
	private Label _label;
	private Label _countLabel;
	private TextureRect _itemIcon;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		_label = GetNodeOrNull<Label>("Label");
		_countLabel = GetNodeOrNull<Label>("CountLabel");
		_itemIcon = GetNodeOrNull<TextureRect>("CenterContainer/ItemIcon");

		if (_label != null)
			_label.MouseFilter = MouseFilterEnum.Ignore;

		if (_countLabel != null)
			_countLabel.MouseFilter = MouseFilterEnum.Ignore;

		if (_itemIcon != null)
			_itemIcon.MouseFilter = MouseFilterEnum.Ignore;
	}

	public void SetSlot(string displayName, int count, bool selected, Texture2D icon)
	{
		bool hasItem = !string.IsNullOrEmpty(displayName) && count > 0;
		bool hasIcon = _itemIcon != null && icon != null;

		if (!hasItem)
		{
			if (_label != null)
			{
				_label.Text = "";
				_label.Visible = false;
			}

			if (_countLabel != null)
				_countLabel.Text = "";

			if (_itemIcon != null)
			{
				_itemIcon.Texture = null;
				_itemIcon.Visible = false;
			}
		}
		else
		{
			if (_itemIcon != null)
			{
				_itemIcon.Texture = icon;
				_itemIcon.Visible = hasIcon;
			}

			if (_label != null)
			{
				_label.Text = displayName;
				_label.Visible = !hasIcon;
			}

			if (_countLabel != null)
				_countLabel.Text = count > 1 ? $"x{count}" : "";
		}
		
		
		if (_itemIcon != null)
		{
			_itemIcon.MouseFilter = MouseFilterEnum.Ignore;
			_itemIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			_itemIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			_itemIcon.CustomMinimumSize = new Vector2(36, 36);
		}

		var style = new StyleBoxFlat();
		style.BgColor = selected ? new Color(0.30f, 0.30f, 0.30f) : new Color(0.10f, 0.10f, 0.10f);
		style.BorderColor = selected ? Colors.Yellow : new Color(0.45f, 0.45f, 0.45f);
		style.BorderWidthLeft = 4;
		style.BorderWidthTop = 4;
		style.BorderWidthRight = 4;
		style.BorderWidthBottom = 4;

		AddThemeStyleboxOverride("panel", style);
	}
}
