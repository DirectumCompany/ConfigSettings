﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ConfigSettings.Settings.Patch;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Парсер файла настроек.
  /// </summary>
  public class ConfigSettingsParser
  {
    #region Поля и свойства

    /// <summary>
    /// Переменные (имя, значение).
    /// </summary>
    private readonly IList<Variable> variables = new List<Variable>();

    /// <summary>
    /// Метапеременные.
    /// </summary>
    private readonly IList<Variable> metaVariables = new List<Variable>();

    /// <summary>
    /// Блоки.
    /// </summary>
    private readonly IList<BlockSetting> blocks = new List<BlockSetting>();

    /// <summary>
    /// Импорты.
    /// </summary>
    private readonly IDictionary<string, Variable> rootImports = new Dictionary<string, Variable>();

    private bool isParsed;

    /// <summary>
    /// Корневой файл настроек.
    /// </summary>
    public string RootSettingsFilePath { get; protected set; }

    /// <summary>
    /// Признак, что есть настройка доступности/недоступности блоков.
    /// </summary>
    public bool HasEnabledOrDisabledBlocks { get { return this.blocks.Any(b => b.IsEnabled != null); } }

    /// <summary>
    /// Признак, что есть настройка содержимого блоков.
    /// </summary>
    public bool HasContentBlocks { get { return this.blocks.Any(b => !string.IsNullOrEmpty(b.Content)); } }

    private readonly List<CommentValue> comments = new List<CommentValue>();

    #endregion

    #region Методы

    /// <summary>
    /// Получить список всех импортируемых конфигов, с учётом рекурсии.
    /// </summary>
    /// <returns>Список всех импортируемых конфигов. Все пути в полученном списке - абсолютные пути импортируемых файлов настроек.</returns>
    public IReadOnlyList<string> GetAllImports()
    {
      var result = new List<string>();
      foreach (var pair in this.rootImports)
      {
        var filePath = pair.Key;
        if (filePath != this.RootSettingsFilePath)
        {
          var sourceConfigFile = pair.Value.FilePath;
          result.Add(GetAbsoluteImportPath(filePath, sourceConfigFile));
        }
      }
      return result;
    }

    /// <summary>
    /// Получить абсолютный путь до импортируемого файла настроек.
    /// </summary>
    /// <param name="importFilePath">Импортируемый файл, указанный в атрибуте from.</param>
    /// <param name="sourceConfigFilePath">Исходный конфигурационный файл, который содержит импорт.</param>
    /// <returns>Абсолютный путь до импортируемого файла настроек.</returns>
    private static string GetAbsoluteImportPath(string importFilePath, string sourceConfigFilePath)
    {
      return Path.IsPathRooted(importFilePath) ? importFilePath : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceConfigFilePath), importFilePath));
    }

    private bool? ComputeBlockAccessibility(string blockName)
    {
      var parser = new ExpressionEvaluator(blockName);
      var value = parser.EvaluateValue(string.Empty, this);
      if (value == "true")
        return true;
      if (value == "false")
        return false;

      return null;
    }

    private bool IsBlockAccessible(string blockName, bool accessibility)
    {
      return this.TryGetBlock(blockName)?.IsEnabled == accessibility;
    }

    /// <summary>
    /// Определить доступность блока.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>True - если есть настройка, что блок доступен.</returns>
    public bool IsBlockEnabled(string blockName)
    {
      return this.ComputeBlockAccessibility(blockName) ?? this.IsBlockAccessible(blockName, true);
    }

    /// <summary>
    /// Определить недоступность блока.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>True - если есть настройка, что блок недоступен.</returns>
    public bool IsBlockDisabled(string blockName)
    {
      return !this.ComputeBlockAccessibility(blockName) ?? this.IsBlockAccessible(blockName, false);
    }

    /// <summary>
    /// Получить содержимое блока в виде строки.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>Содержимым блока.</returns>
    public string GetBlockContent(string blockName)
    {
      return this.TryGetBlock(blockName)?.Content;
    }

    private string GetBlockContentWithoutRoot(string blockName)
    {
      return this.TryGetBlock(blockName)?.ContentWithoutRoot;
    }

    /// <summary>
    /// Получить содержимое блока (без корневого элемента).
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    /// <returns>Типизированный блок.</returns>
    public T GetBlockContent<T>(string blockName) where T : class
    {
      var content = this.GetBlockContentWithoutRoot(blockName);
      if (string.IsNullOrEmpty(content))
        return null;

      return BlockParser.Deserialize<T>(content);
    }

    /// <summary>
    /// Получить содержимое блока в виде xml.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>Содержимым блока.</returns>
    public XElement GetXmlBlockContent(string blockName)
    {
      var content = this.GetBlockContent(blockName);
      if (string.IsNullOrEmpty(content))
        return null;

      var namespaceManager = new XmlNamespaceManager(new NameTable());
      var parserContext = new XmlParserContext(null, namespaceManager, null, XmlSpace.Preserve);
      using (var xmlReader = new XmlTextReader(content, XmlNodeType.Element, parserContext))
        return XElement.Load(xmlReader);
    }

    /// <summary>
    /// Проверить, что для переменной в настройках указано значение.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    public bool HasVariable(string variableName)
    {
      return this.variables.Select(variable => variable.Name == variableName).FirstOrDefault();
    }  
    
    /// <summary>
    /// Получить переменную.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Переменная или null.</returns>
    public Variable TryGetVariable(string variableName)
    {
      return this.variables.LastOrDefault(variable => variable.Name == variableName);
    }

    /// <summary>
    /// Получить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetVariableValue(string variableName)
    {
      string result = null;
      foreach (var variable in this.variables)
      {
        if (variable.Name == variableName)
          result = variable.Value;
      }

      return result;
    }

    /// <summary>
    /// Установить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="settingsFilePath">Источник настройки.</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateVariable(string settingsFilePath, string variableName, string variableValue = null, IReadOnlyList<string> comments = null)
    {
      var newValue = this.TryGetVariable(variableName);
      if (newValue == null)
      {
        newValue = new Variable(settingsFilePath, variableName, variableValue);
        this.variables.Add(newValue);
        return;
      }
      
      newValue.Update(variableValue, comments);
    }

    /// <summary>
    /// Удалить переменную, если она есть.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    public void RemoveVariable(string variableName)
    {
      var variable = this.TryGetVariable(variableName);
      if (variable != null)
        this.variables.Remove(variable);
    }

    /// <summary>
    /// Установить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="settingsFilePath">Источник метапеременной.</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateMetaVariable(string settingsFilePath, string variableName, string variableValue = null, IReadOnlyList<string> comments = null)
    {
      var newValue = this.TryGetVariable(variableName);
      if (newValue == null)
      {
        newValue = new Variable(settingsFilePath, variableName, variableValue);
        this.metaVariables.Add(newValue);
        return;
      }
      
      newValue.Update(variableValue, comments);

    }

    /// <summary>
    /// Получить блок..
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>Блок или null.</returns>
    private BlockSetting TryGetBlock(string blockName)
    {
      return this.blocks.LastOrDefault(b => b.Name == blockName);
    }

    /// <summary>
    /// Установить значение блока.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="blockContent">Содержимое блока в виде строки.</param>
    public void AddOrUpdateBlock(string settingsFilePath, string blockName, bool? isBlockEnabled, string blockContent, IReadOnlyList<string> comments = null)
    {
      var blockContentWithRoot = $@"<block name=""{blockName}"">{blockContent}</block>";
      if (isBlockEnabled != null)
        blockContentWithRoot = $@"<block name=""{blockName}"" enabled=""{isBlockEnabled}"" >{blockContent}</block>";

      var block = this.TryGetBlock(blockName);
      if (block == null)
      {
        block = new BlockSetting(settingsFilePath, blockName, isBlockEnabled, blockContentWithRoot, blockContent, comments);
        this.blocks.Add(block);
        return;
      }
      
      block.Update(isBlockEnabled, blockContentWithRoot, blockContent, comments);
    }

    /// <summary>
    /// Установить значение блока.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="block">Типизированный блок.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    public void AddOrUpdateBlock<T>(string settingsFilePath, string blockName, bool? isBlockEnabled, T block, IReadOnlyList<string> comments = null) where T : class
    {
      var blockContent = BlockParser.Serialize(block);
      this.AddOrUpdateBlock(settingsFilePath, blockName, isBlockEnabled, blockContent);
    }

    /// <summary>
    /// Установить признак доступности.
    /// </summary>
    /// <param name="variableName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="settingsFilePath">Источник настройки.</param>
    public void SetBlockAccessibility(string settingsFilePath, string blockName, bool isBlockEnabled)
    {
      var blockContentWithRoot = $@"<block name=""{blockName}"" enabled=""{isBlockEnabled}"" ></block>";
      
      var block = this.TryGetBlock(blockName);
      if (block == null)
      {
        block = new BlockSetting(settingsFilePath, blockName, isBlockEnabled, blockContentWithRoot);
        this.blocks.Add(block);
        return;
      }
      
      block.Update(isBlockEnabled, block.Content, block.ContentWithoutRoot, block.Comments);
    }


    /// <summary>
    /// Усатновить import from.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void SetImportFrom(string filePath)
    {
      var newValue = this.HasImportFrom(filePath)
        ? new Variable(Path.GetFileName(filePath), this.GetImportFrom(filePath).FilePath)
        : new Variable(Path.GetFileName(filePath), this.RootSettingsFilePath);
      this.rootImports[filePath] = newValue;
    }

    /// <summary>
    /// Проверить, что для метапеременной в настройках указано значение.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    public bool HasMetaVariable(string variableName)
    {
      return this.metaVariables.Select(variable => variable.Name == variableName).FirstOrDefault();
    }

    /// <summary>
    /// Проверить наличие блока.
    /// </summary>
    /// <param name="variableName">Имя блока.</param>
    /// <returns>True, если блок существует.</returns>
    public bool HasBlock(string variableName)
    {
      return this.blocks.Select(variable => variable.Name == variableName).FirstOrDefault();
    }

    /// <summary>
    /// Получить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetMetaVariableValue(string variableName)
    {
      string result = null;
      foreach (var variable in this.metaVariables)
      {
        if (variable.Name == variableName)
          result = variable.Value;
      }

      return result;
    }

    /// <summary>
    /// Проверить наличие переменной import from.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    /// <returns>True, если есть импорт с таким именем файла.</returns>
    public bool HasImportFrom(string fileName)
    {
      return this.GetImportsFrom(fileName).Any();
    }

    /// <summary>
    /// Удалить импорт файла.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    public void RemoveImportFrom(string fileName)
    {
      if (this.HasImportFrom(fileName))
      {
        var imports = this.GetImportsFrom(fileName).ToList();
        foreach (var import in imports)
        {
          this.rootImports.Remove(import);
        }
      }
    }

    /// <summary>
    /// Получить значение импорта.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    /// <returns>Переменная с импортом.</returns>
    public Variable GetImportFrom(string fileName)
    {
      return this.GetImportsFrom(fileName).First().Value;
    }

    /// <summary>
    /// Получить все импорты файла.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    /// <returns>Импорты файла.</returns>
    private IEnumerable<KeyValuePair<string, Variable>> GetImportsFrom(string fileName)
    {
      return this.rootImports.Where(v => v.Value.Value.Equals(Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Распарсить корневой xml-источник настроек.
    /// </summary>
    protected void ParseRootSettingsSource()
    {
      if (this.isParsed)
        return;
      this.isParsed = true;

      if (string.IsNullOrEmpty(this.RootSettingsFilePath))
        return;

      // Добавляем корневой элемент.
      this.rootImports[this.RootSettingsFilePath] = new Variable(Path.GetFileName(this.RootSettingsFilePath), this.RootSettingsFilePath);

      this.ParseSettingsSource(this.RootSettingsFilePath);
    }

    /// <summary>
    /// Распарсить источник настроек.
    /// </summary>
    /// <param name="settingsFilePath">путь к файлу.</param>
    /// <exception cref="ParseConfigException"></exception>
    protected virtual void ParseSettingsSource(string settingsFilePath)
    {
      if (!File.Exists(settingsFilePath))
        return;

      try
      {
        var settings = XDocument.Load(settingsFilePath);
        if (settings?.Root == null)
          return;

        // Сначала обрабатываем import-ы, чтобы не зависеть от их местоположения в xml-ке.
        foreach (var element in settings.Root.Elements())
        {
          var elementName = element.Name.LocalName;
          if (elementName == "import")
            this.ParseImport(settingsFilePath, element);
        }

        // Затем обрабатываем остальные элементы.
        foreach (var element in settings.Root.Elements())
        {
          var elementName = element.Name.LocalName;

          if (elementName == "var")
            this.ParseVar(settingsFilePath, element);
          else if (elementName == "meta")
            this.ParseMeta(settingsFilePath, element);
          else if (elementName == "block")
            this.ParseBlock(settingsFilePath, element);
        }
        foreach (var node in settings.Root.Nodes())
        {
          if (node is XComment comment)
            this.ParseComment(settingsFilePath, comment);
        }

      }
      catch (Exception ex) when (!(ex is ParseConfigException))
      {
        throw new ParseConfigException(settingsFilePath, $"An error occured when parsing config file: '{settingsFilePath}'.", ex);
      }
    }

    /// <summary>
    /// Получить комментарии.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="element">Элемент комменария.</param>
    private void ParseComment(string settingsFilePath, XComment element)
    {
      if (element.NextNode is XElement next)
      {
        var nextName = next.Name.LocalName;
        if (nextName == "import" || nextName == "meta" || nextName == "var" || nextName == "block")
          return;
      }
      this.comments.Add(new CommentValue(element.Value, settingsFilePath));
    }

    /// <summary>
    /// Получить комментарии для ноды.
    /// </summary>
    /// <param name="element">Элемент, для которого нужно получить комменарий.</param>
    /// <returns>Строка с комменарием.</returns>
    private List<string> GetComments(XNode element)
    {
      if (element.PreviousNode is XComment comment)
      {
        var previousComments = GetComments(element.PreviousNode);
        previousComments.Add(comment.Value);
        return previousComments;
      }
      return new List<string>();
    }

    private void ParseImport(string settingsFilePath, XElement element)
    {
      var fromAttribute = element.Attribute("from");
      if (string.IsNullOrEmpty(fromAttribute?.Value))
        return;

      var filePath = fromAttribute.Value;
      var absolutePath = GetAbsoluteImportPath(filePath, settingsFilePath);

      this.rootImports[filePath] = new Variable(Path.GetFileName(absolutePath), settingsFilePath);
      this.rootImports[filePath].Comments = this.GetComments(element);
      this.ParseSettingsSource(absolutePath);
    }

    private void ParseBlock(string settingsFilePath, XElement element)
    {
      var name = element.Attribute("name")?.Value;
      if (string.IsNullOrEmpty(name))
        return;

      var enabledAttribute = element.Attribute("enabled");
      bool? isBlockEnabled = null;
      if (enabledAttribute != null)
      {
        if (enabledAttribute.Value.ToUpperInvariant() == "TRUE")
          isBlockEnabled = true;
        else if (enabledAttribute.Value.ToUpperInvariant() == "FALSE")
          isBlockEnabled = false;
      }

      var blockContentWithoutRoot = string.Concat(element.Nodes());
      var blockContent = !string.IsNullOrEmpty(blockContentWithoutRoot) ? element.ToString() : null;

      var block = this.TryGetBlock(name);
      if (block == null)
      {
        this.blocks.Add(new BlockSetting(settingsFilePath, name, isBlockEnabled, blockContent, blockContentWithoutRoot, this.GetComments(element)));
        return;
      }
      
      block.Update(isBlockEnabled, blockContent, blockContentWithoutRoot, this.GetComments(element));
    }

    private void ParseMeta(string settingsFilePath, XElement element)
    {
      var name = element.Attribute("name")?.Value;
      var value = element.Attribute("value")?.Value;
      if (string.IsNullOrEmpty(name))
        return;

      this.AddOrUpdateMetaVariable(settingsFilePath, name, value, this.GetComments(element));
    }

    private void ParseVar(string settingsFilePath, XElement element)
    {
      var name = element.Attribute("name")?.Value;
      var value = element.Attribute("value")?.Value;
      if (string.IsNullOrEmpty(name))
        return;

      this.AddOrUpdateVariable(settingsFilePath, name, value, this.GetComments(element));
    }

    /// <summary>
    /// Сохранить комментарии.
    /// </summary>
    /// <param name="comments">Комментарии.</param>
    /// <param name="rootElement">Корневой элемент.</param>
    private void SaveComments(List<string> comments, XElement rootElement) 
    {
      if (comments != null)
      {
        foreach (var comment in comments)
          if (!string.IsNullOrEmpty(comment))
            rootElement.Add(new XComment(comment));
      }
    }

    /// <summary>
    /// Сохранить.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Save()
    {
      if (!this.rootImports.Keys.Any())
        throw new InvalidOperationException("Cannot save. rootImports is empty.");

      foreach (var filePath in this.rootImports.Keys)
      {
        var rootElement = new XElement("settings");

        var rootImportsWithEqualPath = this.rootImports.Where(v => v.Value.FilePath.Equals(filePath) &&
                                                                   !v.Key.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        foreach (var kvp in rootImportsWithEqualPath)
        {
          this.SaveComments(kvp.Value.Comments, rootElement);
          rootElement.Add(new XElement("import", new XAttribute("from", kvp.Key)));
        }

        var metaVariablesWithEqualPath = this.metaVariables.Where(v => v.Value.FilePath.Equals(filePath));
        foreach (var kvp in metaVariablesWithEqualPath)
        {
          this.SaveComments(kvp.Value.Comments, rootElement);
          rootElement.Add(new XElement("meta", new XAttribute("name", kvp.Key), new XAttribute("value", kvp.Value.Value)));
        }
        var variablesWithEqualPath = this.variables.Where(v => v.Value.FilePath.Equals(filePath) && !this.HasBlock(v.Key));
        foreach (var kvp in variablesWithEqualPath)
        {
          this.SaveComments(kvp.Value.Comments, rootElement);
          rootElement.Add(
            new XElement("var", new XAttribute("name", kvp.Key), new XAttribute("value", kvp.Value.Value)));
        }

        var blocksWithEqualPath = this.blocks.Where(v => v.Value.FilePath.Equals(filePath));
        foreach (var kvp in blocksWithEqualPath)
        {
          var blockContentWithRoot = string.IsNullOrEmpty(kvp.Value.Content)
            ? kvp.Value.IsEnabled != null ? $@"<block name=""{kvp.Key}"" enabled=""{kvp.Value.IsEnabled}""></block>"
              : $@"<block name=""{kvp.Key}""></block>"
            : kvp.Value.Content;
          var blockContent = XDocument.Parse(blockContentWithRoot);

          this.SaveComments(kvp.Value.Comments, rootElement);
          rootElement.Add(blockContent.Root);
        }

        var commentsWithEqualPath = this.comments.Where(v => v.FilePath.Equals(filePath));
        foreach (var comment in commentsWithEqualPath)
        {
          if (!string.IsNullOrEmpty(comment.Value))
            rootElement.Add(new XComment(comment.Value));
        }

        if (!rootElement.HasElements)
          continue;

        var config = new XDocument();
        config.Add(rootElement);
        config.Save(filePath);
      }
    }

    /// <summary>
    /// Удалить все закешированные настройки.
    /// </summary>
    protected void ClearAllCaches()
    {
      this.variables.Clear();
      this.metaVariables.Clear();
      this.blocks.Clear();
      this.rootImports.Clear();
      this.isParsed = false;
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    public ConfigSettingsParser(string settingsFilePath)
    {
      this.RootSettingsFilePath = settingsFilePath;
      this.ParseRootSettingsSource();
    }

    /// <summary>
    /// Конструктор для наследников.
    /// </summary>
    protected ConfigSettingsParser()
    {
    }

    #endregion
  }
}
