using System;
using System.ComponentModel;
using System.Xml.Linq;
using ConfigSettings.Patch;

namespace ConfigSettings
{
  /// <summary>
  /// Получатель настроек. Представляет собой обертку над <see cref="ConfigSettingsParser"/> с усеченным функционалом.
  /// Считывает настройки из корневого файла (указывается при создании).
  /// </summary>
  public class ConfigSettingsGetter
  {
    /// <summary>
    /// Функция преобразования не типизированного значения настройки.
    /// </summary>
    /// <remarks>
    /// Первым аргументом будет передано имя переменной, вторым - не типизированное значение.
    /// </remarks>
    public static Func<string, string, string> RawValueConverterDelegate { get; set; }

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
      if (!this.configSettingsParser.HasVariable(null, name))
        return getDefaultValue();

      var value = this.configSettingsParser.GetVariableValue(name);
      var convertedValue = RawValueConverterDelegate?.Invoke(name, value);

      if (!string.Equals(value, convertedValue, StringComparison.OrdinalIgnoreCase))
        value = convertedValue;

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
    public T GetBlock<T>(string name) where T : class
    {
      return this.configSettingsParser.GetBlockContent<T>(name);
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
