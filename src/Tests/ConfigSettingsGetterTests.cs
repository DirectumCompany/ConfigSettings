using System;
using System.Xml;
using ConfigSettings.Patch;
using ConfigSettingsTests;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
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
      var getter = CreateConfigSettingsGetter(null);
      getter.Get<bool>("UNEXISTED_BOOLEAN").Should().BeFalse();
    }

    [Test]
    public void WhenGetEmptyXmlBlock()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <block name=""BLOCK1"" enabled=""true""/>
  <block name=""BLOCK2""></block>
");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.GetBlock("BLOCK1").Should().Be(null);
      getter.GetBlock("BLOCK2").Should().Be(null);
    }

    [Test]
    public void WhenGetNotExistXmlBlock()
    {
      var configSettingsPath = this.CreateSettings("");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.GetBlock("BLOCK1").Should().Be(null);
    }

    [Test]
    public void WhenGetXmlBlock()
    {
      var configSettingsPath = this.CreateSettings(
@"<block name=""ORIGIN_TRUE_BLOCK"" enabled=""false"">
  <repository folderName=""base"" solutionType=""Base"" url="""" />
  <repository folderName=""work"" solutionType=""Work"" url="""" />
</block>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      var content = getter.GetXmlBlock("ORIGIN_TRUE_BLOCK");
      content.ToString().Should().Be(
@"<block name=""ORIGIN_TRUE_BLOCK"" enabled=""false"">
  <repository folderName=""base"" solutionType=""Base"" url="""" />
  <repository folderName=""work"" solutionType=""Work"" url="""" />
</block>");
    }

    [Test]
    public void WhenGetBadXmlBlock()
    {
      var configSettingsPath = this.CreateSettings("<block error!");

      var thrownException = ((MethodThatThrows)delegate
      {
        CreateConfigSettingsGetter(configSettingsPath);
      }).GetException();

      thrownException.Should().BeOfType<ParseConfigException>();
      thrownException.InnerException.Should().BeOfType<XmlException>();
      ((ParseConfigException)thrownException).CorruptedFilePath.Should().Be(configSettingsPath);
    }

    [Test]
    public void WhenParseTimestampFromString()
    {
      var getter = CreateConfigSettingsGetter(this.CreateSettings(@"<var name=""FOLDER_AUTO_UPDATE_PERIOD"" value=""00:10:00"" />"));
      var period = getter.Get<TimeSpan>("FOLDER_AUTO_UPDATE_PERIOD");
      period.TotalSeconds.Should().Be(600.0);
    }

    private static ConfigSettingsGetter CreateConfigSettingsGetter(string configSettingsPath)
    {
      return new ConfigSettingsGetter(new ConfigSettingsParser(configSettingsPath));
    }

    private string CreateSettings(string settings)
    {
      return TestTools.CreateSettings(settings, this.tempPath);
    }
  }
}
