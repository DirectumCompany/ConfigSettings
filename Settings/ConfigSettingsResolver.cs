using System;
using System.IO;
using CommonLibrary.Settings.Patch;

namespace CommonLibrary.Settings
{
  /// <summary>
  /// ConfigSettingsResolver.
  /// </summary>
  public static class ConfigSettingsResolver
  {
    private static DateTime configSettingsLastWriteTime = DateTime.MinValue;
    
    private static ConfigSettings cachedConfigSettings;

    /// <summary>
    /// Настройки по умолчанию.
    /// </summary>
    public static ConfigSettings DefaultConfigSettings
    {
      get
      {
        var configSettingsPath = ChangeConfig.GetActualConfigSettingsPath();
        if (string.IsNullOrEmpty(configSettingsPath) || !File.Exists(configSettingsPath))
          return null;

        var lastWriteTime = File.GetLastWriteTimeUtc(configSettingsPath);
        if (lastWriteTime == configSettingsLastWriteTime)
          return cachedConfigSettings;

        configSettingsLastWriteTime = lastWriteTime;

        try
        {
          cachedConfigSettings = ChangeConfig.LoadConfigSettings(configSettingsPath);
        }
        catch (Exception)
        {
          return null;
        }

        return cachedConfigSettings;
      }
    }
  }
}