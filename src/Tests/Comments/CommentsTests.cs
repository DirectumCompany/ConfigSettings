using ConfigSettings.Patch;
using ConfigSettings.Tests;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace ConfigSettingsTests
{
  [TestFixture]
  public class CommentsTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath("CommentsTests");
    private string TempConfigFilePath => Path.Combine(this.tempPath, TestContext.CurrentContext.Test.MethodName + ".xml");

    [Test]
    public void WhenSaveVariableCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "variable_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    [Test]
    public void WhenSaveBlockCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "block_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    [Test]
    public void WhenSaveDoubleVariableCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "double_variable_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    [Test]
    public void WhenSaveDoubleBlockCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "double_block_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.SetBlockValue("testBlockName", null, @"  <tenant name=""alpha"" db=""alpha_db"" />
          <tenant name=""beta"" user=""alpha_user"" />");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    [Test]
    public void WhenSaveImportCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "import_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    [Test]
    public void WhenSaveDoubleImportCommentsShouldNotDisappear()
    {
      var originPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Comments", "Etalon", "double_import_comments.xml");
      File.Copy(originPath, this.TempConfigFilePath);
      var parser = new ConfigSettingsParser(this.TempConfigFilePath);
      parser.SetVariableValue("testVariable", @" 666 ");
      parser.Save();
      IsEqualContents(originPath, this.TempConfigFilePath).Should().BeTrue();
    }

    private bool IsEqualContents(string originPath, string changedPath) 
    {
      var f1 = File.ReadAllLines(originPath);
      var f2 = File.ReadAllLines(changedPath);
      var except = f1.Except(f2).Where(s => !string.IsNullOrEmpty(s.Trim()) && s.Trim().StartsWith("<!--") && s.Trim().EndsWith("-->"));
      return except.Count() == 0;
    }
  }
}
