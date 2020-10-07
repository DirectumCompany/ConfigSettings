using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConfigSettings.Patch;
using ConfigSettings.Utils;

namespace ConfigSettings
{
  /// <summary>
  /// Изменение конфига на основе правил.
  /// </summary>
  public static class ChangeConfig
  {
    #region Константы

    /// <summary>
    /// Дефолтное имя файла с настройками конфига.
    /// </summary>
    public const string DefaultConfigSettingsFileName = "_ConfigSettings.xml";

    /// <summary>
    /// Имя для перегенерируемого блока настроек.
    /// </summary>
    private const string GeneratedBlockName = "{~GENERATED}";

    /// <summary>
    /// Имя метапеременной для принудительного использования пути к .live.config файлу из appdata.
    /// </summary>
    private const string ForceUseAppDataPathMetaVariable = "FORCE_USE_APPDATA_PATH";

    /// <summary>
    /// Объект синхронизации.
    /// </summary>
    private static readonly object lockInstance = new object();

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Явно указанный путь к файлу с настройками конфига.
    /// </summary>
    public static string ConfigSettingsPath { get; set; }

    /// <summary>
    /// Явно указанная директории, с которой нужно начинать поиск файла настроек.
    /// </summary>
    public static string ConfigSettingsDirectory { get; set; }

    #endregion

    #region Методы

    /// <summary>
    /// Найти в директории файл по имени (по точному совпадению или постфиксу).
    /// </summary>
    /// <param name="directoryPath">Путь к директории.</param>
    /// <param name="fileName">Имя файла (точное или постфикс).</param>
    /// <returns>Путь у найденному файлу.</returns>
    public static string FindFirstPathByMask(string directoryPath, string fileName)
    {
      var settingsPath = Path.Combine(directoryPath, fileName);

      if (File.Exists(settingsPath))
        return settingsPath;

      return Directory.GetFiles(directoryPath, $"*{Path.GetFileName(fileName)}").FirstOrDefault();
    }

    /// <summary>
    /// Получить путь к файлу с настройками конфига.
    /// </summary>
    /// <param name="configSettingsFileName">Имя файла (без пути) с настройками конфига.</param>
    /// <returns>Путь к файлу с настройками конфига.</returns>
    private static string FindConfigSettingsPath(string configSettingsFileName)
    {
      string configSettingsPath = null;
      var currentDirectory = !configSettingsFileName.Contains(Path.DirectorySeparatorChar) && !string.IsNullOrEmpty(ConfigSettingsDirectory) ?
        ConfigSettingsDirectory :
        AppDomain.CurrentDomain.BaseDirectory;
      while (!string.IsNullOrEmpty(currentDirectory))
      {
        configSettingsPath = FindFirstPathByMask(currentDirectory, configSettingsFileName);
        if (!string.IsNullOrEmpty(configSettingsPath))
          break;

        try
        {
          var parent = Directory.GetParent(currentDirectory);
          currentDirectory = parent?.FullName;
        }
        catch (IOException)
        {
          currentDirectory = null;
        }
        catch (UnauthorizedAccessException)
        {
          currentDirectory = null;
        }
      }
      return !string.IsNullOrEmpty(configSettingsPath) ? configSettingsPath : Constants.UnexistedConfigSettingsPath;
    }

    /// <summary>
    /// Проверить доступ на чтение в папке.
    /// </summary>
    /// <param name="directoryPath">Путь к папке.</param>
    public static void CheckDirectoryWriteAccess(string directoryPath)
    {
      using (File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
      {
      }
    }

    /// <summary>
    /// Проверить, что файл недоступен для изменения (или создания).
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>True - если изменение (или создание) файла недоступно.</returns>
    public static bool IsFileLocked(string filePath)
    {
      FileStream stream = null;
      try
      {
        var dirPath = Path.GetDirectoryName(Path.GetFullPath(filePath));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        if (!File.Exists(filePath))
        {
          CheckDirectoryWriteAccess(dirPath);
          return false;
        }

        // TODO: Добавить многопоточную проверку прав на запись без использования FileAccess.ReadWrite.
        var fileInfo = new FileInfo(filePath);
        stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return fileInfo.IsReadOnly;
      }
      catch (Exception ex)
      {
        if (ex is IOException || ex is UnauthorizedAccessException)
          return true;

        throw;
      }
      finally
      {
        stream?.Close();
      }
    }

    /// <summary>
    /// Получить путь к реальному (live) файлу-конфигу.
    /// </summary>
    /// <param name="currentConfigPath">Путь к оригинальному файлу-конфигу.</param>
    /// <returns>Путь к реальному (live) файлу-конфигу.</returns>
    public static string GetLiveConfigFilePath(string currentConfigPath)
    {
      return GetLiveConfigFilePath(currentConfigPath, false, null);
    }

    /// <summary>
    /// Получить путь к реальному (live) файлу-конфигу.
    /// </summary>
    /// <param name="currentConfigPath">Путь к оригинальному файлу-конфигу.</param>
    /// <param name="forceReturnAppDataPath">Принудительно возвращать путь к appdata.</param>
    /// <param name="resolveForcedAppDataPath">Функция разрешения пути до папки в appdata, когда флаг forceReturnAppDataPath включен.</param>
    /// <returns>Путь к реальному (live) файлу-конфигу.</returns>
    private static string GetLiveConfigFilePath(string currentConfigPath, bool forceReturnAppDataPath, Func<string, string> resolveForcedAppDataPath)
    {
      var nameWithoutExt = Path.GetFileNameWithoutExtension(currentConfigPath);
      var liveConfigName = nameWithoutExt + ".live" + Path.GetExtension(currentConfigPath);
      if (!forceReturnAppDataPath)
      {
        var liveConfigBesideCurrent = Path.Combine(Path.GetDirectoryName(currentConfigPath), liveConfigName);
        if (!IsFileLocked(liveConfigBesideCurrent))
          return liveConfigBesideCurrent;
      }

      if (resolveForcedAppDataPath == null)
        throw new ArgumentNullException(nameof(resolveForcedAppDataPath), "Remove FORCE_USE_APPDATA_PATH meta tag or specify resolveForcedAppDataPath function on ChangeConfig.Execute");

      var appDataPath = resolveForcedAppDataPath(currentConfigPath);
      if (!Directory.Exists(appDataPath))
        Directory.CreateDirectory(appDataPath);
      return Path.Combine(appDataPath, liveConfigName);
    }

    /// <summary>
    /// Выполнить изменение конфига на основе правил.
    /// </summary>
    /// <param name="currentConfigPath">Путь к текущем конфигу.</param>
    /// <returns>Путь к измененному конфигу.</returns>
    public static string Execute(string currentConfigPath)
    {
      return Execute(currentConfigPath, null, null);
    }

    /// <summary>
    /// Выполнить изменение конфига на основе правил.
    /// </summary>
    /// <param name="currentConfigPath">Путь к текущем конфигу.</param>
    /// <param name="resolveForcedAppDataPath">Функция разрешения пути до папки в appdata, когда указан мета тег FORCE_USE_APPDATA_PATH.</param>
    /// <returns>Путь к измененному конфигу.</returns>
    public static string Execute(string currentConfigPath, Func<string, string> resolveForcedAppDataPath)
    {
      return Execute(currentConfigPath, null, resolveForcedAppDataPath);
    }

    /// <summary>
    /// Выполнить изменение конфига на основе правил.
    /// </summary>
    /// <param name="currentConfigPath">Путь к текущем конфигу.</param>
    /// <param name="settingsFileName">Имя файла с настройками конфига.</param>
    /// <returns>Путь к измененному конфигу.</returns>
    public static string Execute(string currentConfigPath, string settingsFileName)
    {
      return Execute(currentConfigPath, settingsFileName, null);
    }

    /// <summary>
    /// Выполнить изменение конфига на основе правил.
    /// </summary>
    /// <param name="currentConfigPath">Путь к текущем конфигу.</param>
    /// <param name="settingsFileName">Имя файла с настройками конфига.</param>
    /// <param name="resolveForcedAppDataPath">Функция разрешения пути до папки в appdata, когда флаг forceReturnAppDataPath включен.</param>
    /// <returns>Путь к измененному конфигу.</returns>
    public static string Execute(string currentConfigPath, string settingsFileName, Func<string, string> resolveForcedAppDataPath)
    {
      lock (lockInstance)
      {
        var config = XDocument.Load(currentConfigPath);
        var parser = CreateConfigSettingsParser(settingsFileName);

        new ConfigPatch(config, parser).Patch();
        new LogSettingsPatch(config, currentConfigPath).Patch();

        bool forceUseAppDataPath;
        var liveConfigPath = parser.HasMetaVariable(ForceUseAppDataPathMetaVariable) &&
                                bool.TryParse(parser.GetMetaVariableValue(ForceUseAppDataPathMetaVariable),
                                  out forceUseAppDataPath)
          ? GetLiveConfigFilePath(currentConfigPath, forceUseAppDataPath, resolveForcedAppDataPath)
          : GetLiveConfigFilePath(currentConfigPath);
        if (HasGeneratedBlock(config) && File.Exists(liveConfigPath))
          ReplaceConfigWithoutGeneratedBlocks(config, liveConfigPath);
        else
          FileUtils.ReplaceXmlFile(config, liveConfigPath);
        return liveConfigPath;
      }
    }

    /// <summary>
    /// Выполнить изменение конфига на основе правил без сохранения изменений.
    /// </summary>
    /// <param name="currentConfigPath">Путь к текущем конфигу.</param>
    /// <param name="settingsFileName">Имя файла с настройками конфига.</param>
    /// <returns>Измененный конфиг.</returns>
    public static XDocument ExecuteWithoutChange(string currentConfigPath, string settingsFileName)
    {
      lock (lockInstance)
      {
        var config = XDocument.Load(currentConfigPath);
        var parser = CreateConfigSettingsParser(settingsFileName);

        new ConfigPatch(config, parser).Patch();
        new LogSettingsPatch(config, currentConfigPath).Patch();

        return config;
      }
    }

    /// <summary>
    /// Получить путь к файлу настроек конфига.
    /// </summary>
    /// <returns>Путь к файлу настроек конфига.</returns>
    public static string GetActualConfigSettingsPath()
    {
      return GetActualConfigSettingsPath(null);
    }

    private static string GetActualConfigSettingsPath(string settingsFileName)
    {
      string configSettingsPath;
      if (!string.IsNullOrEmpty(settingsFileName))
        configSettingsPath = FindConfigSettingsPath(settingsFileName);
      else if (!string.IsNullOrEmpty(ConfigSettingsPath))
        configSettingsPath = ConfigSettingsPath.Contains(Path.DirectorySeparatorChar) ? ConfigSettingsPath : FindConfigSettingsPath(ConfigSettingsPath);
      else
        configSettingsPath = FindConfigSettingsPath(DefaultConfigSettingsFileName);

      return configSettingsPath;
    }

    /// <summary>
    /// Создать парсер настроек.
    /// </summary>
    /// <param name="settingsFileName">Путь к файлу настроек конфига.</param>
    /// <returns>Парсер.</returns>
    public static ConfigSettingsParser CreateConfigSettingsParser(string settingsFileName)
    {
      var configSettingsPath = GetActualConfigSettingsPath(settingsFileName);
      return new ConfigSettingsParser(configSettingsPath);
    }

    /// <summary>
    /// Определить, есть ли в конфиге перегенерируемые блоки.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    /// <returns>True - если есть перегенерируемые блоки.</returns>
    private static bool HasGeneratedBlock(XDocument config)
    {
      return config.DescendantNodes()
        .OfType<XComment>()
        .Any(commentNode => string.Equals(commentNode.Value.Trim(), GeneratedBlockName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Замена конфига с проверкой на различие с учетом перегенерируемых блоков.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    /// <param name="configPath">Путь к файлу конфига.</param>
    private static void ReplaceConfigWithoutGeneratedBlocks(XDocument config, string configPath)
    {
      var currentConfig = XDocument.Load(configPath);
      var newConfig = new XDocument(config);
      RemoveGeneratedBlocks(currentConfig);
      RemoveGeneratedBlocks(newConfig);
      var currentContent = FileUtils.ToBytes(currentConfig);
      var newContent = FileUtils.ToBytes(newConfig);
      if (!currentContent.SafeSequenceEqual(newContent))
        File.WriteAllBytes(configPath, FileUtils.ToBytes(config));
    }

    /// <summary>
    /// Удалить перегенерируемые блоки из конфига.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    private static void RemoveGeneratedBlocks(XDocument config)
    {
      var generatedBlocks = new List<XNode>();
      CollectGeneratedBlocks(config.Root, generatedBlocks);
      foreach (var node in generatedBlocks)
        node.Remove();
    }

    /// <summary>
    /// Найти перегенерируемые блоки.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="generatedBlocks">Собираемые перегенерируемые блоки.</param>
    private static void CollectGeneratedBlocks(XElement element, List<XNode> generatedBlocks)
    {
      var nextElementIsGenerated = false;
      foreach (var node in element.Nodes())
      {
        var commentNode = node as XComment;
        var elementNode = node as XElement;
        if (commentNode != null)
        {
          var comment = commentNode.Value.Trim();
          if (comment.Length > 3 && comment.StartsWith("{~", StringComparison.Ordinal) && comment.EndsWith("}", StringComparison.Ordinal))
          {
            if (string.Equals(comment, GeneratedBlockName, StringComparison.Ordinal))
            {
              generatedBlocks.Add(commentNode);
              nextElementIsGenerated = true;
            }
          }
          else
            nextElementIsGenerated = false;
        }
        else if (elementNode != null)
        {
          if (nextElementIsGenerated)
          {
            generatedBlocks.Add(node);
            nextElementIsGenerated = false;
          }
          CollectGeneratedBlocks(elementNode, generatedBlocks);
        }
      }
    }

    #endregion
  }
}
