using System.Collections.Generic;
using System.Data;

namespace ConfigSettings.Settings.Patch
{
  /// <summary>
  /// Переменная настроек.
  /// </summary>
  public class Variable
  {
    /// <summary>
    /// Имя настройки.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Значение настройки.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Источник хранения настройки.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Комментарии.
    /// </summary>
    public IReadOnlyList<string> Comments { get; private set;}

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    protected bool Equals(Variable other)
    {
      return Name == other.Name && Value == other.Value;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
      }
    }

    public void Update(string value = null, IReadOnlyList<string> comments = null)
    {
      this.Value = value;
      this.Comments = comments;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="filePath">Источник хранения настройки.</param> 
    /// <param name="name">Имя.</param>
    /// <param name="value">Значение.</param>
    /// <param name="comments">Комментарии.</param>
    public Variable(string filePath, string name, string value = null, IReadOnlyList<string> comments = null)
    {
      this.FilePath = filePath;
      this.Name = name;
      this.Value = value;
      this.Comments = comments;
    }
  }
}
