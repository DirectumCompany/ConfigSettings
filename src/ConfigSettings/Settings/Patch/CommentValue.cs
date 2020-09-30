namespace ConfigSettings.Settings.Patch
{
  /// <summary>
  /// Переменная комментария.
  /// </summary>
  internal class CommentValue
  {
    /// <summary>
    /// Значение настройки.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Источник хранения настройки.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <param name="filePath">Источник хранения.</param>
    public CommentValue(string value, string filePath)
    {
      this.Value = value;
      this.FilePath = filePath;
    }
  }
}
