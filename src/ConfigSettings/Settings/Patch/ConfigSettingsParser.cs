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
  /// Парсер файла настроек.
  /// </summary>
  public class ConfigSettingsParser
  {
    #region Поля и свойства

    /// <summary>
    /// Переменные (имя, значение).
    /// </summary>
    private readonly IDictionary<string, VariableValue> variables = new Dictionary<string, VariableValue>();

    /// <summary>
    /// Метапеременные.
    /// </summary>
    private readonly IDictionary<string, VariableValue> metaVariables = new Dictionary<string, VariableValue>();

    /// <summary>
    /// Блоки.
    /// </summary>
    private readonly IDictionary<string, BlockSetting> blocks = new Dictionary<string, BlockSetting>();

    /// <summary>
    /// Импорты.
    /// </summary>
    private readonly IDictionary<string, VariableValue> rootImports = new Dictionary<string, VariableValue>();

    private bool isParsed;

    /// <summary>
    /// Корневой файл настроек.
    /// </summary>
    protected string rootSettingsFilePath;

    /// <summary>
    /// Признак, что есть настройка доступности/недоступности блоков.
    /// </summary>
    public bool HasEnabledOrDisabledBlocks { get { return this.blocks.Any(b => b.Value.IsEnabled != null); } }

    /// <summary>
    /// Признак, что есть настройка содержимого блоков.
    /// </summary>
    public bool HasContentBlocks { get { return this.blocks.Any(b => !string.IsNullOrEmpty(b.Value.Content)); } }

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
        if (filePath != this.rootSettingsFilePath)
        {
          var sourceConfigFile = pair.Value.FilePath;
          result.Add(this.GetAbsoluteImportPath(filePath, sourceConfigFile));
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
    private string GetAbsoluteImportPath(string importFilePath, string sourceConfigFilePath)
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
      return this.blocks.TryGetValue(blockName, out var blockSetting) && blockSetting.IsEnabled == accessibility;
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
      return this.blocks.TryGetValue(blockName, out var blockSetting) ? blockSetting.Content : null;
    }

    private string GetBlockContentWithoutRoot(string blockName)
    {
      return this.blocks.TryGetValue(blockName, out var blockSetting) ? blockSetting.ContentWithoutRoot : null;
    }

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
      return this.variables.ContainsKey(variableName);
    }

    public bool HasRootVariable(string variableName)
    {
      return this.variables.Any(v => v.Value.FilePath.Equals(this.rootSettingsFilePath, StringComparison.OrdinalIgnoreCase) &&
                                     v.Key.Equals(variableName));
    }

    /// <summary>
    /// Получить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetVariableValue(string variableName)
    {
      return this.variables[variableName].Value;
    }

    /// <summary>
    /// Устаноить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    public void SetVariableValue(string variableName, string variableValue)
    {
      var newValue = this.HasVariable(variableName) ?
        new VariableValue(variableValue, this.variables[variableName].FilePath) : new VariableValue(variableValue, this.rootSettingsFilePath);
      this.variables[variableName] = newValue;
    }

    public void RemoveVariable(string variableName)
    {
      if (this.HasRootVariable(variableName))
        this.variables.Remove(variableName);
    }

    /// <summary>
    /// Устаноить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    public void SetMetaVariableValue(string variableName, string variableValue)
    {
      var newValue = this.HasMetaVariable(variableName) ?
        new VariableValue(variableValue, this.metaVariables[variableName].FilePath) : new VariableValue(variableValue, this.rootSettingsFilePath);
      this.metaVariables[variableName] = newValue;
    }

    public void SetBlockValue(string variableName, bool? isBlockEnabled, string blockContent)
    {
      var blockContentWithRoot = $@"<block name=""{variableName}"">{blockContent}</block>";
      if (isBlockEnabled != null)
        blockContentWithRoot = $@"<block name=""{variableName}"" enabled=""{isBlockEnabled}"" >{blockContent}</block>";
      var newValue = this.HasBlock(variableName)
        ? new BlockSetting(isBlockEnabled, blockContentWithRoot, blockContent, this.blocks[variableName].FilePath)
        : new BlockSetting(isBlockEnabled, blockContentWithRoot, blockContent, this.rootSettingsFilePath);
      this.blocks[variableName] = newValue;
    }

    public void SetBlockValue<T>(string variableName, bool? isBlockEnabled, T block) where T : class
    {
      var blockContent = BlockParser.Serialize(block);
      this.SetBlockValue(variableName, isBlockEnabled, blockContent);
    }

    public void SetBlockAccessibility(string variableName, bool isBlockEnabled)
    {
      var blockContentWithRoot = $@"<block name=""{variableName}"" enabled=""{isBlockEnabled}"" ></block>";
      var newValue = this.HasBlock(variableName)
        ? new BlockSetting(true, blockContentWithRoot, null, this.blocks[variableName].FilePath)
        : new BlockSetting(true, blockContentWithRoot, null, this.rootSettingsFilePath);
      this.blocks[variableName] = newValue;
    }


    public void SetImportFrom(string filePath)
    {
      var newValue = this.HasImportFrom(filePath)
        ? new VariableValue(Path.GetFileName(filePath), this.GetImportFrom(filePath).FilePath)
        : new VariableValue(Path.GetFileName(filePath), this.rootSettingsFilePath);
      this.rootImports[filePath] = newValue;
    }

    /// <summary>
    /// Проверить, что для метапеременной в настройках указано значение.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>True - если значение указано.</returns>
    public bool HasMetaVariable(string variableName)
    {
      return this.metaVariables.ContainsKey(variableName);
    }

    public bool HasBlock(string variableName)
    {
      return this.blocks.ContainsKey(variableName);
    }

    /// <summary>
    /// Получить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetMetaVariableValue(string variableName)
    {
      return this.metaVariables[variableName].Value;
    }

    public bool HasImportFrom(string fileName)
    {
      return this.rootImports.Values.Any(v => v.Value.Equals(Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));
    }

    public VariableValue GetImportFrom(string fileName)
    {
      return this.rootImports.First(k => this.rootImports[k.Key].Value.Equals(Path.GetFileName(fileName),
        StringComparison.OrdinalIgnoreCase)).Value;
    }

    /// <summary>
    /// Распарсить корневой xml-источник настроек.
    /// </summary>
    protected void ParseRootSettingsSource()
    {
      if (this.isParsed)
        return;
      this.isParsed = true;

      if (string.IsNullOrEmpty(this.rootSettingsFilePath))
        return;

      // Добавляем корневой элемент.
      this.rootImports[this.rootSettingsFilePath] = new VariableValue(Path.GetFileName(this.rootSettingsFilePath), this.rootSettingsFilePath);

      this.ParseSettingsSource(this.rootSettingsFilePath);
    }

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
      }
      catch (Exception ex) when (!(ex is ParseConfigException))
      {
        throw new ParseConfigException(settingsFilePath, $"An error occured when parsing config file: '{settingsFilePath}'.", ex);
      }
    }

    private void ParseImport(string settingsFilePath, XElement element)
    {
      var fromAttribute = element.Attribute("from");
      if (string.IsNullOrEmpty(fromAttribute?.Value))
        return;

      var filePath = fromAttribute.Value;
      var absolutePath = GetAbsoluteImportPath(filePath, settingsFilePath);

      this.rootImports[filePath] = new VariableValue(Path.GetFileName(absolutePath), settingsFilePath);
      this.ParseSettingsSource(absolutePath);
    }

    private void ParseBlock(string settingsFilePath, XElement element)
    {
      var nameAttribute = element.Attribute("name");
      if (string.IsNullOrEmpty(nameAttribute?.Value))
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
      this.blocks[nameAttribute.Value] = new BlockSetting(isBlockEnabled, blockContent, blockContentWithoutRoot, settingsFilePath);
      if (isBlockEnabled != null)
        this.variables[nameAttribute.Value] = new VariableValue(isBlockEnabled.Value ? "true" : "false", settingsFilePath);
    }

    private void ParseMeta(string settingsFilePath, XElement element)
    {
      var nameAttribute = element.Attribute("name");
      var valueAttribute = element.Attribute("value");
      if (nameAttribute != null && valueAttribute != null)
      {
        if (!string.IsNullOrEmpty(nameAttribute.Value))
          this.metaVariables[nameAttribute.Value] = new VariableValue(valueAttribute.Value, settingsFilePath);
      }
    }

    private void ParseVar(string settingsFilePath, XElement element)
    {
      var nameAttribute = element.Attribute("name");
      var valueAttribute = element.Attribute("value");
      if (nameAttribute != null && valueAttribute != null)
      {
        if (!string.IsNullOrEmpty(nameAttribute.Value))
          this.variables[nameAttribute.Value] = new VariableValue(valueAttribute.Value, settingsFilePath);
      }
    }

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
          rootElement.Add(new XElement("import", new XAttribute("from", kvp.Key)));

        var metaVariablesWithEqualPath = this.metaVariables.Where(v => v.Value.FilePath.Equals(filePath));
        foreach (var kvp in metaVariablesWithEqualPath)
          rootElement.Add(new XElement("meta", new XAttribute("name", kvp.Key), new XAttribute("value", kvp.Value.Value)));

        var variablesWithEqualPath = this.variables.Where(v => v.Value.FilePath.Equals(filePath) && !this.HasBlock(v.Key));
        foreach (var kvp in variablesWithEqualPath)
        {
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
          rootElement.Add(blockContent.Root);
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
      this.rootSettingsFilePath = settingsFilePath;
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
