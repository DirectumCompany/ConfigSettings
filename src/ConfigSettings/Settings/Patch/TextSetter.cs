using System.Linq;
using System.Xml.Linq;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Класс для установки значения текста xml-элемента конфига.
  /// </summary>
  internal class TextSetter : BaseSetter
  {
    /// <summary>
    /// Применить настройки к элементу.
    /// </summary>
    /// <param name="element">XML элемент.</param>
    /// <param name="configSettingsParser">Парсер настроек.</param>
    /// <param name="isAncestorSetter">Установлено в предке.</param>
    public override void Apply(XElement element, ConfigSettingsParser configSettingsParser, bool isAncestorSetter)
    {
      if (isAncestorSetter)
        return;
      var textNode =
        element.Nodes().OfType<XText>().FirstOrDefault(node => !string.IsNullOrWhiteSpace(node.Value)) ??
        element.Nodes().OfType<XText>().FirstOrDefault();
      if (textNode == null)
      {
        textNode = new XText(string.Empty);
        element.Add(textNode);
      }

      var textValue = textNode.Value;
      var result = this.EvaluateValue(textValue, configSettingsParser);
      if (result != null)
      {
        var leftWhitespace = textValue.Substring(0, textValue.Length - textValue.TrimStart().Length);
        var rightWhitespace = textValue.Substring(textValue.TrimEnd().Length);
        textNode.Value = leftWhitespace + result + rightWhitespace;
      }
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="setterExpression">Выражение для установки.</param>
    public TextSetter(string setterExpression) : base(setterExpression) { }
  }
}
