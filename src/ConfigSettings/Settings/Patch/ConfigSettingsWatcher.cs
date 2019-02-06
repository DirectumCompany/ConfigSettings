using System;
using System.IO;
using System.Timers;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Наблюдатель за файлом настроек.
  /// </summary>
  public class ConfigSettingsWatcher : IDisposable
  {
    #region Поля и свойства

    private string filePath;

    private FileSystemWatcher watcher;

    private Timer debounceTimer;

    private Timer checkFileExistsTimer;

    private Action fileChangedHandler;

    private TimeSpan waitBeforeNotify;

    #endregion

    #region Методы

    private void FileChangedHandler(object sender, FileSystemEventArgs e)
    {
      // Эмуляция debounce с помощью таймера.
      this.debounceTimer.Stop();
      this.debounceTimer.Start();
    }

    private void FileRenamedHandler(object sender, RenamedEventArgs e)
    {
      // Эмуляция debounce с помощью таймера.
      this.debounceTimer.Stop();
      this.debounceTimer.Start();
    }

    private void DebounceHandler(object sender, ElapsedEventArgs e)
    {
      this.fileChangedHandler();
    }

    private void CheckFileExistsHandler(object sender, ElapsedEventArgs e)
    {
      if (File.Exists(this.filePath))
        this.fileChangedHandler();
    }

    private FileSystemWatcher CreateWatcher()
    {
      var watcher = new FileSystemWatcher
      {
        Path = Path.GetDirectoryName(this.filePath),
        Filter = Path.GetFileName(this.filePath),
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
      };
      // Cледим за изменением файла.
      watcher.Changed += this.FileChangedHandler;
      // Следим за удалением файла.
      watcher.Deleted += this.FileChangedHandler;
      // Следим за переименованием файла.
      watcher.Renamed += this.FileRenamedHandler;
      // Включить слежение за файлом.
      watcher.EnableRaisingEvents = true;
      return watcher;
    }

    private Timer CreateDebounceTimer()
    {
      var timer = new Timer(this.waitBeforeNotify.TotalMilliseconds);
      timer.AutoReset = false;
      timer.Elapsed += this.DebounceHandler;
      return timer;
    }

    private Timer CreateCheckFileExistsTimer()
    {
      var timer = new Timer(this.waitBeforeNotify.TotalMilliseconds);
      timer.AutoReset = true;
      timer.Elapsed += this.CheckFileExistsHandler;
      return timer;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
      if (this.watcher != null)
      {
        this.watcher.Dispose();
        this.watcher = null;
      }

      if (this.debounceTimer != null)
      {
        this.debounceTimer.Dispose();
        this.debounceTimer = null;
      }

      if (this.checkFileExistsTimer != null)
      {
        this.checkFileExistsTimer.Dispose();
        this.checkFileExistsTimer = null;
      }

    }

    #endregion

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="filePath">Путь к файлу, за которым надо наблюдать.</param>
    /// <param name="changedHandler">Обработчик изменения файла.</param>
    /// <param name="waitBeforeNotify">Отсрочка, по истечению которой, будет уведомление об изменении файла.</param>
    public ConfigSettingsWatcher(string filePath, Action fileChangedHandler, TimeSpan waitBeforeNotify)
    {
      this.filePath = Path.GetFullPath(filePath);
      this.fileChangedHandler = fileChangedHandler;
      this.waitBeforeNotify = waitBeforeNotify;

      if (!File.Exists(filePath))
      {
        this.checkFileExistsTimer = this.CreateCheckFileExistsTimer();
        this.checkFileExistsTimer.Start();
      }
      else
      {
        this.debounceTimer = this.CreateDebounceTimer();
        this.watcher = this.CreateWatcher();
      }
    }
  }
}
