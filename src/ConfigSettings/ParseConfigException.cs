using System;
using System.Runtime.Serialization;

namespace ConfigSettings
{
  /// <summary>
  /// Ошибка парсинга конфигурационного файла.
  /// </summary>
  [Serializable]
  public class ParseConfigException : Exception
  {
    #region Поля и свойства

    /// <summary>
    /// Путь к файлу, который не удалось распарсить.
    /// </summary>
    public string CorruptedFilePath { get; }

    #endregion

    #region Базовый класс

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      base.GetObjectData(info, context);
      info.AddValue(nameof(this.CorruptedFilePath), this.CorruptedFilePath);
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    public ParseConfigException()
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public ParseConfigException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="corruptedFilePath">Путь к файлу, который не удалось распарсить.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public ParseConfigException(string corruptedFilePath, string message, Exception innerException)
      : base(message, innerException)
    {
      this.CorruptedFilePath = corruptedFilePath;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="info">Сериализованные данные об исключении.</param>
    /// <param name="context">Информация о контексте.</param>
    protected ParseConfigException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      this.CorruptedFilePath = info.GetString(nameof(this.CorruptedFilePath));
    }

    #endregion
  }
}
