using System;
using System.IO;
using ConfigSettings.Patch;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  [TestFixture]
  public class ReloadedConfigSettingsParserTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath(nameof(ReloadedConfigSettingsParserTests));

    private static readonly TimeSpan reloadedTime = TimeSpan.FromMilliseconds(70);
    private static readonly TimeSpan waitForReloadedTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan waitLessForReloadedTime = TimeSpan.FromMilliseconds(50);

    [Test]
    public void CreateConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      WriteConfigFileContent1(fileName);

      reloaded.Should().Be(0);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void CreateFolderWithConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath($"subfolder_{Guid.NewGuid()}");
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      var directory = Path.GetDirectoryName(fileName);
      Directory.CreateDirectory(directory);
      WriteConfigFileContent1(fileName);

      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void DeleteConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      File.Delete(fileName);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
    }

    [Test]
    public void DeleteFolderWithConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath($"subfolder_{Guid.NewGuid()}");
      var directory = Path.GetDirectoryName(fileName);
      Directory.CreateDirectory(directory);
      WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      Directory.Delete(directory, true);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
    }

    [Test]
    public void DeleteFolderWithConfigFileThenCreateTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath($"subfolder_{Guid.NewGuid()}");
      var directory = Path.GetDirectoryName(fileName);
      Directory.CreateDirectory(directory);
      WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      Directory.Delete(directory, true);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);

      Directory.CreateDirectory(directory);
      WriteConfigFileContent1(fileName);
      Assert.That(() => reloaded, Is.EqualTo(2).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void RenameConfigFileTest()
    {
      int reloaded = 0;
      var fileName1 = this.GenerateConfigPath();
      var fileName2 = this.GenerateConfigPath();
      WriteConfigFileContent1(fileName1);
      WriteConfigFileContent2(fileName2);

      var parser = new ReloadedConfigSettingsParser(fileName1, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      File.Delete(fileName1);
      File.Move(fileName2, fileName1);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void ChangeConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      WriteConfigFileContent2(fileName);
      reloaded.Should().Be(0);

      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void ChangeSeveralTimesConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      WriteConfigFileContent2(fileName);
      System.Threading.Thread.Sleep(waitLessForReloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      WriteConfigFileContent3(fileName);
      System.Threading.Thread.Sleep(waitLessForReloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.HasVariable("testVariableName2").Should().Be(false);
      parser.GetVariableValue("testVariableName3").Should().Be("testVariableValue3");
    }

    [Test]
    public void ChangeImportFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var importFileName = this.GenerateConfigPath();

      AddImport(fileName, importFileName);
      WriteConfigFileContent1(importFileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
      parser.HasVariable("testVariableName2").Should().Be(false);

      WriteConfigFileContent2(importFileName);

      reloaded.Should().Be(0);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void CreateImportFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var importFileName = this.GenerateConfigPath();

      AddImport(fileName, importFileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      WriteConfigFileContent1(importFileName);

      reloaded.Should().Be(0);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void AddImportFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);

      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      var importFileName = this.GenerateConfigPath();
      WriteConfigFileContent1(importFileName);
      AddImport(fileName, importFileName);

      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void UnexistedConfigSettingsPathTest()
    {
      int reloaded = 0;
      var _ = new ReloadedConfigSettingsParser(Constants.UnexistedConfigSettingsPath, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
    }

    [Test]
    public void RelativePathTest()
    {
      int reloaded = 0;
      var fileName = "_config.xml";
      WriteConfigFileContent1(fileName);
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, null, reloadedTime);
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName2").Should().Be(false);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      WriteConfigFileContent2(fileName);
      Assert.That(() => reloaded, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void IncorrectConfigSettings()
    {
      int occuredError = 0;
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      WriteCorrectConfigFileContent(fileName);
      var _ = new ReloadedConfigSettingsParser(fileName, () => reloaded++, (ex) => occuredError++, reloadedTime);
      occuredError.Should().Be(0);
      WriteBrokenConfigFileContent(fileName);
      Assert.That(() => occuredError, Is.EqualTo(1).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      WriteCorrectConfigFileContent(fileName);
      Assert.That(() => reloaded, Is.EqualTo(2).After((int)waitForReloadedTime.TotalMilliseconds).PollEvery(10));
      occuredError.Should().Be(1);
    }

    private string GenerateConfigPath()
    {
      return this.GenerateConfigPath(string.Empty);
    }

    private string GenerateConfigPath(string subfolder)
    {
      return Path.Combine(this.tempPath, subfolder, $@"settings_{Guid.NewGuid()}.xml");
    }

    private static void WriteConfigFileContent1(string fileName)
    {
      File.WriteAllText(fileName, @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName"" value=""testVariableValue"" />
</settings>");
    }

    private static void WriteConfigFileContent2(string fileName)
    {
      File.WriteAllText(fileName, @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName2"" value=""testVariableValue2"" />
</settings>");
    }

    private static void WriteConfigFileContent3(string fileName)
    {
      File.WriteAllText(fileName, @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName3"" value=""testVariableValue3"" />
</settings>");
    }

    private static void WriteCorrectConfigFileContent(string fileName)
    {
      File.WriteAllText(fileName, @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""SERVICE_RUNNER_PORT"" value=""10001"" />
  <block name = ""SERVICES"">
    <ServiceSetting Name=""JobScheduler"" Config=""JobScheduler_ConfigSettings.xml"" Package=""JobScheduler.zip"" />
  </block>
</settings>");
    }

    private static void WriteBrokenConfigFileContent(string fileName)
    {
      File.WriteAllText(fileName, @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""SERVICE_RUNNER_PORT"" value=""10001"" />
  <block name = ""SERVICES"">
    <ServiceSetting Name=""!№;%()`~_+!@#$%^&()_+-=][';.,,"" Config=""JobScheduler_ConfigSettings.xml"" Package=""JobScheduler.zip"" />
  </block>
</settings>");
    }

    private static void AddImport(string fileName, string importfileName)
    {
      var parser = new ConfigSettingsParser(fileName);
      parser.AddOrUpdateImortFrom(parser.RootSettingsFilePath, importfileName);
      parser.Save();
    }
  }
}
