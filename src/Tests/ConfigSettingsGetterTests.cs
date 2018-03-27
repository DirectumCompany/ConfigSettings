using System;
using System.IO;
using System.Xml.Linq;
using ConfigSettings;
using ConfigSettings.Patch;
using ConfigSettings.Tests;
using ConfigSettings.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettingsTests
{
  [TestFixture]
  public class ConfigSettingsGetterTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath("ConfigSettingsGetter");

    [Test]
    public void WhenGetBooleanThenValueShoudBeTrue()
    {
      var configSettingsPath = this.CreateSettings(@"<var name=""SHOW_WELCOME_TEXT"" value=""true""/>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Get<bool>("SHOW_WELCOME_TEXT").Should().BeTrue();
    }

    [Test]
    public void WhenGetUpperCaseBooleanThenValueShoudBeTrue()
    {
      var configSettingsPath = this.CreateSettings(@"<var name=""SHOW_WELCOME_TEXT"" value=""TRUE""/>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Get<bool>("SHOW_WELCOME_TEXT").Should().BeTrue();
    }

    [Test]
    public void WhenGetBooleanFromEmptyStringThenValueShoudBeFalse()
    {
      var configSettingsPath = this.CreateSettings(@"<var name=""SHOW_WELCOME_TEXT"" value=""""/>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Get<bool>("SHOW_WELCOME_TEXT").Should().BeFalse();
    }

    [Test]
    public void WhenGetUnexistingBooleanThenValueShoudBeFalse()
    {
      var configSettingsPath = this.CreateSettings(@"<var name=""SHOW_WELCOME_TEXT"" value=""""/>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Get<bool>("UNEXISTED_BOOLEAN").Should().BeFalse();
    }

    [Test]
    public void WhenGetBooleanWithoutParserThenValueShoudBeFalse()
    {
      var getter = CreateNullConfigSettingsGetter();
      getter.Get<bool>("UNEXISTED_BOOLEAN").Should().BeFalse();
    }

    private static ConfigSettingsGetter CreateConfigSettingsGetter(string configSettingsPath)
    {
      return new ConfigSettingsGetter(new ConfigSettingsParser(configSettingsPath, XDocument.Load(configSettingsPath)));
    }

    private static ConfigSettingsGetter CreateNullConfigSettingsGetter()
    {
      return new ConfigSettingsGetter(null);
    }

    private string CreateSettings(string settings)
    {
      var content = $@"<?xml version='1.0' encoding='utf-8'?>
<settings>
{settings}
</settings>";
      var fileName = Path.Combine(this.tempPath, $@"test_settings_{Guid.NewGuid().ToShortString()}.xml");
      File.WriteAllText(fileName, content);
      return fileName;
    }
  }
}
