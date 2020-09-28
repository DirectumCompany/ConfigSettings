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

    public string FilePath { get; }

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    protected bool Equals(BlockSetting other)
    {
      return Name == other.Name && FilePath == other.FilePath;
    }

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
    /// <param name="isEnabled"></param>
    /// <param name="content"></param>
    /// <param name="contentWithoutRoot"></param>
    /// <param name="comments"></param>
    public void Update(bool? isEnabled, string content = null, string contentWithoutRoot = null,
      IReadOnlyList<string> comments = null)
    {
      this.IsEnabled = isEnabled;
      this.Content = content;
      this.ContentWithoutRoot = contentWithoutRoot;
      this.Comments = comments;
    }

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
