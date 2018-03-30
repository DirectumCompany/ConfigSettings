using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Настройки конфига.
  /// </summary>
  public class ConfigSettingsParser
  {
    #region Поля и свойства

    /// <summary>
    /// Переменные (имя, значение).
    /// </summary>
    private readonly IDictionary<string, string> variables = new Dictionary<string, string>();

    /// <summary>
    /// Метапеременные.
    /// </summary>
    private readonly IDictionary<string, string> metaVariables = new Dictionary<string, string>();

    /// <summary>
    /// Блоки.
    /// </summary>
    private readonly IDictionary<string, BlockSetting> blocks = new Dictionary<string, BlockSetting>();    
    
    /// <summary>
    /// Блоки.
    /// </summary>
    private readonly IDictionary<string, string> rootImports = new Dictionary<string, string>();

    private bool isParsed;

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
      BlockSetting blockSetting;
      return this.blocks.TryGetValue(blockName, out blockSetting) && blockSetting.IsEnabled == accessibility;
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
    /// Определить, что для блока есть настроенное содержимое.
    /// </summary>
    /// <param name="blockName">Имя блока.</param>
    /// <returns>True - если есть настройка с соержимым блока.</returns>
    public string GetBlockContent(string blockName)
    {
      BlockSetting blockSetting;
      return this.blocks.TryGetValue(blockName, out blockSetting) ? blockSetting.Content : null;
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

    /// <summary>
    /// Получить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetVariableValue(string variableName)
    {
      return this.variables[variableName];
    }

    /// <summary>
    /// Устаноить значение переменной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <param name="variableValue">Значение переменной.</param>
    public void SetVariableValue(string variableName, string variableValue)
    {
       this.variables[variableName] = variableValue;
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

    /// <summary>
    /// Получить значение метапеременной, указанное в настройке.
    /// </summary>
    /// <param name="variableName">Имя переменной.</param>
    /// <returns>Знаение переменной.</returns>
    public string GetMetaVariableValue(string variableName)
    {
      return this.metaVariables[variableName];
    }

    public bool HasImportFrom(string fileName)
    {
      return this.rootImports.ContainsKey(fileName);
    }

    /// <summary>
    /// Распарсить xml-источник настроек.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="settingsSource">Содержимое фала настроек.</param>
    private void ParseRootSettingsSource(string settingsFilePath, XDocument settingsSource)
    {
      if (this.isParsed)
        return;

      this.isParsed = true;

      this.ParseSettingsSource(settingsFilePath, settingsSource, true);
    }

    private void ParseSettingsSource(string settingsFilePath, XDocument settings, bool fromRoot)
    {
      if (settings?.Root == null)
        return;

      // Сначала обрабатываем import-ы, чтобы не зависеть от их местоположения в xml-ке.
      foreach (var element in settings.Root.Elements())
      {
        var elementName = element.Name.LocalName;
        if (elementName == "import")
          this.ParseImport(settingsFilePath, element, fromRoot);
      }

      // Затем обрабатываем остальные элементы.
      foreach (var element in settings.Root.Elements())
      {
        var elementName = element.Name.LocalName;

        if (elementName == "var")
          this.ParseVar(element);
        else if (elementName == "meta")
          this.ParseMeta(element);
        else if (elementName == "block")
          this.ParseBlock(element);
      }
    }

    private void ParseImport(string settingsFilePath, XElement element, bool fromRoot)
    {
      var fromAttribute = element.Attribute("from");
      if (string.IsNullOrEmpty(fromAttribute?.Value))
        return;

      var filePath = fromAttribute.Value;
      var absolutePath = Path.IsPathRooted(filePath) ? filePath : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath), filePath));

      if (!File.Exists(absolutePath))
        return;

      if (fromRoot)
        this.rootImports[Path.GetFileName(absolutePath)] = absolutePath;

      this.ParseSettingsSource(absolutePath, XDocument.Load(absolutePath), false);
    }

    private void ParseBlock(XElement element)
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

      var blockContent = !string.IsNullOrEmpty(string.Concat(element.Nodes())) ? element.ToString() : null;
      this.blocks[nameAttribute.Value] = new BlockSetting(isBlockEnabled, blockContent);
      if (isBlockEnabled != null)
        this.variables[nameAttribute.Value] = isBlockEnabled.Value ? "true" : "false";
    }

    private void ParseMeta(XElement element)
    {
      var nameAttribute = element.Attribute("name");
      var valueAttribute = element.Attribute("value");
      if (nameAttribute != null && valueAttribute != null)
      {
        if (!string.IsNullOrEmpty(nameAttribute.Value))
          this.metaVariables[nameAttribute.Value] = valueAttribute.Value;
      }
    }

    private void ParseVar(XElement element)
    {
      var nameAttribute = element.Attribute("name");
      var valueAttribute = element.Attribute("value");
      if (nameAttribute != null && valueAttribute != null)
      {
        if (!string.IsNullOrEmpty(nameAttribute.Value))
          this.variables[nameAttribute.Value] = valueAttribute.Value;
      }
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="settingsFilePath">Путь к файлу с настройками.</param>
    /// <param name="settingsSource">Xml-источник настроек.</param>
    public ConfigSettingsParser(string settingsFilePath, XDocument settingsSource)
    {
      this.ParseRootSettingsSource(settingsFilePath, settingsSource);
    }

    #endregion
  }
}
