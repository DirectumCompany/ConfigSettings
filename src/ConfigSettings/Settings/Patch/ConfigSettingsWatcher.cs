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

    private FileSystemWatcher watcher;

    private Timer debounceTimer;

    private Action changedHandler;

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

    private void TimerElapsedHandler(object sender, ElapsedEventArgs e)
    {
      this.changedHandler();
    }

    private FileSystemWatcher CreateWatcher(string filePath)
    {
      var watcher = new FileSystemWatcher
      {
        Path = Path.GetDirectoryName(filePath),
        Filter = Path.GetFileName(filePath),
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
      };
      // Cледим за изменением файла.
      watcher.Changed += this.FileChangedHandler;
      // Следим за созданием файла.
      watcher.Created += this.FileChangedHandler;
      // Следим за удалением файла.
      watcher.Deleted += this.FileChangedHandler;
      // Следим за переименованием файла.
      watcher.Renamed += this.FileRenamedHandler;
      // Включить слежение за файлом.
      watcher.EnableRaisingEvents = true;
      return watcher;
    }

    private Timer CreateTimer(TimeSpan waitBeforeNotify)
    {
      var timer = new Timer(waitBeforeNotify.TotalMilliseconds);
      timer.AutoReset = false;
      timer.Elapsed += this.TimerElapsedHandler;
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
    }

    #endregion

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="filePath">Путь к файлу, за которым надо наблюдать.</param>
    /// <param name="changedHandler">Обработчик изменения файла.</param>
    /// <param name="waitBeforeNotify">Отсрочка, по истечению которой, будет уведомление об изменении файла.</param>
    public ConfigSettingsWatcher(string filePath, Action changedHandler, TimeSpan waitBeforeNotify)
    {
      this.changedHandler = changedHandler;
      this.watcher = this.CreateWatcher(filePath);
      this.debounceTimer = this.CreateTimer(waitBeforeNotify);
    }
  }
}
