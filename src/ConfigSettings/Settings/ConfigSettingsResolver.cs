using System;
using System.IO;
using ConfigSettings.Patch;

namespace ConfigSettings
{
  /// <summary>
  /// ConfigSettingsResolver.
  /// </summary>
  public static class ConfigSettingsResolver
  {
    private static DateTime configSettingsLastWriteTime = DateTime.MinValue;
    
    private static ConfigSettingsParser cachedConfigSettingsParser;

    /// <summary>
    /// Создать парсер дефолтных настроек.
    /// </summary>
    public static ConfigSettingsParser CreateDefaultConfigSettingsParser()
    {
      var configSettingsPath = ChangeConfig.GetActualConfigSettingsPath();
      if (string.IsNullOrEmpty(configSettingsPath) || !File.Exists(configSettingsPath))
        return null;

      var lastWriteTime = File.GetLastWriteTimeUtc(configSettingsPath);
      if (lastWriteTime == configSettingsLastWriteTime)
        return cachedConfigSettingsParser;

      configSettingsLastWriteTime = lastWriteTime;

      try
      {
        cachedConfigSettingsParser = ChangeConfig.CreateConfigSettingsParser(configSettingsPath);
      }
      catch (Exception)
      {
        return null;
      }

      return cachedConfigSettingsParser;
    }
  }
}