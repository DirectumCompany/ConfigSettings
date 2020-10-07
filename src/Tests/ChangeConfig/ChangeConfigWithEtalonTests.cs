using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigSettings.Utils;
using FluentAssertions;
using NUnit.Framework;
using TestStack.BDDfy;

namespace ConfigSettings.Tests
{
  [TestFixture]
  public class ChangeConfigWithEtalonTests
  {
    private string liveConfigPath;
    private DateTime liveConfigDateTime;

    private readonly string tempPath = TestEnvironment.CreateRandomPath("ChangeConfigWithEtalon");

    [Test]
    public void ApplyConfigSettings()
    {
      this.When(_ => _.ApplySettingsToСonfig())
        .Then(_ => _.LiveConfigShouldExists())
          .And(_ => _.LiveConfigShouldBeEqualToEtalon())
        .BDDfy();
    }

    [Test]
    public void ApplyConfigSettingsWithGeneratedBlock()
    {
      this.When(_ => _.ApplySettingsToWithGeneratedBlock())
        .Then(_ => _.LiveConfigShouldExists())
          .And(_ => _.LiveConfigShouldNotBeChanged())
        .BDDfy();
    }


    [Test]
    public void IsFileLockedTest()
    {
      this.When(_ => _.ParralelIsFileLocked())
        .BDDfy();
    }

    [Test]
    public void FindPathByMaskTest()
    {
      this.When(_ => _.FindPathByMask())
        .BDDfy();
    }

    [Test]
    public void ApplyLogSettings()
    {
      var currentConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "source_log.config");
      this.When(_ => _.ApplySettingsToLogСonfig(currentConfigPath))
        .Then(_ => _.ConfigShouldContainsFullPath())
        .And(_ => _.ConfigFullPathShouldNotContainsAppdata(currentConfigPath))
        .BDDfy();
    }

    [Test]
    public void ApplyConfigSettingsAppDataPath()
    {
      var currentConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "source_log.config");
      this.When(_ => _.ApplySettingsToLogСonfigWithAppDataPath(currentConfigPath))
        .Then(_ => _.ConfigFullPathShouldContainsAppdata(currentConfigPath))
        .BDDfy();
    }

    [Test]
    public void ApplyConfigSettingsAppDataPathWithoutResolveFunc()
    {
      var currentConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "source_log.config");
      Assert.Throws<ArgumentNullException>(() => ApplySettingsToLogСonfigWithAppDataPathWithoutResolvePathFunc(currentConfigPath));
    }

    private void ConfigShouldContainsFullPath()
    {
      var existingPathPart = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(this.liveConfigPath)));
      File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "source_log.config")).Contains(existingPathPart).Should().BeFalse();
      File.ReadAllText(this.liveConfigPath).Contains(existingPathPart).Should().BeTrue();
    }

    private void ConfigFullPathShouldContainsAppdata(string currentConfigPath)
    {
      this.liveConfigPath.Contains(ResolveForcedAppDataPath(currentConfigPath)).Should().BeTrue();
    }

    private void ConfigFullPathShouldNotContainsAppdata(string currentConfigPath)
    {
      this.liveConfigPath.Contains(ResolveForcedAppDataPath(currentConfigPath)).Should().BeFalse();
    }


    private void FindPathByMask()
    {
      var fn = Guid.NewGuid().ToString();
      ChangeConfig.FindFirstPathByMask(this.tempPath, fn).Should().BeNull();
      File.WriteAllText(Path.Combine(this.tempPath, "prefix_" + fn), "some content");
      ChangeConfig.FindFirstPathByMask(this.tempPath, fn).Should().EndWith(fn);
    }

    private void ParralelIsFileLocked()
    {
      var fn = Path.Combine(this.tempPath, Guid.NewGuid().ToString());
      var r = Enumerable.Range(0, 100);
      Parallel.ForEach(r, i =>
      {
        ChangeConfig.IsFileLocked(fn).Should().BeFalse();
      });

      File.WriteAllText(fn, @"content");
      Parallel.ForEach(r, i =>
      {
        ChangeConfig.IsFileLocked(fn).Should().BeFalse();
        File.ReadAllText(fn);
      });
    }


    private void LiveConfigShouldNotBeChanged()
    {
      this.liveConfigDateTime.Should().Be(File.GetLastWriteTime(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "with_generated_section.live.config")));
    }

    private void ApplySettingsToСonfig()
    {
      this.liveConfigPath = ChangeConfig.Execute(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "source.config"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "settings.xml"));
    }

    private void ApplySettingsToWithGeneratedBlock()
    {
      this.liveConfigDateTime = File.GetLastWriteTime(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "with_generated_section.live.config"));
      this.liveConfigPath = ChangeConfig.Execute(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "with_generated_section.config"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "settings.xml"));
    }

    private void LiveConfigShouldExists()
    {
      File.Exists(this.liveConfigPath).Should().BeTrue();
    }

    private void LiveConfigShouldBeEqualToEtalon()
    {
      var diff = TextDiffer.DiffFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "target.config"), this.liveConfigPath);
      if (!string.IsNullOrEmpty(diff))
        diff = (Environment.NewLine + diff + Environment.NewLine).Replace("\n", string.Empty);
      diff.Should().BeEmpty();
    }

    private void ApplySettingsToLogСonfig(string currentConfigPath)
    {
      this.liveConfigPath = ChangeConfig.Execute(currentConfigPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "settings.xml"), ResolveForcedAppDataPath);
    }

    private void ApplySettingsToLogСonfigWithAppDataPath(string currentConfigPath)
    {
      this.liveConfigPath = ChangeConfig.Execute(currentConfigPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "settings_with_appdata_path.xml"), ResolveForcedAppDataPath);
    }

    private void ApplySettingsToLogСonfigWithAppDataPathWithoutResolvePathFunc(string currentConfigPath)
    {
      this.liveConfigPath = ChangeConfig.Execute(currentConfigPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChangeConfig", "Etalon", "settings_with_appdata_path.xml"));
    }

    private static string ResolveForcedAppDataPath(string currentConfigPath)
    {
      return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigSettings",
        "Configs",
        Path.GetDirectoryName(currentConfigPath).GetMD5Hash().Substring(0, 8));
    }
  }
}
