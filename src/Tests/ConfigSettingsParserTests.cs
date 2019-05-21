using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ConfigSettings.Utils;
using ConfigSettings.Patch;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  public class TestTenant
  {
    [XmlAttribute]
    public string Name { get; set; }
  }

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

    [Test]
    public void TestGetBlockTyped()
    {
      var settings = this.CreateSettings(@"
  <block name=""testBlockName"">
    <TestTenant Name=""alpha"" Db=""alpha_db"" />
    <TestTenant Name=""beta"" User=""alpha_user"" />
  </block>");
      var parser = new ConfigSettingsParser(settings);
      var tenants = parser.GetBlockContent<List<TestTenant>>("testBlockName");
      tenants.Should().HaveCount(2);
      tenants[0].Name.Should().Be("alpha");
      tenants[1].Name.Should().Be("beta");
    }    
    
    [Test]
    public void TestSetBlockTyped()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      var tenants = new List<TestTenant>
      {
        new TestTenant { Name = "t1"}, 
        new TestTenant {Name = "t2"}
      };
      parser.SetBlockValue("testBlockName", true, tenants);
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <block name=""testBlockName"" enabled=""True"">
    <TestTenant Name=""t1"" />
    <TestTenant Name=""t2"" />
  </block>
");
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
