using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConfigSettings
{
  /// <summary>
  /// Смена имени основного конфига системы.
  /// </summary>
  public abstract class AppConfig
  {
    /// <summary>
    /// Путь до основного конфиг файла.
    /// </summary>
    private static string appConfigFilePath;

    private static string AppConfigFilePath
    {
      get
      {
        if (string.IsNullOrEmpty(appConfigFilePath))
          appConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

        return appConfigFilePath;
      }
    }

    /// <summary>
    /// Сменить имя.
    /// </summary>
    /// <returns>Экземпляр класса. Также создаёт новый конфиг согласно правилам.</returns>
    public static AppConfig Change()
    {
      var liveConfigPath = ChangeConfig.Execute(AppConfigFilePath);
      return new ChangeAppConfig(liveConfigPath);
    }

    /// <summary>
    /// Внутренний класс для подмены ClientConfigPaths. Работает через reflection.
    /// </summary>
    private class ChangeAppConfig : AppConfig
    {
      public ChangeAppConfig(string path)
      {
        AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
        ResetConfigMechanism();
      }

      private static void ResetConfigMechanism()
      {
        typeof(ConfigurationManager)
            .GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, 0);

        typeof(ConfigurationManager)
            .GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, null);

        typeof(ConfigurationManager)
            .Assembly.GetTypes()
            .First(x => x.FullName ==
                        "System.Configuration.ClientConfigPaths")
            .GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, null);
      }
    }
  }
}
