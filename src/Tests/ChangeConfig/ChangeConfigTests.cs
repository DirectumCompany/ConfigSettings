using System;
using System.IO;
using ConfigSettings.Utils;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  [TestFixture]
  public class ChangeConfigTests
  {
    private string liveConfigPath;

    private readonly string tempPath = TestEnvironment.CreateRandomPath("ChangeConfig");
    private readonly string tempImportedPath = TestEnvironment.CreateRandomPath("ChangeConfig_ImportedSetting");

    [Test]
    public void UncommentBlock()
    {
      var settingsPath = this.CreateSettings(@"
  <block name='IS_TEST_ENABLED_BLOCK' enabled='true' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{~IS_TEST_ENABLED_BLOCK}-->
  <!--<SomeBlock/>-->");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);

      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{~IS_TEST_ENABLED_BLOCK}-->
  <SomeBlock />
");
    }

    [Test]
    public void CommentBlockNegative()
    {
      var settingsPath = this.CreateSettings(@"
  <block name='IS_TEST_ENABLED_BLOCK' enabled='true' />
  <block name='IS_TEST_DISABLED_BLOCK' enabled='false' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{~!IS_TEST_ENABLED_BLOCK}-->
  <EnabledBlock />  
  <!--{~!IS_TEST_DISABLED_BLOCK}-->
  <DisabledBlock />");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);

      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{~!IS_TEST_ENABLED_BLOCK}-->
  <!--<EnabledBlock />-->
  <!--{~!IS_TEST_DISABLED_BLOCK}-->
  <DisabledBlock />
");
    }

    [Test]
    public void TryUncommentBlockWhenVarIsEquals()
    {
      var settingsPath = this.CreateSettings(@"
  <var name='DATABASE_ENGINE' value='postgres' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{~$equals(DATABASE_ENGINE, ""mssql"")}-->
  <!--<MssqlBlock />-->
  <!--{~$equals(DATABASE_ENGINE, ""postgres"")}-->
  <!--<PostgresBlock />-->
");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{~$equals(DATABASE_ENGINE, ""mssql"")}-->
  <!--<MssqlBlock />-->
  <!--{~$equals(DATABASE_ENGINE, ""postgres"")}-->
  <PostgresBlock />
");
    }

    [Test]
    public void TryUncommentBlockWhenBlockIsNotEquals()
    {
      var settingsPath = this.CreateSettings(@"
  <block name='IS_TEST_ENABLED_BLOCK' enabled='true' />
  <block name='IS_TEST_DISABLED_BLOCK' enabled='false' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{~$not(IS_TEST_ENABLED_BLOCK)}-->
  <!--<TestEnabledBlock />--> 
  <!--{~$not(IS_TEST_DISABLED_BLOCK)}-->
  <!--<TestDisabledBlock />--> 
");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{~$not(IS_TEST_ENABLED_BLOCK)}-->
  <!--<TestEnabledBlock />-->
  <!--{~$not(IS_TEST_DISABLED_BLOCK)}-->
  <TestDisabledBlock />
");
    }

    [Test]
    public void TryUncommentBlockWhenNegateEquals()
    {
      var settingsPath = this.CreateSettings(@"
  <var name='DATABASE_ENGINE' value='postgres' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{~!$equals(DATABASE_ENGINE, ""mssql"")}-->
  <!--<TestDisabledBlock />--> 
  <!--{~!$equals(DATABASE_ENGINE, ""postgres"")}-->
  <!--<TestEnabledBlock />--> 
");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{~!$equals(DATABASE_ENGINE, ""mssql"")}-->
  <TestDisabledBlock />
  <!--{~!$equals(DATABASE_ENGINE, ""postgres"")}-->
  <!--<TestEnabledBlock />-->
");
    }

    [Test]
    public void UncommentBlockAndChangeAttributeValue()
    {
      var settingsPath = this.CreateSettings(@"
  <var name='DATABASE_ENGINE' value='postgres' />");

      var enabledBlockConfigPath = this.CreateConfig(@"
  <!--{@attrName=""ChangedValue""}-->
  <!--{~$equals(DATABASE_ENGINE, ""postgres"")}-->
  <!--<PostgresBlock attrName='AttrValue'/>-->
");

      this.liveConfigPath = ChangeConfig.Execute(enabledBlockConfigPath, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@attrName=""ChangedValue""}-->
  <!--{~$equals(DATABASE_ENGINE, ""postgres"")}-->
  <PostgresBlock attrName=""ChangedValue"" />
");
    }

    [Test]
    public void NewLinesInAttributes()
    {
      var settingsPath = this.CreateSettings(@"
  <var name='STORAGE_SERVICE_ZIP_PATH' value='D:\Temp\StorageService.zip' />
  <var name='STORAGE_SERVICE_BIN_PATH' value='D:\Temp\StorageService\bin\Storage' />");

      var attributesWithoutNewlines = this.CreateConfig(@"
  <!--{@storageServicePort=STORAGE_SERVICE_PORT @storageServiceZipPath=STORAGE_SERVICE_ZIP_PATH @storageServiceBinPath=STORAGE_SERVICE_BIN_PATH}-->
  <storageServiceSetting />
");

      this.liveConfigPath = ChangeConfig.Execute(attributesWithoutNewlines, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@storageServicePort=STORAGE_SERVICE_PORT @storageServiceZipPath=STORAGE_SERVICE_ZIP_PATH @storageServiceBinPath=STORAGE_SERVICE_BIN_PATH}-->
  <storageServiceSetting storageServiceZipPath=""D:\Temp\StorageService.zip"" storageServiceBinPath=""D:\Temp\StorageService\bin\Storage"" />
");

      var attributesWithNewlines = this.CreateConfig(@"
  <!--{@storageServicePort=STORAGE_SERVICE_PORT 
       @storageServiceZipPath=STORAGE_SERVICE_ZIP_PATH 
       @storageServiceBinPath=STORAGE_SERVICE_BIN_PATH}-->
  <storageServiceSetting />
");
      this.liveConfigPath = ChangeConfig.Execute(attributesWithNewlines, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@storageServicePort=STORAGE_SERVICE_PORT 
       @storageServiceZipPath=STORAGE_SERVICE_ZIP_PATH 
       @storageServiceBinPath=STORAGE_SERVICE_BIN_PATH}-->
  <storageServiceSetting storageServiceZipPath=""D:\Temp\StorageService.zip"" storageServiceBinPath=""D:\Temp\StorageService\bin\Storage"" />
");
    }

    [Test]
    public void ImportFromAbsolutePath()
    {
      var subSettingsPath = this.CreateSettings(@"
  <var name='IMPORTED' value='From subSettingsPath' />");
      var settingsPath = this.CreateSettings($@"
  <var name='ORIGIN' value='From Origin' />
  <import from='{subSettingsPath}'/>");

      var configWithImportSection = this.CreateConfig(@"
  <!--{@origin=ORIGIN @imported=IMPORTED}-->
  <setting />
");

      this.liveConfigPath = ChangeConfig.Execute(configWithImportSection, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@origin=ORIGIN @imported=IMPORTED}-->
  <setting origin=""From Origin"" imported=""From subSettingsPath"" />
");
    }

    [Test]
    public void ImportFromRelativePath()
    {
      var subSettingsPath = this.CreateSettings(@"
  <var name='IMPORTED' value='From subSettingsPath' />");
      var subSettingsFileName = Path.GetFileName(subSettingsPath);
      File.Move(subSettingsPath, Path.Combine(this.tempImportedPath, subSettingsFileName));
      var lastDirName = Path.GetFileName(this.tempImportedPath);
      var relativePath = $@"..\{Path.Combine(lastDirName, subSettingsFileName)}";
      var settingsPath = this.CreateSettings($@"
  <var name='ORIGIN' value='From Origin' />
  <import from='{relativePath}'/>");

      var configWithImportSection = this.CreateConfig(@"
  <!--{@origin=ORIGIN @imported=IMPORTED}-->
  <setting />
");

      this.liveConfigPath = ChangeConfig.Execute(configWithImportSection, settingsPath);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@origin=ORIGIN @imported=IMPORTED}-->
  <setting origin=""From Origin"" imported=""From subSettingsPath"" />
");
    }

    [Test]
    public void RootSettingsShouldOverrideImportedWhenImportedIsLast()
    {
      var lvlTwo = this.CreateSettings(@"
  <var name='ORIGIN' value='lvlTwo' />
  <var name='IMPORTED' value='lvlTwo' />
  <var name='IMPORTED_LVL2' value='lvlTwo' />");

      var lvlOne = this.CreateSettings($@"
  <var name='ORIGIN' value='lvlOne' />
  <var name='IMPORTED' value='lvlOne' />
  <import from='{lvlTwo}' />");

      var root = this.CreateSettings($@"
  <var name='ORIGIN' value='root' />
  <import from='{lvlOne}' />");

      var configWithImportSection = this.CreateConfig(@"
  <!--{@origin=ORIGIN @imported=IMPORTED @imported_v2=IMPORTED_LVL2}-->
  <setting />
");
      this.liveConfigPath = ChangeConfig.Execute(configWithImportSection, root);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@origin=ORIGIN @imported=IMPORTED @imported_v2=IMPORTED_LVL2}-->
  <setting origin=""root"" imported=""lvlOne"" imported_v2=""lvlTwo"" />
");
    }

    [Test]
    public void RootSettingsShouldOverrideImportedWhenImportedIsFirst()
    {
      var lvlTwo = this.CreateSettings(@"
  <var name='ORIGIN' value='lvlTwo' />
  <var name='IMPORTED' value='lvlTwo' />
  <var name='IMPORTED_LVL2' value='lvlTwo' />");

      var lvlOne = this.CreateSettings($@"
  <import from='{lvlTwo}' />
  <var name='ORIGIN' value='lvlOne' />
  <var name='IMPORTED' value='lvlOne' />");

      var root = this.CreateSettings($@"
  <import from='{lvlOne}' />
  <var name='ORIGIN' value='root' />");

      var configWithImportSection = this.CreateConfig(@"
  <!--{@origin=ORIGIN @imported=IMPORTED @imported_v2=IMPORTED_LVL2}-->
  <setting />
");
      this.liveConfigPath = ChangeConfig.Execute(configWithImportSection, root);
      this.GetLiveConfig(this.liveConfigPath).Should().Be(@"
  <!--{@origin=ORIGIN @imported=IMPORTED @imported_v2=IMPORTED_LVL2}-->
  <setting origin=""root"" imported=""lvlOne"" imported_v2=""lvlTwo"" />
");
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

    private string CreateConfig(string config)
    {
      var content = $@"<?xml version='1.0' encoding='utf-8'?>
<configuration>
{config}
</configuration>";
      var fileName = Path.Combine(this.tempPath, $@"test_config_{Guid.NewGuid().ToShortString()}.config");
      File.WriteAllText(fileName, content);
      return fileName;
    }

    public string GetLiveConfig(string configPath)
    {
      var content = File.ReadAllText(configPath);
      return content.Replace(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>", string.Empty).Replace("</configuration>", string.Empty);
    }
  }
}
