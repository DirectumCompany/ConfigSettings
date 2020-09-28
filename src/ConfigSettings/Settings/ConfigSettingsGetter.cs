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
    /// <summary>
    /// Парсер настроек.
    /// </summary>
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

    /// <summary>
    /// Получить блок в виде строки.
    /// </summary>
    /// <param name="name">Имя блока.</param>
    /// <returns>Строка.</returns>
    public string GetBlock(string name)
    {
      return this.configSettingsParser.GetBlockContent(name);
    }

    /// <summary>
    /// Получить блок.
    /// </summary>
    /// <param name="name">Имя блока.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    /// <returns>Типизированный блок.</returns>
    public T GetBlock<T>(string name) where T: class
    {
      return this.configSettingsParser.GetBlockContent<T>(name);
    }

    /// <summary>
    /// Установить блок.
    /// </summary>
    /// <param name="name">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="block">Типизированный блок.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    /// <returns>Типизированный блок.</returns>
    public void SetBlock<T>(string name, bool? isBlockEnabled, T block) where T: class
    {
      this.configSettingsParser.SetBlockValue(name, isBlockEnabled, block);
    }

    /// <summary>
    /// Получить блок в виде XML.
    /// </summary>
    /// <param name="name">Имя блока.</param>
    /// <returns>XML блока.</returns>
    public XElement GetXmlBlock(string name)
    {
      return this.configSettingsParser.GetXmlBlockContent(name);
    }

    /// <summary>
    /// Установить значение.
    /// </summary>
    /// <param name="name">Имя переменной.</param>
    /// <param name="value">Значение переменной.</param>
    /// <typeparam name="T">Тип переменной.</typeparam>
    public void Set<T>(string name, T value)
    {
      this.configSettingsParser.AddOrUpdateVariable(name, value.ToString());
    }

    /// <summary>
    /// Установить значение блока.
    /// </summary>
    /// <param name="name">Имя блока.</param>
    /// <param name="enabled">Доступность блока.</param>
    /// <param name="value">Значение блока.</param>
    public void SetBlock(string name, bool? enabled, string value)
    {
      this.configSettingsParser.SetBlockValue(name, enabled, value);
    }

    /// <summary>
    /// Задать импорт.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void SetImport(string filePath)
    {
      this.configSettingsParser.SetImportFrom(filePath);
    }

    /// <summary>
    /// Сохранить.
    /// </summary>
    public void Save()
    {
      this.configSettingsParser.Save();
    }

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="configSettingsParser">Парсер настроек.</param>
    public ConfigSettingsGetter(ConfigSettingsParser configSettingsParser)
    {
      this.configSettingsParser = configSettingsParser ?? new ConfigSettingsParser(null);
    }

    /// <summary>
    /// Конструтор.
    /// </summary>
    public ConfigSettingsGetter() : this(ConfigSettingsResolver.CreateDefaultConfigSettingsParser())
    {
    }
  }
}
