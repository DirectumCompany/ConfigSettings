using System.Security.Cryptography;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Класс-расширение для работы с потоками.
  /// </summary>
  public static class StreamUtils
  {
    /// <summary>
    /// Получить строковое представление MD5-хеша.
    /// </summary>
    /// <param name="hash">MD5-хеш.</param>
    /// <returns>Строковое представление MD5-хеша.</returns>
    private static string GetHashString(byte[] hash)
    {
      string resultHashString = string.Empty;
      for (int i = 0; i <= hash.Length - 1; i++)
        resultHashString += hash[i].ToString("x2");
      return resultHashString;
    }

  
    /// <summary>
    /// Метод-расширение для получения MD5-хеша массива байт.
    /// </summary>
    /// <param name="data">Массив байт.</param>
    /// <returns>MD5-хеш массива байт.</returns>
    public static string GetMD5Hash(this byte[] data)
    {
      using (var algorythmMd5 = (HashAlgorithm)new MD5CryptoServiceProvider())
        return GetHashString(algorythmMd5.ComputeHash(data));
    }          
  }
}
