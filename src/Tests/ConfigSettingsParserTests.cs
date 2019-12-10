using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ConfigSettings.Patch;
using ConfigSettings.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  public class TestTenant
  {
    [XmlAttribute]
    public string Name { get; set; }
  }

  public class TenantGroup
  {
    [XmlAttribute]
    public List<TestTenant> Tenants { get; set; }
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


    [Test]
    public void TestSetBlockTypedArray()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);

      var tenantGroups = new List<List<TestTenant>>()
      {
        new List<TestTenant>
        {
          new TestTenant { Name = "a1" },
          new TestTenant { Name = "a2" }
        },
        new List<TestTenant>
        {
          new TestTenant { Name = "b1" },
          new TestTenant { Name = "b2" }
        },
      };

      parser.SetBlockValue("tenantGroups", true, tenantGroups);
      parser.Save();

      this.GetConfigSettings(this.TempConfigFilePath).Should().Be(@"
  <block name=""tenantGroups"" enabled=""True"">
    <ArrayOfTestTenant>
      <TestTenant Name=""a1"" />
      <TestTenant Name=""a2"" />
    </ArrayOfTestTenant>
    <ArrayOfTestTenant>
      <TestTenant Name=""b1"" />
      <TestTenant Name=""b2"" />
    </ArrayOfTestTenant>
  </block>
");
    }

    [Test]
    public void TestGetAllImports()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetImportFrom(@"test\file\path");
      parser.SetImportFrom(@"test\file\path2");

      var imports = parser.GetAllImports();

      imports.Should().HaveCount(2);
      imports.All(file => Path.IsPathRooted(file)).Should().BeTrue();
      imports.Should().Contain(Path.Combine(this.tempPath, @"test\file\path"));
      imports.Should().Contain(Path.Combine(this.tempPath, @"test\file\path2"));
    }

    [Test]
    public void TestGetAllImportsRecursively()
    {
      var import1 = this.CreateSettings("");
      var import2 = this.CreateSettings("");
      var import3 = this.CreateSettings($@"<import from=""{import1}"" /><import from=""{Path.GetFileName(import2)}"" />");
      var root = this.CreateSettings($@"<import from=""{import3}"" />");
      var parser = new ConfigSettingsParser(root);

      var imports = parser.GetAllImports();

      imports.Should().HaveCount(3);
      imports.All(file => Path.IsPathRooted(file)).Should().BeTrue();
      imports.Should().Contain(import1);
      imports.Should().Contain(import2);
      imports.Should().Contain(import3);
    }

    [Test]
    public void TestRemoveImportFrom()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetImportFrom(@"test\file\path");
      parser.SetImportFrom(@"test\file\path2");

      var imports = parser.GetAllImports();
      imports.Should().HaveCount(2);
      parser.RemoveImportFrom(@"test\file\path");
      parser.RemoveImportFrom(@"test\file\path3");
      parser.GetAllImports().Should().HaveCount(1);
      
      parser.RemoveImportFrom(imports.FirstOrDefault(i => i.EndsWith(@"\path2")));
      parser.GetAllImports().Should().HaveCount(0);
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
