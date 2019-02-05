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

    [Test]
    public void CreateConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      this.WriteConfigFileContent1(fileName);

      reloaded.Should().Be(0);
      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void DeleteConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      this.WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      File.Delete(fileName);
      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.HasVariable("testVariableName").Should().Be(false);
    }

    [Test]
    public void RenameConfigFileTest()
    {
      int reloaded = 0;
      var fileName1 = this.GenerateConfigPath();
      var fileName2 = this.GenerateConfigPath();
      this.WriteConfigFileContent1(fileName1);
      this.WriteConfigFileContent2(fileName2);

      var parser = new ReloadedConfigSettingsParser(fileName1, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      File.Delete(fileName1);
      File.Move(fileName2, fileName1);
      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void ChangeConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      this.WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      this.WriteConfigFileContent2(fileName);
      reloaded.Should().Be(0);

      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void ChangeSeveralTimesConfigFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      this.WriteConfigFileContent1(fileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      this.WriteConfigFileContent2(fileName);
      System.Threading.Thread.Sleep(150);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      this.WriteConfigFileContent3(fileName);
      System.Threading.Thread.Sleep(150);
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");

      System.Threading.Thread.Sleep(300);

      reloaded.Should().Be(1);
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

      this.AddImport(fileName, importFileName);
      this.WriteConfigFileContent1(importFileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
      parser.HasVariable("testVariableName2").Should().Be(false);

      this.WriteConfigFileContent2(importFileName);

      reloaded.Should().Be(0);
      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.HasVariable("testVariableName").Should().Be(false);
      parser.GetVariableValue("testVariableName2").Should().Be("testVariableValue2");
    }

    [Test]
    public void CreateImportFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var importFileName = this.GenerateConfigPath();

      this.AddImport(fileName, importFileName);

      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));
      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      this.WriteConfigFileContent1(importFileName);

      reloaded.Should().Be(0);
      System.Threading.Thread.Sleep(300);
      reloaded.Should().Be(1);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    [Test]
    public void AddImportFileTest()
    {
      int reloaded = 0;
      var fileName = this.GenerateConfigPath();
      var parser = new ReloadedConfigSettingsParser(fileName, () => reloaded++, TimeSpan.FromMilliseconds(200));

      reloaded.Should().Be(0);
      parser.HasVariable("testVariableName").Should().Be(false);

      var importFileName = this.GenerateConfigPath();
      this.WriteConfigFileContent1(importFileName);
      this.AddImport(fileName, importFileName);
      System.Threading.Thread.Sleep(300);

      reloaded.Should().Be(1);
      parser.GetVariableValue("testVariableName").Should().Be("testVariableValue");
    }

    private string GenerateConfigPath()
    {
      return Path.Combine(this.tempPath, $@"settings_{Guid.NewGuid()}.xml");
    }

    private void WriteConfigFileContent1(string fileName)
    {
      File.WriteAllText(fileName, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName"" value=""testVariableValue"" />
</settings>");
    }

    private void WriteConfigFileContent2(string fileName)
    {
      File.WriteAllText(fileName, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName2"" value=""testVariableValue2"" />
</settings>");
    }

    private void WriteConfigFileContent3(string fileName)
    {
      File.WriteAllText(fileName, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
<var name=""testVariableName3"" value=""testVariableValue3"" />
</settings>");
    }

    private void AddImport(string fileName, string importfileName)
    {
      var parser = new ConfigSettingsParser(fileName);
      parser.SetImportFrom(importfileName);
      parser.Save();
    }
  }
}
