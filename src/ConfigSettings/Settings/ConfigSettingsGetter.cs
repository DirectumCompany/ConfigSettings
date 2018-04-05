using System;
using ConfigSettings.Patch;

namespace ConfigSettings
{
  /// <summary>
  /// Получатель настроек.
  /// </summary>
  public class ConfigSettingsGetter
  {
    private readonly ConfigSettingsParser configSettingsParser;

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
      if (!this.configSettingsParser.HasVariable(name))
        return getDefaultValue();

      var value = this.configSettingsParser.GetVariableValue(name);
      if (string.IsNullOrEmpty(value))
        return getDefaultValue();

      return (T)Convert.ChangeType(value, typeof(T));
    }

    public void Set<T>(string name, T value)
    {
      this.configSettingsParser.SetVariableValue(name, value.ToString());
    }

    public void SetBlock(string name, bool? enabled, string value)
    {
      this.configSettingsParser.SetBlockValue(name, enabled, value);
    }

    public void SetImport(string filePath)
    {
      this.configSettingsParser.SetImportFrom(filePath);
    }

    public void Save()
    {
      this.configSettingsParser.Save();
    }

    public ConfigSettingsGetter(ConfigSettingsParser configSettingsParser)
    {
      this.configSettingsParser = configSettingsParser ?? new ConfigSettingsParser(null);
    }

    public ConfigSettingsGetter() : this(ConfigSettingsResolver.DefaultConfigSettingsParser)
    {
    }
  }
}
