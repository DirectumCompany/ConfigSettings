using System.Linq;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Расширения для Linq.
  /// </summary>
  public static class LinqExtensions
  {
    /// <summary>
    /// Проверить два массива на эквивалентность содержимого с учетом того, что коллекции могут быть Null.
    /// </summary>
    /// <typeparam name="T">Тип элементов массива.</typeparam>
    /// <param name="first">Первая массив.</param>
    /// <param name="second">Вторая массив.</param>
    /// <returns>True, если содержимое массивов эквивалетно или они оба null. Иначе - false.</returns>
    public static bool SafeSequenceEqual<T>(this T[] first, T[] second)
    {
      if (ReferenceEquals(first, second))
        return true;
      if (first == null && second != null)
        return false;
      if (first != null && second == null)
        return false;
      return first.SequenceEqual(second);
    }    
  }
}
