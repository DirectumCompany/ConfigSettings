using System;
using System.IO;
using ConfigSettings.Utils;

namespace ConfigSettingsTests
{
  public static class TestTools
  {
    public static string CreateSettings(string settings, string filePath)
    {
      var content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
{settings}
</settings>";
      var fileName = Path.Combine(filePath, $@"test_settings_{Guid.NewGuid().ToShortString()}.xml");
      File.WriteAllText(fileName, content);
      return fileName;
    }

    public static string GetConfigSettings(string configPath)
    {
      var content = File.ReadAllText(configPath);
      content = content.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>", string.Empty).Replace("</settings>", string.Empty);
      content = content.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings />", string.Empty);
      return content;
    }
  }
}