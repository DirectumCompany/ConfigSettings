using System.Collections.Generic;
using System.Data;
using System.IO;

namespace ConfigSettings.Settings.Patch
{
  /// <summary>
  /// Переменная настроек.
  /// </summary>
  public class ImportFrom
  {
    /// <summary>
    /// Откуда импортировать.
    /// </summary>
    public string From { get; private set; }
    
    /// <summary>
    /// Это корневой импорт.
    /// </summary>
    public bool IsRoot { get; }
    
    /// <summary>
    /// Источник хранения настройки.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Комментарии.
    /// </summary>
    public IReadOnlyList<string> Comments { get; private set;}

    /// <summary>
    /// Получить абсолютный путь.
    /// </summary>
    /// <returns></returns>
    public string GetAbsolutePath()
    {
      return Path.IsPathRooted(this.From) ? this.From : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FilePath), this.From));
    }
    
    /// <summary>
    /// Обновить импорт.
    /// </summary>
    /// <param name="comments"></param>
    public void Update(IReadOnlyList<string> comments = null)
    {
      this.Comments = comments;
    }

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    protected bool Equals(ImportFrom other)
    {
      return From == other.From && FilePath == other.FilePath;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((From != null ? From.GetHashCode() : 0) * 397) ^ (FilePath != null ? FilePath.GetHashCode() : 0);
      }
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="filePath">Источник хранения настройки.</param> 
    /// <param name="from">Имя.</param>
    /// <param name="isRoot">Это корневой импорт</param>
    /// <param name="comments">Комментарии.</param>
    public ImportFrom(string filePath, string from, bool isRoot = false, IReadOnlyList<string> comments = null)
    {
      this.FilePath = filePath;
      this.From = from;
      this.IsRoot = isRoot;
      this.Comments = comments;
    }
  }
}
