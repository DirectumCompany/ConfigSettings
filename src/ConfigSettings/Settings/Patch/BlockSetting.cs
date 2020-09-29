using System.Collections.Generic;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Настройки блока.
  /// </summary>
  public class BlockSetting
  {
    /// <summary>
    /// Имя блока.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Доступность блока.
    /// </summary>
    public bool? IsEnabled { get; private set;}

    /// <summary>
    /// Содержимое блока.
    /// </summary>
    public string Content { get; private set;}
    
    /// <summary>
    /// Содержимое блока без корневого заголовка.
    /// </summary>
    public string ContentWithoutRoot { get; private set; }
 
    /// <summary>
    /// Комментарии.
    /// </summary>
    public IReadOnlyList<string> Comments { get; private set;}

    /// <summary>
    /// Источник настройки.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    /// <summary>
    /// Equality.
    /// </summary>
    /// <param name="other">Object.</param>
    /// <returns>True, if Equals.</returns>
    protected bool Equals(BlockSetting other)
    {
      return Name == other.Name && FilePath == other.FilePath;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (FilePath != null ? FilePath.GetHashCode() : 0);
      }
    }

    /// <summary>
    /// Обновить свойства блока.
    /// </summary>
    /// <param name="isEnabled">Доступность.</param>
    /// <param name="content">Содержимое с корневым элементом.</param>
    /// <param name="contentWithoutRoot">Содержимое без корневого элемента.</param>
    /// <param name="comments">Комментарии.</param>
    public void Update(bool? isEnabled, string content = null, string contentWithoutRoot = null,
      IReadOnlyList<string> comments = null)
    {
      this.IsEnabled = isEnabled;
      this.Content = content;
      this.ContentWithoutRoot = contentWithoutRoot;
      this.Comments = comments;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="filePath">Источник настройки.</param>
    /// <param name="name">Имя блока.</param>
    /// <param name="isEnabled">Доступность.</param>
    /// <param name="content">Содержимое с корневым элементом.</param>
    /// <param name="contentWithoutRoot">Содержимое без корневого элемента.</param>
    /// <param name="comments">Комментарии.</param>
    public BlockSetting(string filePath, string name,  bool? isEnabled, string content = null, string contentWithoutRoot = null, IReadOnlyList<string> comments = null)
    {
      this.FilePath = filePath;
      this.Name = name;
      this.IsEnabled = isEnabled;
      this.Content = content;
      this.ContentWithoutRoot = contentWithoutRoot;
      this.Comments = comments;
    }
  }
}
