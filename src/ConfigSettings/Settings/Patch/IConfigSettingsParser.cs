using System.Collections.Generic;

namespace ConfigSettings.Patch
{
  public interface IConfigSettingsParser
  {
    IReadOnlyList<string> GetAllImports();
  }
}