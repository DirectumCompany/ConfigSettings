using ConfigSettings.Patch;
using ConfigSettings.Tests;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using ConfigSettings.Utils;
using System.Collections.Generic;

namespace ConfigSettingsTests
{
  [TestFixture]
  public class CommentsTests
  {
    private readonly string tempPath = TestEnvironment.CreateRandomPath("CommentsTests");

    [Test]
    public void WhenSaveVariableCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() { 
        "<!--VARIABLE comment -->", 
        @"<var name=""VARIABLE"" value=""all"" />" };
      SaveInFileAndCheck(linesToAdd);
    }

    [Test]
    public void WhenSaveDoubleVariableCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() {
        "<!--VARIABLE comment 1 -->",
        "<!--VARIABLE comment 2 -->",
        @"<var name=""VARIABLE"" value=""all"" />" };
      SaveInFileAndCheck(linesToAdd);
    }

    [Test]
    public void WhenSaveBlockCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() {
        "<!--TENANTS comment-->",
        @"<block name=""TENANTS"">",
        "<!--alpha comment-->",
        @"<tenant name=""alpha"" db=""alpha_db"" />",
        "<!--beta comment-->",
        @"<tenant name=""beta"" user=""alpha_user"" />",
        "</block>" };
      SaveInFileAndCheck(linesToAdd);
    }


    [Test]
    public void WhenSaveDoubleBlockCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() {
        "<!--TENANTS comment 1-->",
        "<!--TENANTS comment 2-->",
        @"<block name=""TENANTS"">",
        "<!--alpha comment 1-->",
        "<!--alpha comment 2-->",
        @"<tenant name=""alpha"" db=""alpha_db"" />",
        "<!--beta comment 1-->",
        "<!--beta comment 2-->",
        @"<tenant name=""beta"" user=""alpha_user"" />",
        "</block>" };
      SaveInFileAndCheck(linesToAdd);
    }

    [Test]
    public void WhenSaveImportCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() {
          "<!--Import comment-->",
        @"<import from=""_imported_config.xml"" />" };
      SaveInFileAndCheck(linesToAdd);
    }

    [Test]
    public void WhenSaveDoubleImportCommentsShouldNotDisappear()
    {
      var linesToAdd = new List<string>() {
        "<!--Import comment 1-->",
        "<!--Import comment 2-->",
        @"<import from=""_imported_config.xml"" />" };
      SaveInFileAndCheck(linesToAdd);
    }

    private bool IsFileContainLines(string path, List<string> lines)
    {
      var fileLines = File.ReadAllLines(path).Select(l => l.Trim());
      var intersect = fileLines.Intersect(lines, StringComparer.OrdinalIgnoreCase);
      return lines.SequenceEqual(intersect);
    }

    private void SaveInFileAndCheck(List<string> linesToAdd)
    {
      var tempFile = this.CreateSettings(string.Join("\n", linesToAdd));
      var parser = new ConfigSettingsParser(tempFile);
      parser.AddOrUpdateVariable(parser.RootSettingsFilePath, "testVariable", @" 666 ");
      parser.Save();
      IsFileContainLines(tempFile, linesToAdd).Should().BeTrue();
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
