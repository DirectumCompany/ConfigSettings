using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ConfigSettings.Settings.Patch;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Парсер файла настроек. Считывает настройки с указанного файла и со всех импортируемых файлов.
  /// Помнит файл-источник для каждой считанной настройки.
  /// Читает все импортируемые файлы и кеширует все считанные настройки при создании инстанса класса.
  /// Позволяет записывать настройки в любой файл, не проверяет вхождение этого файла в считанный набор файлов.
  /// !!! ПРИ СОХРАНЕНИИ ПЕРЕЗАПИСЫВАЕТ ВСЕ СЧИТАННЫЕ РАНЕЕ ФАЙЛЫ.
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
    private readonly IList<ImportFrom> importsFrom = new List<ImportFrom>();

    private bool isParsed;

    /// <summary>
    /// Корневой файл настроек.
    /// </summary>
    public string RootSettingsFilePath { get; protected set; }

    /// <summary>
    /// Признак, что есть настройка содержимого блоков.
    /// </summary>
    public bool HasContentBlocks { get { return this.blocks.Any(b => !string.IsNullOrEmpty(b.Content)); } }

    private readonly List<CommentValue> commentsValues = new List<CommentValue>();

    #endregion

    #region Методы

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
      return this.GetBlock(null, blockName)?.IsEnabled == accessibility;
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
    /// Xml часть с доступностью блока.
    /// </summary>
    /// <param name="enabled">Доступность блока.</param>
    /// <returns>Строка в виде части xml.</returns>
    private static string BlockEnabledXmlPart(bool? enabled)
    {
      return enabled == null ? string.Empty : $@" enabled=""{enabled.ToString().ToLower()}""";
    }

    /// <summary>
    /// Получить переменную.
    /// </summary>
    /// <param name="settingsPath">Путь к файлу, в котором надо искать переменную.</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Переменная или null.</returns>
    internal Variable GetVariable(string settingsPath, string variableName)
    {
      return this.GetAllVariables(variableName).LastOrDefault(variable => settingsPath == null || variable.FilePath == settingsPath);
    }

    /// <summary>
    /// Получить блок.
    /// </summary>
    /// <param name="settingsPath">Путь до файла с настройками.</param>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>Блок или null.</returns>
    internal BlockSetting GetBlock(string settingsPath, string blockName)
    {
      return this.blocks.LastOrDefault(b => b.Name == blockName
                                            && (settingsPath == null || b.FilePath == settingsPath));
    }

    /// <summary>
    /// Получить переменные с заданным именем со всех импортируемых конфигов.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Список переменных.</returns>
    public IReadOnlyList<Variable> GetAllVariables(string variableName)
    {
      return this.variables.Where(variable => variable.Name == variableName).ToList();
    }

    /// <summary>
    /// Получить список всех импортируемых конфигов, с учётом рекурсии.
    /// </summary>
    /// <returns>Список всех импортируемых конфигов. Все пути в полученном списке - абсолютные пути импортируемых файлов настроек.</returns>
    public IReadOnlyList<string> GetAllImports()
    {
      return this.importsFrom.Where(r => !r.IsRoot).Select(r => r.GetAbsolutePath()).ToList();
    }

    /// <summary>
    /// Получить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Значение переменной.</returns>
    public string GetVariableValue(string variableName)
    {
      return this.GetVariable(null, variableName)?.Value;
    }

    /// <summary>
    /// Получить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetMetaVariableValue(string variableName)
    {
      return this.metaVariables.LastOrDefault(variable => variable.Name == variableName)?.Value;
    }

    /// <summary>
    /// Получить содержимое блока в виде строки.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>Содержимым блока.</returns>
    public string GetBlockContent(string blockName)
    {
      return this.GetBlock(null, blockName)?.Content;
    }

    private string GetBlockContentWithoutRoot(string blockName)
    {
      return this.GetBlock(null, blockName)?.ContentWithoutRoot;
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
    /// Получить значение импорта.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Переменная с импортом.</returns>
    public ImportFrom GetImportFrom(string filePath)
    {
      return this.GetImportsFromExceptRoot(filePath).LastOrDefault();
    }

    /// <summary>
    /// Получить все импорты файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Импорты файла.</returns>
    private IEnumerable<ImportFrom> GetImportsFromExceptRoot(string filePath)
    {
      return this.importsFrom.Where(v => !v.IsRoot && v.GetAbsolutePath().EndsWith(filePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Установить значение переменной в указанном файле.
    /// </summary>
    /// <param name="settingsFilePath">Источник настройки.</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    internal void AddOrUpdateVariable(string settingsFilePath, string variableName, string variableValue, IReadOnlyList<string> comments = null)
    {
      var newValue = this.GetVariable(settingsFilePath, variableName);
      if (newValue == null)
      {
        newValue = new Variable(settingsFilePath, variableName, variableValue, comments);
        this.variables.Add(newValue);
        return;
      }

      newValue.Update(variableValue, comments);
    }

    /// <summary>
    /// Установить значение переменной в корневом файле.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateVariable(string variableName, string variableValue = null, IReadOnlyList<string> comments = null)
    {
      this.AddOrUpdateVariable(this.RootSettingsFilePath, variableName, variableValue, comments);
    }

    /// <summary>
    /// Установить значение метапеременной в указанном файле.
    /// </summary>
    /// <param name="settingsFilePath">Источник метапеременной.</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    internal void AddOrUpdateMetaVariable(string settingsFilePath, string variableName, string variableValue = null, IReadOnlyList<string> comments = null)
    {
      var newValue = this.GetVariable(settingsFilePath, variableName);
      if (newValue == null)
      {
        newValue = new Variable(settingsFilePath, variableName, variableValue, comments);
        this.metaVariables.Add(newValue);
        return;
      }

      newValue.Update(variableValue, comments);
    }

    /// <summary>
    /// Установить значение метапеременной в корневом файле.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateMetaVar(string variableName, string variableValue = null, IReadOnlyList<string> comments = null)
    {
      this.AddOrUpdateMetaVariable(this.RootSettingsFilePath, variableName, variableValue, comments);
    }

    /// <summary>
    /// Установить значение блока в указанном файле.
    /// </summary>
    /// <param name="settingsFilePath">Источник настройки.</param>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="blockContentWithoutRoot">Содержимое блока в виде строки.</param>
    /// <param name="comments">Комментарии.</param>
    internal void AddOrUpdateBlock(string settingsFilePath, string blockName, bool? isBlockEnabled, string blockContentWithoutRoot, IReadOnlyList<string> comments = null)
    {
      var blockContentWithRoot = !string.IsNullOrEmpty(blockContentWithoutRoot)
        ? $@"<block name=""{blockName}""{BlockEnabledXmlPart(isBlockEnabled)}>{blockContentWithoutRoot}</block>"
        : null;

      var block = this.GetBlock(settingsFilePath, blockName);
      if (block == null)
      {
        block = new BlockSetting(settingsFilePath, blockName, isBlockEnabled, blockContentWithRoot, blockContentWithoutRoot, comments);
        this.blocks.Add(block);
        return;
      }

      block.Update(isBlockEnabled, blockContentWithRoot, blockContentWithoutRoot, comments);
    }

    /// <summary>
    /// Установить значение блока в указанном файле.
    /// </summary>
    /// <param name="settingsFilePath">Источник настройки.</param>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="block">Типизированный блок.</param>
    /// <param name="comments">Комментарии.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    internal void AddOrUpdateBlock<T>(string settingsFilePath, string blockName, bool? isBlockEnabled, T block, IReadOnlyList<string> comments = null) where T : class
    {
      var blockContent = BlockParser.Serialize(block);
      this.AddOrUpdateBlock(settingsFilePath, blockName, isBlockEnabled, blockContent);
    }

    /// <summary>
    /// Установить значение блока в корневом файле.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="block">Типизированный блок.</param>
    /// <param name="comments">Комментарии.</param>
    /// <typeparam name="T">Тип блока.</typeparam>
    public void AddOrUpdateBlock<T>(string blockName, bool? isBlockEnabled, T block, IReadOnlyList<string> comments = null) where T : class
    {
      this.AddOrUpdateBlock(this.RootSettingsFilePath, blockName, isBlockEnabled, block, comments);
    }

    /// <summary>
    /// Установить значение блока в корневом файле.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <param name="isBlockEnabled">Доступность блока.</param>
    /// <param name="blockContentWithoutRoot">Содержимое блока в виде строки.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateBlock(string blockName, bool? isBlockEnabled, string blockContentWithoutRoot, IReadOnlyList<string> comments = null)
    {
      this.AddOrUpdateBlock(this.RootSettingsFilePath, blockName, isBlockEnabled, blockContentWithoutRoot, comments);
    }

    /// <summary>
    /// Установить import from в указанном файле.
    /// </summary>
    /// <param name="settingsFilePath">Источник настройки.</param>
    /// <param name="filePath">Путь к файлу.</param>
    /// <param name="comments">Комментарии.</param>
    internal void AddOrUpdateImportFrom(string settingsFilePath, string filePath, IReadOnlyList<string> comments = null)
    {
      var importFrom = this.GetImportFrom(filePath);
      if (importFrom == null)
      {
        importFrom = new ImportFrom(settingsFilePath, filePath, false, comments);
        ParseSettingsSource(importFrom.GetAbsolutePath());
        this.importsFrom.Add(importFrom);
        return;
      }

      // Мы не можем изменить filePath, т.к. это ключ, по которому проверяется уникальность.
      importFrom.Update(comments);
    }

    /// <summary>
    /// Установить import from в корневом файле.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <param name="comments">Комментарии.</param>
    public void AddOrUpdateImportFrom(string filePath, IReadOnlyList<string> comments = null)
    {
      this.AddOrUpdateImportFrom(this.RootSettingsFilePath, filePath, comments);
    }

    /// <summary>
    /// Удалить переменную, если она есть.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    public void RemoveAllVariables(string variableName)
    {
      foreach (var variable in this.GetAllVariables(variableName))
        this.variables.Remove(variable);
    }

    /// <summary>
    /// Удалить переменную, если она есть.
    /// </summary>
    /// <param name="settingsPath">Путь до конфига.</param>
    /// <param name="variableName">Имя переменной.</param>
    internal void RemoveVariable(string settingsPath, string variableName)
    {
      var variable = this.GetVariable(settingsPath, variableName);
      if (variable != null)
        this.variables.Remove(variable);
    }

    /// <summary>
    /// Удалить переменную, если она есть.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    public void RemoveVariable(string variableName)
    {
      this.RemoveVariable(this.RootSettingsFilePath, variableName);
    }

    /// <summary>
    /// Удалить импорт файла.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    public void RemoveImportFrom(string fileName)
    {
      var importFromToDelete = this.GetImportFrom(fileName);
      if (importFromToDelete != null)
        this.importsFrom.Remove(importFromToDelete);
    }

    /// <summary>
    /// Проверить, что для переменной в настройках указано значение.
    /// </summary>
    /// <param name="settingsPath">Путь до файла с настройками</param>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    internal bool HasVariable(string settingsPath, string variableName)
    {
      return this.GetVariable(settingsPath, variableName) != null;
    }

    /// <summary>
    /// Проверить, что для переменной в настройках указано значение.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    public bool HasVariable(string variableName)
    {
      return this.HasVariable(null, variableName);
    }

    /// <summary>
    /// Проверить, что для метапеременной в настройках указано значение.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    public bool HasMetaVariable(string variableName)
    {
      return this.metaVariables.FirstOrDefault(variable => variable.Name == variableName) != null;
    }

    /// <summary>
    /// Проверить наличие блока.
    /// </summary>
    /// <param name="settingsFilePath">Путь до файла с настройками</param>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>True, если блок существует.</returns>
    internal bool HasBlock(string settingsFilePath, string blockName)
    {
      return this.GetBlock(settingsFilePath, blockName) != null;
    }

    /// <summary>
    /// Проверить наличие блока.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>True, если блок существует.</returns>
    public bool HasBlock(string blockName)
    {
      return this.HasBlock(null, blockName);
    }

    /// <summary>
    /// Проверить наличие переменной import from.
    /// </summary>
    /// <param name="fileName">Путь к файлу.</param>
    /// <returns>True, если есть импорт с таким именем файла.</returns>
    public bool HasImportFrom(string fileName)
    {
      return this.GetImportsFromExceptRoot(fileName).Any();
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

      var rootImport = new ImportFrom(this.RootSettingsFilePath, this.RootSettingsFilePath, isRoot: true);
      this.ParseSettingsSource(rootImport.GetAbsolutePath());

      // Добавляем корневой элемент последним.
      this.importsFrom.Add(rootImport);
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
        if (settings.Root == null)
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
      this.commentsValues.Add(new CommentValue(element.Value, settingsFilePath));
    }

    /// <summary>
    /// Получить комментарии для ноды.
    /// </summary>
    /// <param name="element">Элемент, для которого нужно получить комменарий.</param>
    /// <returns>Строка с комменарием.</returns>
    private static IReadOnlyList<string> GetComments(XNode element)
    {
      if (element.PreviousNode is XComment comment)
      {
        var previousComments = GetComments(element.PreviousNode).ToList();
        previousComments.Add(comment.Value);
        return previousComments;
      }
      return new List<string>();
    }

    private void ParseImport(string settingsFilePath, XElement element)
    {
      var from = element.Attribute("from")?.Value;
      if (string.IsNullOrEmpty(from))
        return;

      this.AddOrUpdateImportFrom(settingsFilePath, from, GetComments(element));
    }

    private void ParseBlock(string settingsFilePath, XElement element)
    {
      var blockName = element.Attribute("name")?.Value;
      if (string.IsNullOrEmpty(blockName))
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
      this.AddOrUpdateBlock(settingsFilePath, blockName, isBlockEnabled, blockContentWithoutRoot, GetComments(element));
    }

    private void ParseMeta(string settingsFilePath, XElement element)
    {
      var name = element.Attribute("name")?.Value;
      var value = element.Attribute("value")?.Value;
      if (string.IsNullOrEmpty(name))
        return;

      this.AddOrUpdateMetaVariable(settingsFilePath, name, value, GetComments(element));
    }

    private void ParseVar(string settingsFilePath, XElement element)
    {
      var name = element.Attribute("name")?.Value;
      var value = element.Attribute("value")?.Value;
      if (string.IsNullOrEmpty(name))
        return;

      this.AddOrUpdateVariable(settingsFilePath, name, value, GetComments(element));
    }

    /// <summary>
    /// Сохранить комментарии.
    /// </summary>
    /// <param name="comments">Комментарии.</param>
    /// <param name="rootElement">Корневой элемент.</param>
    private static void SaveComments(IReadOnlyList<string> comments, XElement rootElement)
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
      if (!this.importsFrom.Any())
        throw new InvalidOperationException($"Cannot save. {nameof(importsFrom)} is empty.");

      // Цикл по всем импортам, включая корневой.
      foreach (var importFrom in this.importsFrom)
      {
        var filePath = importFrom.GetAbsolutePath();

        var rootElement = new XElement("settings");

        var rootImportsWithEqualPath = this.importsFrom.Where(v => v.FilePath.Equals(filePath) &&
                                                                   !v.From.Equals(importFrom.From, StringComparison.OrdinalIgnoreCase));
        foreach (var kvp in rootImportsWithEqualPath)
        {
          SaveComments(kvp.Comments, rootElement);
          rootElement.Add(new XElement("import", new XAttribute("from", kvp.From)));
        }

        var metaVariablesWithEqualPath = this.metaVariables.Where(v => v.FilePath.Equals(filePath));
        foreach (var kvp in metaVariablesWithEqualPath)
        {
          SaveComments(kvp.Comments, rootElement);
          rootElement.Add(new XElement("meta", new XAttribute("name", kvp.Name), new XAttribute("value", kvp.Value)));
        }
        var variablesWithEqualPath = this.variables.Where(v => v.FilePath.Equals(filePath));
        foreach (var kvp in variablesWithEqualPath)
        {
          SaveComments(kvp.Comments, rootElement);
          rootElement.Add(
            new XElement("var", new XAttribute("name", kvp.Name), new XAttribute("value", kvp.Value)));
        }

        var blocksWithEqualPath = this.blocks.Where(v => v.FilePath.Equals(filePath));
        foreach (var kvp in blocksWithEqualPath)
        {
          var blockContentWithRoot = string.IsNullOrEmpty(kvp.Content)
            ? $@"<block name=""{kvp.Name}""{BlockEnabledXmlPart(kvp.IsEnabled)}></block>"
            : kvp.Content;
          var blockContent = XDocument.Parse(blockContentWithRoot);

          SaveComments(kvp.Comments, rootElement);
          rootElement.Add(blockContent.Root);
        }

        var commentsWithEqualPath = this.commentsValues.Where(v => v.FilePath.Equals(filePath));
        foreach (var comment in commentsWithEqualPath)
        {
          if (!string.IsNullOrEmpty(comment.Value))
            rootElement.Add(new XComment(comment.Value));
        }

        if (!rootElement.HasElements && !File.Exists(filePath))
          continue;

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
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
      this.importsFrom.Clear();
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
