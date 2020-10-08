using System.CommandLine;
using System.CommandLine.DragonFruit;

namespace ConfigSettings.CommandLine
{
  class Program
  {
    public static void Main(string[] args)
    {
      var csWrapper = new ConfigSettingsCommandLineWrapper(new System.CommandLine.IO.SystemConsole());
      var rootCommand = new RootCommand();
      foreach (var mi in ReflectionUtils.GetPublicMethods(csWrapper.GetType()))
      {
        var methodCommand = new Command(mi.Name);
        methodCommand.ConfigureFromMethod(mi, csWrapper);
        methodCommand.AddAlias(methodCommand.Name.ToKebabCase()); 
        foreach(var option in methodCommand.Options)
        {
          option.IsRequired = true;
        }
        rootCommand.Add(methodCommand);
      }

      rootCommand.Invoke(args);
    }
  }
}
