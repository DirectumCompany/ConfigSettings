using System.Collections.Generic;

namespace ConfigSettings.Settings.Patch
{
  /// <summary>
  /// Переменная настроек.
  /// </summary>
  public class VariableValue
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
    /// Комментарии.
    /// </summary>
    public List<string> Comments { get; set; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <param name="filePath">Источник хранения настройки.</param>
    public VariableValue(string value, string filePath)
    {
      this.Value = value;
      this.FilePath = filePath;
    }
  }
}
