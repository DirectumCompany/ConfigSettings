using System;
using System.IO;
using System.Xml.Linq;
using ConfigSettings.Patch;
using ConfigSettings.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  [TestFixture]
  public class ConfigSettingsParserTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath("ConfigSettingsParserTests");

    private string TempConfigFilePath => Path.Combine(this.tempPath, TestContext.CurrentContext.Test.MethodName + ".xml");

    [Test]
    public void WhenSaveVariablesInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariableName", "testVariableValue");
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <var name=""testVariableName"" value=""testVariableValue"" />
");
    }

    [Test]
    public void WhenSaveMetaVariableInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetMetaVariableValue("testMetaVariableName", "testVariableValue");
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <meta name=""testMetaVariableName"" value=""testVariableValue"" />
");
    }

    [Test]
    public void WhenSaveBlockInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetBlockValue("testBlockName", null, @"  <tenant name=""alpha"" db=""alpha_db"" />
    <tenant name=""beta"" user=""alpha_user"" />");
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <block name=""testBlockName"">
    <tenant name=""alpha"" db=""alpha_db"" />
    <tenant name=""beta"" user=""alpha_user"" />
  </block>
");
    }

    [Test]
    public void WhenSaveImportFromInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetImportFrom(@"test\file\path");
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <import from=""test\file\path"" />
");
    }

    [Test]
    public void TestHasImportFromExistedFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetImportFrom(@"test\file\path");
      parser.HasImportFrom("path").Should().BeTrue();
      parser.HasImportFrom(@"test\file\path").Should().BeTrue();
      parser.HasImportFrom("path1").Should().BeFalse();
      parser.HasImportFrom("file").Should().BeFalse();
    }

    [Test]
    public void TestHasImportFromUnexistedFile()
    {
      var unexistedImport = Guid.NewGuid() + ".xml";
      var settings = this.CreateSettings($@"<import from=""{unexistedImport}"" />");
      var parser = new ConfigSettingsParser(settings);
      parser.HasImportFrom(unexistedImport).Should().BeTrue();
    }

    public string GetConfigSettings(string configPath)
    {
      var content = File.ReadAllText(configPath);
      return content.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>", string.Empty).Replace("</settings>", string.Empty);
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
