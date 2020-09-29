using System;
using System.IO;
using System.Xml;
using ConfigSettings.Patch;
using ConfigSettings.Utils;
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
    public void WhenChangeVariableThenBlockShouldNotChanged()
    {
      var configSettingsPath = this.CreateSettings(@"   <var name=""GIT_ROOT_DIRECTORY"" value=""d:\ee"" />
  <block name=""REPOSITORIES"">
    <repository folderName=""base"" solutionType=""Base"" url="""" />
    <repository folderName=""work"" solutionType=""Work"" url="""" />
  </block>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Set("GIT_ROOT_DIRECTORY", "d:\\ee2");
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <var name=""GIT_ROOT_DIRECTORY"" value=""d:\ee2"" />
  <block name=""REPOSITORIES"">
    <repository folderName=""base"" solutionType=""Base"" url="""" />
    <repository folderName=""work"" solutionType=""Work"" url="""" />
  </block>
");
    }

    [Test]
    public void WhenSetEmptyBlockContent()
    {
      var configSettingsPath = this.CreateSettings(@"  <block name=""REPOSITORIES"">
  </block>");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.SetBlock("TESTBLOCK", null, null);
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <block name=""REPOSITORIES""></block>
  <block name=""TESTBLOCK""></block>
");
    }

    [Test]
    public void WhenSetEmptyBlockEnabled()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <block name=""ORIGIN_TRUE_BLOCK"" enabled=""True""/>
  <block name=""ORIGIN_FALSE_BLOCK"" enabled=""false""/>
  <block name=""ORIGIN_NULL_BLOCK""/>
");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.SetBlock("TEST_TRUE_BLOCK", true, null);
      getter.SetBlock("TEST_FALSE_BLOCK", false, null);
      getter.SetBlock("TEST_NULL_BLOCK", null, null);
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <block name=""ORIGIN_TRUE_BLOCK"" enabled=""true""></block>
  <block name=""ORIGIN_FALSE_BLOCK"" enabled=""false""></block>
  <block name=""ORIGIN_NULL_BLOCK""></block>
  <block name=""TEST_TRUE_BLOCK"" enabled=""true""></block>
  <block name=""TEST_FALSE_BLOCK"" enabled=""false""></block>
  <block name=""TEST_NULL_BLOCK""></block>
");
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
    public void WhenSetBlockValueAndEnabled()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <block name=""ORIGIN_TRUE_BLOCK"" enabled=""false"">
    <repository folderName=""base"" solutionType=""Base"" url="""" />
    <repository folderName=""work"" solutionType=""Work"" url="""" />
</block>
");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.SetBlock("TEST_TRUE_BLOCK", true, @"
  <testRepository folderName=""base"" solutionType=""Base"" url="""" />
  <testRepository folderName=""work"" solutionType=""Work"" url="""" />");
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <block name=""ORIGIN_TRUE_BLOCK"" enabled=""false"">
    <repository folderName=""base"" solutionType=""Base"" url="""" />
    <repository folderName=""work"" solutionType=""Work"" url="""" />
  </block>
  <block name=""TEST_TRUE_BLOCK"" enabled=""true"">
    <testRepository folderName=""base"" solutionType=""Base"" url="""" />
    <testRepository folderName=""work"" solutionType=""Work"" url="""" />
  </block>
");
    }

    [Test]
    public void WhenSetRelativeImportThenPathShouldNotBeAbsolute()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <import from=""origin/import/from"" />
");

      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.SetImport("test/import/from");
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <import from=""origin/import/from"" />
  <import from=""test/import/from"" />
");
    }

    [Test]
    public void WhenParseEmptyImportAndAddVariableThenImportBlockShouldNotBeSaved()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <import from="""" />
");
      var getter = CreateConfigSettingsGetter(configSettingsPath);
      getter.Set("testName", "testValue");
      getter.Save();
      var content = this.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <var name=""testName"" value=""testValue"" />
");
    }

    [Test]
    public void WhenSetValueWithNullConfigSettingsParserThenExceptionShouldBeRaisen()
    {
      var getter = CreateConfigSettingsGetter(null);
      getter.Get<bool>("NEW_VALUE").Should().BeFalse();
      getter.Set("NEW_VALUE", true);
      getter.Get<bool>("NEW_VALUE").Should().BeTrue();
    }

    [Test]
    public void WhenSaveNullConfigSettingsParserThenSaveShouldNotWork()
    {
      var getter = CreateConfigSettingsGetter(null);
      getter.Set("NEW_VALUE", true);
      var exception = ((MethodThatThrows)delegate { getter.Save(); }).GetException();
      exception.Should().BeOfType<InvalidOperationException>();
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
      var content = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
{settings}
</settings>";
      var fileName = Path.Combine(this.tempPath, $@"test_settings_{Guid.NewGuid().ToShortString()}.xml");
      File.WriteAllText(fileName, content);
      return fileName;
    }

    public string GetConfigSettings(string configPath)
    {
      var content = File.ReadAllText(configPath);
      return content.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>", string.Empty).Replace("</settings>", string.Empty);
    }
  }
}
