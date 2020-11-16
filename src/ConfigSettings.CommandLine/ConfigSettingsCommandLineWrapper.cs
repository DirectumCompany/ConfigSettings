using System.Collections;
using System.CommandLine;
using System.CommandLine.IO;
using ConfigSettings.Patch;

namespace ConfigSettings.CommandLine
{
  /// <summary>
  /// Обёртка надо ConfigSettingsParser и Getter для вызова из командной строки.
  /// </summary>
  public class ConfigSettingsCommandLineWrapper
  {
    private static ConfigSettingsParser CreateParser(string settingsFilePath) => new ConfigSettingsParser(settingsFilePath);

    private static ConfigSettingsGetter CreateGetter(string settingsFilePath) => new ConfigSettingsGetter(new ConfigSettingsParser(settingsFilePath));

    private readonly IConsole console;

    private void LogIfNotEmpty(object resultItem)
    {
      var stringValue = resultItem?.ToString();
      if (!string.IsNullOrWhiteSpace(stringValue))
        this.console.Out.WriteLine(stringValue);
    }

    private void LogResult(object result)
    {
      if (result is IList resultAsList)
      {
        foreach (var resultItem in resultAsList) 
          LogIfNotEmpty(resultItem);
        
        return;
      }
      
      LogIfNotEmpty(result);
    }

    /// <summary>
    /// Дефолтное имя файла с настройками.
    /// </summary>
    /// <returns></returns>
    public int DefaultFilename()
    {
      this.LogResult(ChangeConfig.DefaultConfigSettingsFileName);
      return 0;
    }

    public int GetImports(string path)
    {
      this.LogResult(CreateParser(path).GetAllImports());
      return 0;
    }

    public int Has(string path, string name)
    {
      this.LogResult(CreateParser(path).HasVariable(name));
      return 0;
    }
    
    public int HasBlock(string path, string name)
    {
      this.LogResult(CreateParser(path).HasBlock(name));
      return 0;
    }
    
    public int Get(string settingsFilePath, string variableName)
    {
      this.LogResult(CreateGetter(settingsFilePath).Get<string>(variableName));
      return 0;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="console">Интерфейс консоли.</param>
    public ConfigSettingsCommandLineWrapper(IConsole console)
    {
      this.console = console;
    }
  }
}
