using System;
using CommonLibrary.Settings.Patch;

namespace CommonLibrary.Settings
{
  /// <summary>
  /// Получатель настроек.
  /// </summary>
  public class ConfigSettingsGetter
  {
    private readonly ConfigSettings configSettings;

    public ConfigSettingsGetter()
    {
      this.configSettings = ConfigSettingsResolver.DefaultConfigSettings;
    }

    /// <summary>
    /// Получить настройку.
    /// </summary>
    /// <typeparam name="T">Тип настройки.</typeparam>
    /// <param name="name">Имя.</param>
    /// <returns>Настройка.</returns>
    public T Get<T>(string name)
    {
      return this.Get(name, default(T));
    }

    /// <summary>
    /// Получить настройку.
    /// </summary>
    /// <typeparam name="T">Тип настройки.</typeparam>
    /// <param name="name">Имя.</param>
    /// <param name="defaultValue">Значение по умолчанию.</param>
    /// <returns>Настройка.</returns>
    public T Get<T>(string name, T defaultValue)
    {
      return this.Get(name, () => defaultValue);
    }

    /// <summary>
    /// Получить настройку.
    /// </summary>
    /// <typeparam name="T">Тип настройки.</typeparam>
    /// <param name="name">Имя.</param>
    /// <param name="getDefaultValue">Функция для получения значения по умолчанию.</param>
    /// <returns>Настройка.</returns>
    public T Get<T>(string name, Func<T> getDefaultValue)
    {
      if (!this.configSettings.HasVariable(name))
        return getDefaultValue();

      var value = this.configSettings.GetVariableValue(name);
      if (string.IsNullOrEmpty(value))
        return getDefaultValue();

      return (T)Convert.ChangeType(value, typeof(T));
    }

    public void Set<T>(string name, T value)
    {
      this.configSettings.SetVariableValue(name, value.ToString());
    }
  }
}
