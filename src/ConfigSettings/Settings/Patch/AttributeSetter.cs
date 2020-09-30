using System;
using System.Xml.Linq;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Класс для установки значений атрибутов xml-элементов конфига.
  /// </summary>
  internal class AttributeSetter : BaseSetter
  {
    #region Поля и свойства

    /// <summary>
    /// Имя атрибута.
    /// </summary>
    private string AttributeName { get; }

    /// <summary>
    /// Перечень имен xml-элементов, ограничивающих область примения установки.
    /// </summary>
    /// <remarks>Например, ["system", "endpoint"] указывает, что можно устанавливать значение атрибута только в элементах с именем "endpoint" с родителем "system".</remarks>
    private string[] ElementNames { get; }

    #endregion

    #region Методы

    /// <summary>
    /// Определить, подходит ли xml-элемент для установки значения его атрибута.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="isAncestorSetter">Признак, что правило установки является унаследованным (с предыдущих уровней xml-документа).</param>
    /// <returns>True - если подходит.</returns>
    private bool IsElelementSuitable(XElement element, bool isAncestorSetter)
    {
      if (this.ElementNames.Length == 0 && isAncestorSetter)
        return false;

      var i = this.ElementNames.Length - 1;
      var currentElement = element;
      while (i >= 0 && currentElement != null)
      {
        if (currentElement.Name.LocalName != this.ElementNames[i])
          return false;
        currentElement = currentElement.Parent;
        i--;
      }
      return i < 0;
    }

    #endregion

    #region Базовый класс

    /// <inheritdoc />
    public override void Apply(XElement element, ConfigSettingsParser configSettingsParser, bool isAncestorSetter)
    {
      if (!this.IsElelementSuitable(element, isAncestorSetter))
        return;
      var isExistingAttribute = element.Attribute(this.AttributeName) != null;
      var attributeValue = isExistingAttribute ? element.Attribute(this.AttributeName).Value : string.Empty;
      var result = this.EvaluateValue(attributeValue, configSettingsParser);
      if (result != null)
        element.SetAttributeValue(this.AttributeName, result != "_REMOVE_" ? result : null);
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="attributeName">Имя атрибута.</param>
    /// <param name="setterExpression">Выражение для установки значения.</param>
    public AttributeSetter(string attributeName, string setterExpression)
      : base(setterExpression)
    {
      var parts = attributeName.Split('.');
      this.AttributeName = parts[parts.Length - 1];
      this.ElementNames = new string[parts.Length - 1];
      Array.Copy(parts, this.ElementNames, this.ElementNames.Length);
    }

    #endregion
  }
}
