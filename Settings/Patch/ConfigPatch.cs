using System.Collections.Generic;
using System.Xml.Linq;

namespace CommonLibrary.Settings.Patch
{
  /// <summary>
  /// Класс для коррекции конфига на основании настроек.
  /// </summary>
  public class ConfigPatch
  {
    #region Поля и свойства

    /// <summary>
    /// Xml-конфиг.
    /// </summary>
    private readonly XDocument config;

    /// <summary>
    /// Настройки конфига.
    /// </summary>
    private readonly ConfigSettings configSettings;

    #endregion

    #region Методы

    /// <summary>
    /// Скорректировать конфиг на основании настроек.
    /// </summary>
    public void Patch()
    {
      new BlockAccessor(this.config).ApplyAccess(this.configSettings);
      this.VisitElement(this.config.Root, new List<List<BaseSetter>>());
    }

    /// <summary>
    /// Обработать xml-элемент конфига.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="setters">Набор "установщиков" с учетом текущего вложения элемента.</param>
    private void VisitElement(XElement element, List<List<BaseSetter>> setters)
    {
      this.ApplySetters(element, setters);
      var currentSetters = new List<BaseSetter>();
      setters.Add(currentSetters);
      foreach (var node in element.Nodes())
      {
        var commentNode = node as XComment;
        var elementNode = node as XElement;
        if (commentNode != null)
          this.VisitComment(commentNode, currentSetters);
        else if (elementNode != null)
        {
          this.VisitElement(elementNode, setters);
          currentSetters.Clear();
        }
      }
      setters.RemoveAt(setters.Count - 1);
    }

    /// <summary>
    /// Обработать комментарий.
    /// </summary>
    /// <param name="commentNode">Узел комментария.</param>
    /// <param name="setters">Набор "установщиков".</param>
    private void VisitComment(XComment commentNode, List<BaseSetter> setters)
    {
      var comment = commentNode.Value.Trim();
      if (comment.Length > 2 && comment[0] == '{' && comment[comment.Length - 1] == '}')
      {
        comment = comment.Substring(1, comment.Length - 2);
        var setterExpressions = this.GetSetterExpressions(comment);
        foreach (var setterExpression in setterExpressions)
        {
          if (setterExpression[0] == '@')
          {
            if (setterExpression.Length > 1 && setterExpression[1] == '=')
              setters.Add(new TextSetter(setterExpression.Substring(2)));
            else
            {
              var equalsIndex = setterExpression.IndexOf('=');
              if (equalsIndex > 0)
              {
                var attributeName = setterExpression.Substring(1, equalsIndex - 1);
                var attributeExpression = setterExpression.Substring(equalsIndex + 1);
                setters.Add(new AttributeSetter(attributeName, attributeExpression));
              }
            }
          }
        }
      }
      else
        setters.Clear();
    }

    /// <summary>
    /// Разбить текст на части, представляющие собой выражения конкретных "установщиков".
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <returns>Выражения "установщиков".</returns>
    private IEnumerable<string> GetSetterExpressions(string text)
    {
      var result = new List<string>();
      var insideStringLiteral = false;
      var functionNestedLevel = 0;
      var currentPosition = 0;
      for (int i = 0; i < text.Length; i++)
      {
        var symbol = text[i];
        if (symbol == '"')
          insideStringLiteral = !insideStringLiteral;
        else if (symbol == '(' && !insideStringLiteral)
          functionNestedLevel++;
        else if (symbol == ')' && !insideStringLiteral)
          functionNestedLevel--;
        else if (symbol == ' ' && !insideStringLiteral && functionNestedLevel == 0)
        {
          if (currentPosition < i)
            result.Add(text.Substring(currentPosition, i - currentPosition));
          currentPosition = i + 1;
        }
      }
      if (currentPosition < text.Length)
        result.Add(text.Substring(currentPosition));
      return result;
    }

    /// <summary>
    /// Применить к элементу набор "установщиков".
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="setters">Набор "установщиков" с учетом текущего вложения элемента.</param>
    private void ApplySetters(XElement element, List<List<BaseSetter>> setters)
    {
      for (int i = 0; i < setters.Count; i++)
      {
        var isAncestorSetter = i < setters.Count - 1;
        foreach (var setter in setters[i])
          setter.Apply(element, this.configSettings, isAncestorSetter);
      }
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    public ConfigPatch(XDocument config, ConfigSettings configSettings)
    {
      this.config = config;
      this.configSettings = configSettings;
    }

    #endregion
  }
}
