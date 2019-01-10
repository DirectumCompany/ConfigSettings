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
  }
}
