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

    private static ReloadedConfigSettingsParser CreateParser(Action reloadHandler, TimeSpan? waitBeforeReload)
    {
      var configSettingsPath = ChangeConfig.GetActualConfigSettingsPath();
      return new ReloadedConfigSettingsParser(configSettingsPath, reloadHandler, waitBeforeReload);
    }

    #endregion

    #region IDisposable

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
      : base(CreateParser(null, null))
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
    public ReloadedConfigSettingsGetter(Action reloadHandler)
      : base(CreateParser(reloadHandler, null))
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="waitBeforeReload">Отсрочка, по истечению которой, перечитываются настройки из измененнного файла.</param>
    public ReloadedConfigSettingsGetter(Action reloadHandler, TimeSpan? waitBeforeReload)
      : base(CreateParser(reloadHandler, waitBeforeReload))
    {
    }

    #endregion
  }
}
