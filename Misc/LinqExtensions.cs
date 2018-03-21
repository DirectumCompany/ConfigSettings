using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CommonLibrary
{
  /// <summary>
  /// Расширения для Linq.
  /// </summary>
  public static class LinqExtensions
  {
    /// <summary>
    /// Добавить в коллекцию перечень элементов.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="collection">Коллекция.</param>
    /// <param name="source">Перечень элементов, которые необходимо добавить.</param>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
    {
      if (source == null)
        throw new ArgumentNullException("source");

      foreach (T item in source)
        collection.Add(item);
    }

    /// <summary>
    /// Добавить в словарь несколько элементов.
    /// </summary>
    /// <typeparam name="TKey">Тип ключей словаря.</typeparam>
    /// <typeparam name="TValue">Тип значений словаря.</typeparam>
    /// <param name="dictionary">Словарь.</param>
    /// <param name="source">Коллекция пар, которые нужно добавить в словарь.</param>
    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
      if (source == null)
        throw new ArgumentNullException("source");

      foreach (var item in source)
        dictionary[item.Key] = item.Value;
    }

    /// <summary>
    /// Проверить два перечисления на эквивалентность содержимого с учетом того, что коллекции могут быть Null.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="first">Первая коллекция.</param>
    /// <param name="second">Вторя коллекция.</param>
    /// <returns>True, если содержимое коллекций эквивалетно или они обе null. Иначе - false.</returns>
    public static bool SafeSequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
      if (object.ReferenceEquals(first, second))
        return true;
      if (first == null && second != null)
        return false;
      if (first != null && second == null)
        return false;
      return first.SequenceEqual(second);
    }

    /// <summary>
    /// Проверить два перечисления на эквивалентность содержимого с учетом того, что коллекции могут быть Null.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="first">Первая коллекция.</param>
    /// <param name="second">Вторя коллекция.</param>
    /// <param name="comparer">Сравнивальщик элементов.</param>
    /// <returns>True, если содержимое коллекций эквивалетно или они обе null. Иначе - false.</returns>
    public static bool SafeSequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer)
    {
      if (object.ReferenceEquals(first, second))
        return true;
      if (first == null || second == null)
        return false;
      return first.SequenceEqual(second, comparer);
    }

    /// <summary>
    /// Проверить два перечисления на эквивалентность содержимого с учетом того, что коллекции могут быть Null.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="first">Первая коллекция.</param>
    /// <param name="second">Вторя коллекция.</param>
    /// <returns>True, если содержимое коллекций эквивалетно или они обе null. Иначе - false.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Positionally",
      Justification = "Позиционно - корректный термин.")]
    public static bool SequencePositionallyEquals<T>(this IList<T> first, IList<T> second)
    {
      if (object.ReferenceEquals(first, second))
        return true;
      if (first == null && second != null)
        return false;
      if (first != null && second == null)
        return false;
      if (first.Count != second.Count)
        return false;
      for (int i = 0; i < first.Count; i++)
        if (!object.ReferenceEquals(first[i], second[i]))
          return false;
      return true;
    }

    /// <summary>
    /// Проверить два перечисления на эквивалентность с точным соблюдением одинакового порядка элемента в перечислениях.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <param name="first">Первая коллекция.</param>
    /// <param name="second">Вторая коллекция.</param>
    /// <param name="comparer">Функция для сравнения элементов, например, object.Equals или object.ReferenceEquals.</param>
    /// <returns>True, если содержимое коллекций эквивалетно или они обе null. Иначе - false.</returns>
    public static bool SequenceEqualsWithExactOrder<T>(this IEnumerable<T> first, IEnumerable<T> second,
      Func<T, T, bool> comparer)
    {
      if (object.ReferenceEquals(first, second))
        return true;
      if (first == null && second != null)
        return false;
      if (first != null && second == null)
        return false;
      var sourceEnumerator = first.GetEnumerator();
      var targetEnumerator = second.GetEnumerator();
      while (sourceEnumerator.MoveNext())
      {
        if (!targetEnumerator.MoveNext())
          return false;
        if (!comparer(sourceEnumerator.Current, targetEnumerator.Current))
          return false;
      }
      return true;
    }

    /// <summary>
    /// Проверить два массива на эквивалентность содержимого с учетом того, что коллекции могут быть Null.
    /// </summary>
    /// <typeparam name="T">Тип элементов массива.</typeparam>
    /// <param name="first">Первая массив.</param>
    /// <param name="second">Вторая массив.</param>
    /// <returns>True, если содержимое массивов эквивалетно или они оба null. Иначе - false.</returns>
    public static bool SafeSequenceEqual<T>(this T[] first, T[] second)
    {
      if (object.ReferenceEquals(first, second))
        return true;
      if (first == null && second != null)
        return false;
      if (first != null && second == null)
        return false;
      return first.SequenceEqual(second);
    }

    /// <summary>
    /// Выполнить действие для каждого элемента перечня.
    /// </summary>
    /// <typeparam name="T">Тип элементов перечня.</typeparam>
    /// <param name="collection">Перечень элементов.</param>
    /// <param name="action">Действие.</param>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
      if (action == null)
        throw new ArgumentNullException("action");

      foreach (T item in collection)
        action(item);
    }
   
    /// <summary>
    /// Разбить последовательность на страницы.
    /// </summary>
    /// <typeparam name="T">Тип элементов последовательности.</typeparam>
    /// <param name="source">Последовательность.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Набор страниц.</returns>
    public static IEnumerable<IList<T>> SplitPages<T>(this IEnumerable<T> source, int pageSize)
    {
      while (source.Any())
      {
        yield return source.Take(pageSize).ToList();
        source = source.Skip(pageSize);
      }
    }

    /// <summary>
    /// Проверить наличия атрибута у члена класса.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="memberInfo">Член класса.</param>
    /// <returns>true - атрибут задан.</returns>
    public static bool HasAttribute<T>(this MemberInfo memberInfo)
      where T : System.Attribute
    {
      return memberInfo.GetCustomAttributes(typeof(T), true).Any();
    }

    /// <summary>
    /// Отбросить null значения из коллекции.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="collection">Коллекция.</param>
    /// <returns>Коллекция без null значений.</returns>
    public static IEnumerable<T> SkipNull<T>(this IEnumerable<T> collection)
      where T : class
    {
      return collection.Where(value => value != null);
    }

    /// <summary>
    /// Отбросить null и пустые строки из коллекции.
    /// </summary>
    /// <param name="collection">Коллекция.</param>
    /// <returns>Коллекция без null и пустых строк.</returns>
    public static IEnumerable<string> SkipNullOrEmpty(this IEnumerable<string> collection)
    {
      return collection.Where(value => !string.IsNullOrEmpty(value));
    }

    /// <summary>
    /// Отбросить null и пустые строки с пробелами из коллекции.
    /// </summary>
    /// <param name="collection">Коллекция.</param>
    /// <returns>Коллекция без null и пустых строк с пробелами.</returns>
    public static IEnumerable<string> SkipNullOrWhiteSpace(this IEnumerable<string> collection)
    {
      return collection.Where(value => !string.IsNullOrWhiteSpace(value));
    }

    /// <summary>
    /// Создать множество из коллекции элементов.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="collection">Коллекция.</param>
    /// <returns>Множество.</returns>
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> collection)
    {
      return new HashSet<T>(collection);
    }

    /// <summary>
    /// Получить из коллекции все объекты заданного типа.
    /// </summary>
    /// <typeparam name="TSource">Тип элементов коллекции.</typeparam>
    /// <param name="source">Коллекция.</param>
    /// <param name="type">Тип элементов, которые нужно получить.</param>
    /// <returns></returns>
    public static IEnumerable<TSource> OfType<TSource>(this IEnumerable<TSource> source, Type type)
    {
      return source.Where(item => type.IsAssignableFrom(item.GetType()));
    }

    /// <summary>
    /// Получить индекс первого вхождения экземпляра, удовлетворяющего заданному условию.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="source">Коллекция.</param>
    /// <param name="condition">Условие.</param>
    /// <returns></returns>
    /// <remarks>
    /// По мотивам http://stackoverflow.com/questions/4075340/finding-first-index-of-element-that-matches-a-condition-using-linq.
    /// </remarks>
    public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> condition)
    {
      return source.Select((value, index) => new { value, index = index + 1 })
        .Where(p => condition(p.value))
        .Select(p => p.index)
        .FirstOrDefault() - 1;
    }
  }
}
