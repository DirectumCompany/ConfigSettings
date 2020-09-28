using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Перезагрузить файл настроек.
    /// </summary>
    private void Reload()
    {
      this.ClearWatchers();
      this.ClearAllCaches();
      this.TryParseRootSettingsSource();
      this.reloadHandler?.Invoke();
    }

    private void TryParseRootSettingsSource()
    {
      try
      {
        this.ParseRootSettingsSource();
      }
      catch (Exception ex)
      {
        this.errorHandler?.Invoke(ex);
      }
    }

    #endregion

    #region Базовый класс

    /// <inheritdoc />
    protected override void ParseSettingsSource(string settingsFilePath)
    {
      if (string.Equals(settingsFilePath, Constants.UnexistedConfigSettingsPath))
        return;

      if (!this.watchers.Any(w => w.FilePath.Equals(settingsFilePath, StringComparison.OrdinalIgnoreCase)))
      {
        var watcher = new ConfigSettingsWatcher(settingsFilePath, this.Reload, this.waitBeforeReload);
        this.watchers.Add(watcher);
      }
      base.ParseSettingsSource(settingsFilePath);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
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
    /// <param name="errorHandler">Обработчик ошибки парсинга файла настроек.</param>
    public ReloadedConfigSettingsParser(string settingsFilePath, Action reloadHandler, Action<Exception> errorHandler)
      : this(settingsFilePath, reloadHandler, errorHandler, null)
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="reloadHandler">Обработчик изменения файла настроек.</param>
    /// <param name="errorHandler">Обработчик ошибки парсинга файла настроек.</param>
    /// <param name="waitBeforeReload">Отсрочка, по истечении которой перечитываются настройки из измененнного файла.</param>
    public ReloadedConfigSettingsParser(string settingsFilePath, Action reloadHandler, Action<Exception> errorHandler, TimeSpan? waitBeforeReload)
    {
      this.reloadHandler = reloadHandler;
      this.errorHandler = errorHandler;
      this.waitBeforeReload = waitBeforeReload ?? TimeSpan.FromSeconds(5);
      this.RootSettingsFilePath = settingsFilePath;
      this.TryParseRootSettingsSource();
    }

    #endregion
  }
}