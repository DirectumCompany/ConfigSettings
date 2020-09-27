using System.CommandLine;
using System.CommandLine.IO;
using ConfigSettings.Patch;

namespace ConfigSettings.CommandLine
{
  public class ConfigSettingsCommandLineWrapper
  {
    private static ConfigSettingsParser CreateParser(string settingsFilePath) => new ConfigSettingsParser(settingsFilePath);

    private static ConfigSettingsGetter CreateGetter(string settingsFilePath) => new ConfigSettingsGetter(new ConfigSettingsParser(settingsFilePath));

    private readonly IConsole console;

    private void LogResult(object result)
    {
      this.console.Out.WriteLine(result.ToString());
    }

    public int DefaultConfigSettingsFileName()
    {
      this.LogResult(ChangeConfig.DefaultConfigSettingsFileName);
      return 0;
    }

    public int GetAllImports(string settingsFilePath)
    {
      this.LogResult(CreateParser(settingsFilePath).GetAllImports());
      return 0;
    }

    public int HasVariable(string settingsFilePath, string variableName)
    {
      this.LogResult(CreateParser(settingsFilePath).HasVariable(variableName));
      return 0;
    }
    public int Get(string settingsFilePath, string variableName)
    {
      this.LogResult(CreateGetter(settingsFilePath).Get<string>(variableName));
      return 0;
    }

    public ConfigSettingsCommandLineWrapper(IConsole console)
    {
      this.console = console;
    }
  }
}
