using System.IO;
using ConfigSettings.Patch;
using ConfigSettings.Tests;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettingsTests
{
  /// <summary>
  /// Добавление/изменение/сохранение настроек/блоков/импортов при использовании парсера
  /// </summary>
  [TestFixture]
  public class UpdateValuesParserTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath(nameof(UpdateValuesParserTests));
    
    [Test]
    public void VariableWithLastValuesShouldStillExistsAfterSave()
    {
      var baseSettings = this.CreateSettings(@"
<var name=""n1"" value=""vb1"" />
<var name=""n1"" value=""vb2"" />
");

      var rootSettings = this.CreateSettings($@"
<import from=""{baseSettings}"" />
<var name=""n1"" value=""v1"" />
<var name=""n1"" value=""v2"" />
");
      var parser = new ConfigSettingsParser(rootSettings);
      parser.Save();

      TestTools.GetConfigSettings(baseSettings).Should().Be(@"
  <var name=""n1"" value=""vb2"" />
"); 
      
      TestTools.GetConfigSettings(rootSettings).Should().Be($@"
  <import from=""{baseSettings}"" />
  <var name=""n1"" value=""v2"" />
");
    }

    [Test]
    public void RemoveAllVariables()
    {
      var baseSettings = this.CreateSettings(@"
<var name=""n1"" value=""vb1"" />
");

      var rootSettings = this.CreateSettings($@"
<import from=""{baseSettings}"" />
<var name=""n1"" value=""v1"" />
");
      var parser = new ConfigSettingsParser(rootSettings);
      parser.RemoveAllVariables("n1");
      parser.Save();
      
      TestTools.GetConfigSettings(baseSettings).Should().Be(@""); 
      
      TestTools.GetConfigSettings(rootSettings).Should().Be($@"
  <import from=""{baseSettings}"" />
");
    }   
    
    [Test]
    public void RemoveRootVariable()
    {
      var baseSettings = this.CreateSettings(@"
<var name=""n1"" value=""vb1"" />
");

      var rootSettings = this.CreateSettings($@"
<import from=""{baseSettings}"" />
<var name=""n1"" value=""v1"" />
");
      var parser = new ConfigSettingsParser(rootSettings);
      parser.RemoveVariable("n1");
      parser.Save();
      
      TestTools.GetConfigSettings(baseSettings).Should().Be(@"
  <var name=""n1"" value=""vb1"" />
"); 
      
      TestTools.GetConfigSettings(rootSettings).Should().Be($@"
  <import from=""{baseSettings}"" />
");
    }    
    
    [Test]
    public void RemoveBaseVariable()
    {
      var baseSettings = this.CreateSettings(@"
<var name=""n1"" value=""vb1"" />
");

      var rootSettings = this.CreateSettings($@"
<import from=""{baseSettings}"" />
<var name=""n1"" value=""v1"" />
");
      var parser = new ConfigSettingsParser(rootSettings);
      parser.RemoveVariable("n1", baseSettings);
      parser.Save();
      
      TestTools.GetConfigSettings(baseSettings).Should().Be(@""); 
      
      TestTools.GetConfigSettings(rootSettings).Should().Be($@"
  <import from=""{baseSettings}"" />
  <var name=""n1"" value=""v1"" />
");
    }

    [Test]
    public void AddBaseConfigWithVariable()
    {
      var rootPath = Path.Combine(this.tempPath, nameof(this.AddBaseConfigWithVariable), "rootConfig.xml");
      var basePath = Path.Combine(this.tempPath, nameof(this.AddBaseConfigWithVariable), "baseConfig.xml");
      var parser = new ConfigSettingsParser(rootPath);
      parser.AddOrUpdateImortFrom(rootPath, basePath);
      parser.AddOrUpdateVariable(basePath, "n1", "v1");
      parser.Save();

      TestTools.GetConfigSettings(basePath).Should().Be(@"
  <var name=""n1"" value=""v1"" />
");
    }
    
    private string CreateSettings(string settings)
    {
      return TestTools.CreateSettings(settings, this.tempPath);
    }
  }
}