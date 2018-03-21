using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace ConfigSettings.Tests
{
  public class TextDiffer
  {
    private static readonly string[] DiffStatuses = { " ", "-", "+", "?", "*" };

    public static string DiffText(string left, string right)
    {
      var differ = new Differ();
      var builder = new InlineDiffBuilder(differ);
      var diff = builder.BuildDiffModel(left, right);
      var result = new List<string>();
      foreach (var line in diff.Lines.Where(line => line.Type != ChangeType.Unchanged))
        result.Add(DiffStatuses[(int)line.Type] + " " + line.Text + " ");
      return string.Join(Environment.NewLine, result);
    }

    public static string DiffFiles(string leftPath, string rightPath)
    {
      var left = File.Exists(leftPath) ? File.ReadAllText(leftPath) : string.Empty;
      var right = File.Exists(rightPath) ? File.ReadAllText(rightPath) : string.Empty;
      var res = DiffText(left, right);
      return res;
    }
  }
}
