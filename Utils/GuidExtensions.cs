using System;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Расширения для Guid.
  /// </summary>
  public static class GuidExtensions
  {
    /// <summary>
    /// Получить первые 8 символов у идентификатора.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <returns>Первые 8 символов.</returns>
    public static string ToShortString(this Guid id)
    {
      return id.ToString().Substring(0, 8);
    }
  }
}
