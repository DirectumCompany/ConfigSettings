using System;
using System.Text;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Хэлпер для обработки строк.
  /// </summary>
  public static class StringUtils
  {
    /// <summary>
    /// Вычислить MD5-хеш для строки.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns>Хеш.</returns>
    public static string GetMD5Hash(this string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");

      return Encoding.UTF8.GetBytes(value).GetMD5Hash();
    }

    /// <summary>
    /// Заменить последнее вхождение.
    /// </summary>
    /// <param name="source">Исходная строка.</param>
    /// <param name="find">Что заменить.</param>
    /// <param name="replace">На что заменить.</param>
    /// <returns>Строка с заменой.</returns>
    public static string ReplaceLastOccurrence(string source, string find, string replace)
    {
      var place = source.LastIndexOf(find, StringComparison.Ordinal);
      return place == -1 ? source : source.Remove(place, find.Length).Insert(place, replace);
    }

    /// <summary>
    /// Заменить первое вхождение.
    /// </summary>
    /// <param name="source">Исходная строка.</param>
    /// <param name="find">Что заменить.</param>
    /// <param name="replace">На что заменить.</param>
    /// <returns>Строка с заменой.</returns>
    public static string ReplaceFirstOccurrence(string source, string find, string replace)
    {
      var place = source.IndexOf(find, StringComparison.Ordinal);
      return place  == -1 ? source : $"{source.Substring(0, place)}{replace}{source.Substring(place + find.Length)}";
    }
  }
}
