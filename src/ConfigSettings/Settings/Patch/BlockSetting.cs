namespace ConfigSettings.Patch
{
  /// <summary>
  /// Настройки блока.
  /// </summary>
  internal class BlockSetting
  {
    /// <summary>
    /// Доступность блока.
    /// </summary>
    public bool? IsEnabled { get; }

    /// <summary>
    /// Содержимое блока.
    /// </summary>
    public string Content { get; }

    public BlockSetting(bool? isEnabled, string content)
    {
      this.IsEnabled = isEnabled;
      this.Content = content;
    }
  }
}
