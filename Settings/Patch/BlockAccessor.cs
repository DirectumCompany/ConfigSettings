using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CommonLibrary.Settings.Patch
{
  /// <summary>
  /// Класс для управления доступностью блоков xml-конфига.
  /// </summary>
  public class BlockAccessor
  {
    #region Поля и свойства

    /// <summary>
    /// Xml-конфиг.
    /// </summary>
    private readonly XDocument config;

    #endregion

    #region Методы

    /// <summary>
    /// Применить настройки доступности блоков.
    /// </summary>
    /// <param name="configSettings">Настройки конфига.</param>
    public void ApplyAccess(ConfigSettings configSettings)
    {
      this.Apply(this.EnableBlocks, configSettings);
      this.Apply(this.DisableBlocks, configSettings);

      if (configSettings.HasContentBlocks)
        this.Apply(this.SetBlockContent, configSettings);
    }

    /// <summary>
    /// Применить настройки доступности блоков.
    /// </summary>
    /// <param name="action">Действие для управления доступностью.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    private void Apply(Action<XElement, ConfigSettings, List<XNode>> action, ConfigSettings configSettings)
    {
      var nodesToRemove = new List<XNode>();
      action(this.config.Root, configSettings, nodesToRemove);
      foreach (var node in nodesToRemove)
        node.Remove();
    }

    /// <summary>
    /// Сделать блоки xml-элемента недоступными (закомментировать элементы).
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    /// <param name="nodesToRemove">Узлы для последующего удаления.</param>
    private void DisableBlocks(XElement element, ConfigSettings configSettings, List<XNode> nodesToRemove)
    {
      var needDisableBlock = false;
      foreach (var node in element.Nodes())
      {
        var commentNode = node as XComment;
        var elementNode = node as XElement;
        if (commentNode != null)
        {
          var comment = commentNode.Value.Trim();
          if (comment.Length > 3 && comment.StartsWith("{~", StringComparison.Ordinal) && comment.EndsWith("}", StringComparison.Ordinal))
          {
            var blockName = comment.Substring(2, comment.Length - 3);
            var negateDisabled = this.IsNegatedBlockName(ref blockName);
            var blockDisabled = configSettings.IsBlockDisabled(blockName);
            if (negateDisabled)
              blockDisabled = !blockDisabled;

            if (blockDisabled)
              needDisableBlock = true;
          }
          else
            needDisableBlock = false;
        }
        else if (elementNode != null)
        {
          if (needDisableBlock)
          {
            node.AddAfterSelf(new XComment(node.ToString()));
            nodesToRemove.Add(node);
            needDisableBlock = false;
          }
          this.DisableBlocks(elementNode, configSettings, nodesToRemove);
        }
      }
    }

    /// <summary>
    /// Сделать блоки xml-элемента доступными (раскомментировать элементы).
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    /// <param name="nodesToRemove">Узлы для последующего удаления.</param>
    private void EnableBlocks(XElement element, ConfigSettings configSettings, List<XNode> nodesToRemove)
    {
      var needEnableBlock = false;
      foreach (var node in element.Nodes())
      {
        var commentNode = node as XComment;
        var elementNode = node as XElement;
        if (commentNode != null)
        {
          var comment = commentNode.Value.Trim();
          if (comment.Length > 3 && comment.StartsWith("{~", StringComparison.Ordinal) && comment.EndsWith("}", StringComparison.Ordinal))
          {
            var blockName = comment.Substring(2, comment.Length - 3);
            var negateEnabled = this.IsNegatedBlockName(ref blockName);
            var blockEnabled = configSettings.IsBlockEnabled(blockName);
            if (negateEnabled)
              blockEnabled = !blockEnabled;

            if (blockEnabled)
              needEnableBlock = true;
          }
          else
          {
            if (needEnableBlock)
            {
              var newBlock = this.ParseElement(comment, element);
              node.AddAfterSelf(newBlock);
              nodesToRemove.Add(node);
              needEnableBlock = false;
            }
          }
        }
        else if (elementNode != null)
        {
          needEnableBlock = false;
          this.EnableBlocks(elementNode, configSettings, nodesToRemove);
        }
      }
    }

    /// <summary>
    /// Установить содержимое блока.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    /// <param name="nodesToRemove">Узлы для последующего удаления.</param>
    private void SetBlockContent(XElement element, ConfigSettings configSettings, List<XNode> nodesToRemove)
    {
      string blockContent = null;
      foreach (var node in element.Nodes())
      {
        var commentNode = node as XComment;
        var elementNode = node as XElement;
        if (commentNode != null)
        {
          var comment = commentNode.Value.Trim();
          if (comment.Length > 3 && comment.StartsWith("{~", StringComparison.Ordinal) && comment.EndsWith("}", StringComparison.Ordinal))
          {
            var blockName = comment.Substring(2, comment.Length - 3);
            blockContent = configSettings.GetBlockContent(blockName);
          }
          else
            blockContent = null;
        }
        else if (elementNode != null)
        {
          if (!string.IsNullOrEmpty(blockContent))
          {
            nodesToRemove.AddRange(elementNode.Nodes());
            foreach (var subNode in this.ParseElement(blockContent, elementNode).Nodes())
              elementNode.Add(subNode);
            blockContent = null;
          }
          this.SetBlockContent(elementNode, configSettings, nodesToRemove);
        }
      }
    }

    /// <summary>
    /// Распарсить строку с xml-выражением с учетом внешних пространств имен.
    /// </summary>
    /// <param name="elementExpression">Строка с xml-выражением.</param>
    /// <param name="contextElement">Контекстный xml-элемент.</param>
    /// <returns>Созданный по строке xml-элемент.</returns>
    /// <remarks>См. http://stackoverflow.com/questions/1219419/xdocument-or-xelement-parsing-of-xml-element-containing-namespaces .</remarks>
    private XElement ParseElement(string elementExpression, XElement contextElement)
    {
      var namespaceManager = new XmlNamespaceManager(new NameTable());
      foreach (var item in this.GetNamespaces(contextElement))
        namespaceManager.AddNamespace(item.Key, item.Value);
      var parserContext = new XmlParserContext(null, namespaceManager, null, XmlSpace.Preserve);
      using (var xmlReader = new XmlTextReader(elementExpression, XmlNodeType.Element, parserContext))
        return XElement.Load(xmlReader);
    }

    /// <summary>
    /// Получить все пространства имен, доступные для xml-элемента.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <returns>Доступные ространства имен (набор пар "префикс - пространство имен").</returns>
    private Dictionary<string, string> GetNamespaces(XElement element)
    {
      var namespaces = new Dictionary<string, string>();
      var currentElement = element;
      while (currentElement != null)
      {
        foreach (var attribute in currentElement.Attributes().Where(a => a.IsNamespaceDeclaration))
        {
          var namespacePrefix = attribute.Name.LocalName;
          namespacePrefix = namespacePrefix == "xmlns" ? string.Empty : namespacePrefix;
          if (!namespaces.ContainsKey(namespacePrefix))
            namespaces.Add(namespacePrefix, attribute.Value);
        }
        currentElement = currentElement.Parent;
      }
      return namespaces;
    }

    private bool IsNegatedBlockName(ref string blockName)
    {
      var negated = blockName.StartsWith("!", StringComparison.OrdinalIgnoreCase);
      if (negated)
        blockName = blockName.Substring(1);
      return negated;
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    public BlockAccessor(XDocument config)
    {
      this.config = config;
    }

    #endregion
  }
}
