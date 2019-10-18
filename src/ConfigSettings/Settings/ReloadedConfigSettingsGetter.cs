using System;
using ConfigSettings.Patch;

namespace ConfigSettings
{
  /// <summary>
  /// Получатель настроек, который перечитывает файл настроек при его изменении.
  /// </summary>
  public class ReloadedConfigSettingsGetter: ConfigSettingsGetter, IDisposable
  {
    #region Методы

    private static ReloadedConfigSettingsParser CreateParser(Action reloadHandler, Action<Exception> errorHandler, TimeSpan? waitBeforeReload)
    {
      var configSettingsPath = ChangeConfig.GetActualConfigSettingsPath();
      return new ReloadedConfigSettingsParser(configSettingsPath, reloadHandler, errorHandler, waitBeforeReload);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
      ((ReloadedConfigSettingsParser)this.configSettingsParser).Dispose();
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    public ReloadedConfigSettingsGetter()
      : base(CreateParser(null, null, null))
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="parser">Парсер настроек.</param>
    public ReloadedConfigSettingsGetter(ReloadedConfigSettingsParser parser)
      : base(parser)
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="errorHandler">Обработчик ошибки.</param>
    public ReloadedConfigSettingsGetter(Action reloadHandler, Action<Exception> errorHandler)
      : base(CreateParser(reloadHandler, errorHandler, null))
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="waitBeforeReload">Отсрочка, по истечению которой, перечитываются настройки из измененнного файла.</param>
    /// <param name="errorHandler">Обработчик ошибки.</param>
    public ReloadedConfigSettingsGetter(Action reloadHandler, Action<Exception> errorHandler, TimeSpan? waitBeforeReload)
      : base(CreateParser(reloadHandler, errorHandler, waitBeforeReload))
    {
    }

    #endregion
  }
}
