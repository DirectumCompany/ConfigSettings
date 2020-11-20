using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using ConfigSettings.Patch;
using ConfigSettingsTests;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  public class TestTenant
  {
    [XmlAttribute]
    public string Name { get; set; }
  }

  [XmlType("test_custom_root")]
  [XmlRoot("test_custom_root")]
  public class TestTenantWithCustomRootElementName
  {
    [XmlAttribute("custom_name")]
    public string Name { get; set; }
  }

  public class TestPlugin
  {
    [XmlIgnore]
    public Dictionary<string, string> Attributes { get; private set; } = new Dictionary<string, string>();

    [XmlAnyAttribute]
    public XmlAttribute[] XmlAttributes
    {
      get
      {
        if (this.Attributes == null)
          return null;
        var doc = new XmlDocument();
        return this.Attributes.Select(p =>
        {
          var a = doc.CreateAttribute(p.Key);
          a.Value = p.Value;
          return a;
        }).ToArray();
      }
      set { this.Attributes = value?.ToDictionary(a => a.Name, a => a.Value); }
    }
  }

  public class TestNestedTenants
  {
    public List<TestTenant> Tenants { get; set; }
  }


  [TestFixture]
  public class ConfigSettingsParserTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath("ConfigSettingsParserTests");

    private string TempConfigFilePath =>
      Path.Combine(this.tempPath, TestContext.CurrentContext.Test.MethodName + ".xml");

    [Test]
    public void WhenSaveVariablesInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateVariable(parser.RootSettingsFilePath, "testVariableName", "testVariableValue");
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <var name=""testVariableName"" value=""testVariableValue"" />
");
    }

    [Test]
    public void WhenSaveMetaVariableInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateMetaVariable(parser.RootSettingsFilePath, "testMetaVariableName", "testVariableValue");
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <meta name=""testMetaVariableName"" value=""testVariableValue"" />
");
    }

    [Test]
    public void WhenSaveBlockInSingleFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "testBlockName", null,
        @"  <tenant name=""alpha"" db=""alpha_db"" />
    <tenant name=""beta"" user=""alpha_user"" />");
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should()
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
      var tempPathWithUnixistedSubfolders = Path.Combine(TestEnvironment.CreateRandomPath("ConfigSettingsParserTests"),
        "SubFolder",
        $"{nameof(this.WhenSaveImportFromInSingleFile)}.xmnl");
      var parser = new ConfigSettingsParser(tempPathWithUnixistedSubfolders);
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path");
      parser.Save();

      TestTools.GetConfigSettings(tempPathWithUnixistedSubfolders).Should()
        .Be(@"
  <import from=""test\file\path"" />
");
    }

    [Test]
    public void TestHasImportFromExistedFile()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path");
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
    public void TestGetBlockTypedArray()
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
    public void TestSetBlockTypedArray()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      var tenants = new List<TestTenant>
      {
        new TestTenant {Name = "t1"},
        new TestTenant {Name = "t2"}
      };
      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "testBlockName", true, tenants);
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <block name=""testBlockName"" enabled=""true"">
    <TestTenant Name=""t1"" />
    <TestTenant Name=""t2"" />
  </block>
");
    }

    [Test]
    public void TestSetBlockTypedArrayWithCustomElementName()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      var tenants = new List<TestTenantWithCustomRootElementName>
      {
        new TestTenantWithCustomRootElementName {Name = "t1"},
        new TestTenantWithCustomRootElementName {Name = "t2"}
      };
      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "testBlockName", true, tenants);
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should()
        .Be(@"
  <block name=""testBlockName"" enabled=""true"">
    <test_custom_root custom_name=""t1"" />
    <test_custom_root custom_name=""t2"" />
  </block>
");
    }

    [Test]
    public void TestGetBlockTypedArrayWithCustomElementName()
    {
      var settings = this.CreateSettings(@"
  <block name=""testBlockName"">
    <test_custom_root custom_name=""t1"" />
    <test_custom_root custom_name=""t2"" />
  </block>");
      var parser = new ConfigSettingsParser(settings);
      var tenants = parser.GetBlockContent<List<TestTenantWithCustomRootElementName>>("testBlockName");
      tenants.Should().HaveCount(2);
      tenants[0].Name.Should().Be("t1");
      tenants[1].Name.Should().Be("t2");
    }

    [Test]
    public void TestSetBlockTypedArrayOfArray()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);

      var tenantGroups = new List<List<TestTenant>>()
      {
        new List<TestTenant>
        {
          new TestTenant {Name = "a1"},
          new TestTenant {Name = "a2"}
        },
        new List<TestTenant>
        {
          new TestTenant {Name = "b1"},
          new TestTenant {Name = "b2"}
        },
      };

      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "tenantGroups", true, tenantGroups);
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should().Be(@"
  <block name=""tenantGroups"" enabled=""true"">
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
    public void TestSetBlockTypedNestedList()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);

      var tenantList = new List<TestTenant>
      {
        new TestTenant {Name = "a1"},
        new TestTenant {Name = "a2"}
      };

      var nestedTenantList = new List<TestNestedTenants> {new TestNestedTenants {Tenants = tenantList}};

      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "nestedTenantGroups", true, nestedTenantList);
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should().Be(@"
  <block name=""nestedTenantGroups"" enabled=""true"">
    <TestNestedTenants>
      <Tenants>
        <TestTenant Name=""a1"" />
        <TestTenant Name=""a2"" />
      </Tenants>
    </TestNestedTenants>
  </block>
");
    }

    [Test]
    public void TestSetBlockTypedArrayAsAttributes()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);

      var da1 = new TestPlugin();
      da1.Attributes.Add("id", "id_value");
      var db1 = new TestPlugin();
      db1.Attributes.Add("name", "name_value");

      parser.AddOrUpdateBlock(parser.RootSettingsFilePath, "TestPluginSettings", true, new List<TestPlugin>
      {
        da1,
        db1
      });
      parser.Save();

      TestTools.GetConfigSettings(this.TempConfigFilePath).Should().Be(@"
  <block name=""TestPluginSettings"" enabled=""true"">
    <TestPlugin id=""id_value"" />
    <TestPlugin name=""name_value"" />
  </block>
");
    }

    [Test]
    public void TestGetBlockTypedArrayOfArray()
    {
      var settings = this.CreateSettings(@"
 <block name=""tenantGroups"" enabled=""True"">
    <ArrayOfTestTenant>
      <TestTenant Name=""a1"" />
      <TestTenant Name=""a2"" />
    </ArrayOfTestTenant>
    <ArrayOfTestTenant>
      <TestTenant Name=""b1"" />
      <TestTenant Name=""b2"" />
    </ArrayOfTestTenant>
  </block>");

      var parser = new ConfigSettingsParser(settings);
      var tenantgroups = parser.GetBlockContent<List<List<TestTenant>>>("tenantGroups");
      tenantgroups.Should().HaveCount(2);
      var tenantsA = tenantgroups[0];
      tenantsA[0].Name.Should().Be("a1");
      tenantsA[1].Name.Should().Be("a2");

      var tenantsB = tenantgroups[1];
      tenantsB[0].Name.Should().Be("b1");
      tenantsB[1].Name.Should().Be("b2");
    }

    [Test]
    public void TestGetAllImports()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path");
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path2");

      var imports = parser.GetAllImports();

      imports.Should().HaveCount(2);
      imports.All(Path.IsPathRooted).Should().BeTrue();
      imports[0].Should().EndWith(@"test\file\path");
      imports[1].Should().EndWith(@"test\file\path2");
    }

    [Test]
    public void TestGetAllImportsRecursively()
    {
      var import1 = this.CreateSettings("");
      var import2 = this.CreateSettings("");
      var import3 = this.CreateSettings($@"
<import from=""{import1}"" />
<import from=""{Path.GetFileName(import2)}"" />");
      var root = this.CreateSettings($@"
<import from=""{import3}"" />");
      var parser = new ConfigSettingsParser(root);

      var imports = parser.GetAllImports();

      imports.Should().HaveCount(3);
      imports.All(Path.IsPathRooted).Should().BeTrue();
      imports[0].Should().Be(import1);
      imports[1].Should().Be(import2);
      imports[2].Should().Be(import3);
    }

    [Test]
    public void TestRemoveImportFrom()
    {
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path");
      parser.AddOrUpdateImportFrom(parser.RootSettingsFilePath, @"test\file\path2");

      var imports = parser.GetAllImports();
      imports.Should().HaveCount(2);
      parser.RemoveImportFrom(@"test\file\path");
      parser.RemoveImportFrom(@"test\file\path3");
      parser.GetAllImports().Should().HaveCount(1);

      parser.RemoveImportFrom(imports.FirstOrDefault(i => i.EndsWith(@"\path2")));
      parser.GetAllImports().Should().HaveCount(0);
    }

    [Test]
    public void WhenSetRelativeImportThenPathShouldNotBeAbsolute()
    {
      var configSettingsPath = this.CreateSettings(@"  
  <import from=""origin/import/from"" />
");

      var parser = new ConfigSettingsParser(configSettingsPath);
      parser.AddOrUpdateImportFrom("test/import/from");
      parser.Save();
      var content = TestTools.GetConfigSettings(configSettingsPath);
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
      var parser = new ConfigSettingsParser(configSettingsPath);
      parser.AddOrUpdateVariable("testName", "testValue");
      parser.Save();
      var content = TestTools.GetConfigSettings(configSettingsPath);
      content.Should().Be(@"
  <var name=""testName"" value=""testValue"" />
");
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
      var getter = new ConfigSettingsParser(configSettingsPath);
      getter.AddOrUpdateBlock("TEST_TRUE_BLOCK", true, @"
  <testRepository folderName=""base"" solutionType=""Base"" url="""" />
  <testRepository folderName=""work"" solutionType=""Work"" url="""" />");
      getter.Save();
      var content = TestTools.GetConfigSettings(configSettingsPath);
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
    public void WhenSaveNullConfigSettingsParserThenSaveShouldNotWork()
    {
      var parser = new ConfigSettingsParser(null);
      parser.AddOrUpdateVariable("NEW_VALUE", "true");
      var exception = ((MethodThatThrows) delegate { parser.Save(); }).GetException();
      exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public void WhenSetValueWithNullConfigSettingsParserThenExceptionShouldBeRaisen()
    {
      var parser = new ConfigSettingsParser(null);
      parser.GetVariable(null, "NEW_VALUE").Should().BeNull();
      parser.AddOrUpdateVariable("NEW_VALUE", "true");
      parser.GetVariable(null, "NEW_VALUE").Value.Should().Be("true");
    }

    private string CreateSettings(string settings)
    {
      return TestTools.CreateSettings(settings, this.tempPath);
    }
  }
}