using System.Configuration;

namespace CommonLibrary.Settings
{
  /// <summary>
  /// Базовая настройка.
  /// </summary>
  public abstract class BaseSetting : ConfigurationSection
  {
    /// <summary>
    /// Имя настройки.
    /// </summary>
    public abstract string SettingName { get; }
  }
}
