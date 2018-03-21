using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CommonLibrary
{
  /// <summary>
  /// Хэлпер для обработки строк.
  /// </summary>
  public static class StringUtils
  {
    #region Константы

    /// <summary>
    /// Символ пробела.
    /// </summary>
    private const char Space = ' ';

    /// <summary>
    /// Значение, которое добавляется в конец строки при её тримминге.
    /// </summary>
    private const string TrimmingEndValue = "...";

    /// <summary>
    /// Символ кавычек.
    /// </summary>
    private const string Quotes = "\"";

    #endregion

    #region Методы

    /// <summary>
    /// Перевести первую букву строки в верхний регистр.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns>Строка с первой буквой в верхнем регистре.</returns>
    public static string UppercaseFirst(this string value)
    {
      if (string.IsNullOrEmpty(value))
        return string.Empty;

      return char.ToUpper(value[0], CultureInfo.InvariantCulture) + value.Substring(1);
    }

    /// <summary>
    /// Расширенный метод который применяется к строке-шаблону и подставляет в него аргументы.
    /// </summary>
    /// <param name="format">Строка-шаблон.</param>
    /// <param name="args">Аргументы которые нужно подставить в строку.</param>
    /// <returns></returns>
    public static string Parameters(this string format, params object[] args)
    {
      return string.Format(format, args);
    }

    /// <summary>
    /// Закончить строку точкой, если она не оканчивается на какой-либо знак конца предложения.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <returns>Строка с точкой на конце.</returns>
    public static string EndWithPeriod(this string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return value;

      const string Period = ".";
      string[] excludeSymbols = new[] { Period, "?", "!", ":" };
      string trimmedValue = value.TrimEnd();
      return excludeSymbols.Any(s => trimmedValue.EndsWith(s, StringComparison.OrdinalIgnoreCase)) ? trimmedValue : trimmedValue + Period;
    }

    /// <summary>
    /// Преобразование строки в SecureString.
    /// </summary>
    /// <param name="value">Строка для преобразования.</param>
    /// <returns>Преобразованная SecureString.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
      Justification = "Нельзя уничтожать строку, поскольку она используется во внешнем по отношению к функции коде.")]
    public static SecureString ToSecureString(this string value)
    {
      SecureString secureStr = new SecureString();
      foreach (char ch in value)
        secureStr.AppendChar(ch);
      secureStr.MakeReadOnly();
      return secureStr;
    }

    /// <summary>
    /// Преобразование SecureString в небезопасную строку.
    /// </summary>
    /// <param name="value">Безопасная строка.</param>
    /// <returns>Небезопасная строка.</returns>
    public static string ToUnsecuredString(this SecureString value)
    {
      if (value == null)
        throw new ArgumentNullException(nameof(value));

      IntPtr unmanagedString = IntPtr.Zero;
      try
      {
        unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(value);
        return Marshal.PtrToStringUni(unmanagedString);
      }
      finally
      {
        Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
      }
    }

    /// <summary>
    /// Вычислить MD5-хеш для строки.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns>Хеш.</returns>
    public static string GetMD5Hash(this string value)
    {
      if (value == null)
        throw new ArgumentNullException(nameof(value));

      return Encoding.UTF8.GetBytes(value).GetMD5Hash();
    }

    /// <summary>
    /// Хеш коллекции элементов.
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции.</typeparam>
    /// <param name="collection">Коллекция элементов.</param>
    /// <param name="hashProperties">Свойства элемента для вычисления хэша.</param>
    /// <returns>MD5Hash в виде строки.</returns>
    public static string GetCollectionHash<T>(IEnumerable<T> collection, Func<T, IEnumerable<string>> hashProperties)
    {
      var sb = new StringBuilder();
      var sortedList = collection.ToList();
      sortedList.Sort((x, y) => string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase));
      foreach (var item in sortedList)
        foreach (var propertyValue in hashProperties(item))
          sb.Append(propertyValue ?? string.Empty);
      return sb.ToString().GetMD5Hash();
    }

    /// <summary>
    /// Удалить конец строки.
    /// </summary>
    /// <param name="originalValue">Исходное значение.</param>
    /// <param name="entryValue">Значение вхождения.</param>
    /// <returns>Строка без вхождения.</returns>
    /// <remarks>Используется сравнение строк OrdinalIgnoreCase.</remarks>
    public static string RemoveEnd(this string originalValue, string entryValue)
    {
      if (string.IsNullOrEmpty(originalValue))
        return string.Empty;
      if (string.IsNullOrEmpty(entryValue))
        return originalValue;

      return originalValue.EndsWith(entryValue, StringComparison.OrdinalIgnoreCase) ?
        originalValue.Remove(originalValue.Length - entryValue.Length, entryValue.Length) : originalValue;
    }

    /// <summary>
    /// Вычисление склонения существительного, идущего после числительного.
    /// </summary>
    /// <param name="number">Число.</param>
    /// <param name="nominative">Именительный падеж существительного.</param>
    /// <param name="genitiveSingular">Родительный падеж единственного числа существительного.</param>
    /// <param name="genitivePlural">Родительный падеж множественного числа существительного.</param>
    /// <returns>Существительное в нужной форме.</returns>
    /// <example>
    /// NumberDeclension(1, "час", "часа", "часов") // час.
    /// NumberDeclension(2, "час", "часа", "часов") // часа.
    /// NumberDeclension(5, "час", "часа", "часов") // часов.
    /// </example>
    public static string NumberDeclension(long number, string nominative, string genitiveSingular, string genitivePlural)
    {
      long lastDigit = number % 10;
      long lastTwoDigits = number % 100;
      if (lastDigit == 1 && lastTwoDigits != 11)
        return nominative;

      if ((lastDigit == 2 && lastTwoDigits != 12) || (lastDigit == 3 && lastTwoDigits != 13) || (lastDigit == 4 && lastTwoDigits != 14))
        return genitiveSingular;

      return genitivePlural;
    }

    /// <summary>
    /// Удалить из строки все символы не являющиеся буквами.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns>Строка содержащая только буквы.</returns>
    public static string RemoveNonLetters(this string value)
    {
      if (value == null)
        return null;
      var newString = new char[value.Length];
      int length = 0;
      foreach (var currentChar in value)
        if (char.IsLetter(currentChar))
        {
          newString[length] = currentChar;
          length++;
        }
      return new string(newString, 0, length);
    }

    /// <summary>
    /// Обрезать строку многоточием, если её длина превышает заданное максимальное количество символов.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <param name="maxLength">Ограничение по количеству символов в строке.</param>
    /// <returns>Строка, ограниченная заданным количеством символов.</returns>
    public static string TrimEnd(this string value, int maxLength)
    {
      string normalizedValue = value.TrimEnd();
      return normalizedValue.Length > maxLength ? normalizedValue.Substring(0, Math.Max(0, maxLength)) + TrimmingEndValue : normalizedValue;
    }

    /// <summary>
    /// Число прописью.
    /// </summary>
    /// <param name="number">Целое число.</param>
    /// <returns>Число, записанное словами.</returns>
    public static string NumberToWords(long number)
    {
      if (number == 0)
        return "нуль";

      var result = PositiveNumberToWords(Math.Abs(number));
      if (number < 0)
        result = string.Format("минус {0}", result);

      return result;
    }

    /// <summary>
    /// Посчитать количество вхождений подстроки в заданную строку.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <param name="substring">Подстрока.</param>
    /// <returns>Количество вхождений подстроки в заданную строку.</returns>
    public static int CountSubstringOccurrences(this string value, string substring)
    {
      var count = 0;
      var position = 0;
      while (true)
      {
        position = value.IndexOf(substring, position, StringComparison.OrdinalIgnoreCase);
        if (position == -1)
          break;
        count++;
        position++;
      }
      return count;
    }

    /// <summary>
    /// Записать целое положительное число словами.
    /// </summary>
    /// <param name="number">Целое положительное число.</param>
    /// <returns>Число, записанное словами.</returns>
    private static string PositiveNumberToWords(long number)
    {
      string[] units =
      {
        string.Empty, "од", "дв", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять", "десять",
        "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
      };
      string[] decades = { string.Empty, string.Empty, "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
      string[] hundreds = { string.Empty, "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
      string[] thousands = { string.Empty, string.Empty, "тысяч", "миллион", "миллиард", "триллион", "квадрилион", "квинтилион" };

      var numberByWords = string.Empty;
      var num = number;
      for (var th = 1; num > 0; th++)
      {
        int group = (int)(num % 1000);
        num = (num - group) / 1000;
        if (group > 0)
        {
          var digit1 = group % 10;
          var digit3 = (group - (group % 100)) / 100;
          var digit2 = (group - (digit3 * 100) - digit1) / 10;
          if (digit2 == 1)
            digit1 += 10;
          numberByWords = string.Format(Space + "{0}" + Space + "{1}" + Space + "{2}" + Space + "{3}",
            hundreds[digit3], decades[digit2], units[digit1] + EndDigit(digit1, th != 2), thousands[th] + EndThousands(th, digit1)) + numberByWords;
        }
      }

      return string.Join(Space.ToString(),
        numberByWords.Split(new[] { Space.ToString() }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Вернуть окончание для цифры, разбивающей число на тысячи (например, тысяча, миллион, миллиард).
    /// </summary>
    /// <param name="thousandOrder">Порядковый номер тысячного разряда.</param>
    /// <param name="thousandDigit">Цифра из класса тысяч.</param>
    /// <returns>Окончание для цифры.</returns>
    private static string EndThousands(long thousandOrder, long thousandDigit)
    {
      var is234 = thousandDigit >= 2 && thousandDigit <= 4;
      var isMoreThan4 = thousandDigit > 4 || thousandDigit == 0;
      if ((thousandOrder > 2 && is234) || (thousandOrder == 2 && thousandDigit == 1))
        return "а";
      else if (thousandOrder > 2 && isMoreThan4)
        return "ов";
      else if (thousandOrder == 2 && is234)
        return "и";

      return string.Empty;
    }

    /// <summary>
    /// Вернуть окончание для одиночной цифры.
    /// </summary>
    /// <param name="digit">Цифра.</param>
    /// <param name="isMale">Признак того, что числительное должно быть мужского рода.</param>
    /// <returns>Окончание для цифры.</returns>
    private static string EndDigit(int digit, bool isMale)
    {
      if (digit > 2 || digit == 0)
        return string.Empty;

      if (digit == 1)
        return isMale ? "ин" : "на";
      else
        return isMale ? "а" : "е";
    }

    /// <summary>
    /// Обернуть строку в кавычки.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns>Строку обернутая в кавычки.</returns>
    public static string AddQuotes(this string value)
    {
      return string.Format("{0}{1}{2}", Quotes, value, Quotes);
    }

    /// <summary>
    /// Удалить из строки email адрес (вместе с мусором).
    /// </summary>
    /// <param name="value">Строка с email.</param>
    /// <returns>Строка без email.</returns>
    public static string RemoveEmailAddress(this string value)
    {
      if (string.IsNullOrEmpty(value))
        return string.Empty;

      if (value.IndexOf('@') == -1)
        return value.Trim();

      string regex = "\\(.*?\\)|<.*?>";
      value = Regex.Replace(value, regex, string.Empty);
      value = value.Trim().Trim('\"');
      return value;
    }

    /// <summary>
    /// Заменить символы переноса строк.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <param name="replaceValue">На что заменить.</param>
    /// <returns>Строка без символов перевода строк.</returns>
    public static string ReplaceNewLines(this string value, string replaceValue)
    {
      return value.Replace("\r\n", replaceValue).Replace("\n", replaceValue).Replace("\r", replaceValue);
    }

    /// <summary>
    /// Заменить пробельные символы.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <param name="replaceValue">На что заменить.</param>
    /// <returns>Строка без символов перевода строк.</returns>
    public static string ReplaceWhiteSpaces(this string value, string replaceValue)
    {
      // Передача null в качестве разделителя приводит к разделению по пробельным символам.
      return string.Join(replaceValue, value.Split((string[])null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Проверить, что строка является русскоязычной.
    /// </summary>
    /// <param name="value">Строка.</param>
    /// <returns></returns>
    public static bool IsRussian(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return false;

      var letters = value.Where(c => char.IsLetter(c));
      return letters.Any() && letters.All(c => IsRussian(c));
    }

    /// <summary>
    /// Проверить, что символ является русским.
    /// </summary>
    /// <param name="character">Символ.</param>
    /// <returns></returns>
    public static bool IsRussian(char character)
    {
      return (character >= 'а' && character <= 'я') || (character >= 'А' && character <= 'Я') ||
        character == 'ё' || character == 'Ё';
    }

    /// <summary>
    /// Разбить текст на строки.
    /// </summary>
    /// <param name="text">Разбиваемый текст.</param>
    /// <returns>Список строк.</returns>
    /// <remarks>Использовать вместо Split(new[] { Environment.NewLine }), чтобы обрабатывать различные маркеры конца строк.</remarks>
    public static IEnumerable<string> SplitToLines(this string text)
    {
      if (string.IsNullOrEmpty(text))
        yield break;

      using (var reader = new StringReader(text))
      {
        var line = reader.ReadLine();
        while (line != null)
        {
          yield return line;
          line = reader.ReadLine();
        }
      }
    }

    /// <summary>
    /// Заменить подстроку без учёта регистра.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <param name="search">Что заменить.</param>
    /// <param name="replacement">На что заменить.</param>
    /// <returns>Строка.</returns>
    public static string ReplaceIgnoreCase(this string input, string search, string replacement)
    {
      return Regex.Replace(input, Regex.Escape(search), replacement.Replace("$", "$$"),
        RegexOptions.IgnoreCase);
    }

    #endregion
  }
}
