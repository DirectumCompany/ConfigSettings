using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ConfigSettings.Utils;
using NUnit.Framework;

namespace ConfigSettings.Tests
{
  [SetUpFixture]
  public class TestEnvironment
  {
    private static string OutputPath => TestContext.CurrentContext.TestDirectory;

    public static string TempOutputPath => Path.Combine(OutputPath, "_Tmp");

    [OneTimeSetUp]
    public static void SetUp()
    {
      CleanTempOutputPath();
      Console.SetOut(TextWriter.Null);
    }

    public static void CleanTempOutputPath()
    {
      if (Directory.Exists(TempOutputPath))
      {
        KillAllProcessesInTempPath();
        DirectoryUtils.TryDeleteDirectory(TempOutputPath);
      }

      Directory.CreateDirectory(TempOutputPath);
    }

    public static void KillAllProcessesInTempPath()
    {
      var processes = GetLiveProcessesInTempOutputPath(Process.GetProcesses());
      foreach (var process in processes)
      {
        process.Kill();
        process.WaitForExit();
      }
    }

    public static IEnumerable<Process> GetLiveProcessesInTempOutputPath(IEnumerable<Process> processes)
    {
      return processes.Where(p =>
      {
        try
        {
          return !p.HasExited && p.MainModule.FileName.StartsWith(TempOutputPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Win32Exception)
        {
          return false;
        }
      }).ToList();
    }

    public static string CreateRandomPath(string dirPrefix)
    {
      var tempPath = Path.Combine(TempOutputPath, $"{dirPrefix}_{Guid.NewGuid().ToShortString()}");
      Directory.CreateDirectory(tempPath);
      return tempPath;
    }
  }
}
