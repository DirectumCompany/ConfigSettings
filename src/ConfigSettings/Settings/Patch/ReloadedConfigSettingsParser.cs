using System;
using System.Collections.Generic;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Парсер, который перечитывает файл настроек при его изменении.
  /// </summary>
  public class ReloadedConfigSettingsParser : ConfigSettingsParser, IDisposable
  {
    #region Поля и свойства

    private readonly IList<ConfigSettingsWatcher> watchers = new List<ConfigSettingsWatcher>();

    private readonly Action reloadHandler;

    private readonly Action<Exception> errorHandler;

    private readonly TimeSpan waitBeforeReload;

    #endregion

    #region Методы

    protected override void ParseSettingsSource(string settingsFilePath)
    {
      if (!string.Equals(settingsFilePath, Constants.UnexistedConfigSettingsPath))
      {
        var watcher = new ConfigSettingsWatcher(settingsFilePath, this.Reload, this.waitBeforeReload);
        this.watchers.Add(watcher);
      }
      base.ParseSettingsSource(settingsFilePath);
    }

    /// <summary>
    /// Удалить всех наблюдателей.
    /// </summary>
    private void ClearWatchers()
    {
      foreach (var watcher in this.watchers)
        watcher.Dispose();
      this.watchers.Clear();
    }

    /// <summary>
    /// Перезагрузать файл настроек.
    /// </summary>
    private void Reload()
    {
      this.ClearWatchers();
      this.ClearAllCaches();
      try
      {
        this.ParseRootSettingsSource();
      }
      catch (Exception ex)
      {
        this.errorHandler?.Invoke(ex);
      }
      this.reloadHandler?.Invoke();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
      this.ClearWatchers();
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    public ReloadedConfigSettingsParser(string settingsFilePath)
      : this(settingsFilePath, null, null)
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="errorHandler">Обработчик изменения файла настроек.</param>
    public ReloadedConfigSettingsParser(string settingsFilePath, Action reloadHandler, Action<Exception> errorHandler)
      : this(settingsFilePath, reloadHandler, errorHandler, null)
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="errorHandler">Обработчик изменения файла настроек.</param>
    /// <param name="waitBeforeReload">Отсрочка, по истечению которой, перечитываются настройки из измененнного файла.</param>
    public ReloadedConfigSettingsParser(string settingsFilePath, Action reloadHandler, Action<Exception> errorHandler, TimeSpan? waitBeforeReload)
    {
      this.reloadHandler = reloadHandler;
      this.errorHandler = errorHandler;
      this.waitBeforeReload = waitBeforeReload ?? TimeSpan.FromSeconds(5);
      this.rootSettingsFilePath = settingsFilePath;
      this.ParseRootSettingsSource();
    }

    #endregion
  }
}