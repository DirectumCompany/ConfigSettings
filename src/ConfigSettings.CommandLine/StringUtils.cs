using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConfigSettings.CommandLine
{
  public static class StringUtils
  {
    public static string ToKebabCase(this string source)
    {
      return string.IsNullOrWhiteSpace(source)
        ? source
        : string.Concat(source.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString()))
          .ToLower(CultureInfo.InvariantCulture);
    }
  }
}
