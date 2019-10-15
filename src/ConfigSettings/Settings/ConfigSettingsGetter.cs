using System;
using System.ComponentModel;
using System.Xml.Linq;
using ConfigSettings.Patch;

namespace ConfigSettings
{
  /// <summary>
  /// Получатель настроек.
  /// </summary>
  public class ConfigSettingsGetter
  {
    protected readonly ConfigSettingsParser configSettingsParser;

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
    public virtual T Get<T>(string name, Func<T> getDefaultValue)
    {
      if (!this.configSettingsParser.HasVariable(name))
        return getDefaultValue();

      var value = this.configSettingsParser.GetVariableValue(name);
      if (string.IsNullOrEmpty(value))
        return getDefaultValue();

      var converter = TypeDescriptor.GetConverter(typeof(T));
      return converter.CanConvertFrom(typeof(string))
        ? (T)converter.ConvertFrom(value)
        : getDefaultValue();
    }

    public string GetBlock(string name)
    {
      return this.configSettingsParser.GetBlockContent(name);
    }

    public T GetBlock<T>(string name) where T: class
    {
      return this.configSettingsParser.GetBlockContent<T>(name);
    }

    public XElement GetXmlBlock(string name)
    {
      return this.configSettingsParser.GetXmlBlockContent(name);
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

    public ConfigSettingsGetter() : this(ConfigSettingsResolver.CreateDefaultConfigSettingsParser())
    {
    }
  }
}
