using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace CommonLibrary
{
  /// <summary>
  /// Класс-расширение для работы с файлами.
  /// </summary>
  public static class FileUtils
  {
    #region Константы

    /// <summary>
    /// Символ, на который заменяются все недопустимые символы в имени файла.
    /// </summary>
    private const string InvalidCharInFileNameReplacement = "_";

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Зарезервированные имена в Windows.
    /// </summary>
    private static readonly string[] reservedWords =
    {
      "CON", "PRN", "AUX", "NUL",
      "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
      "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    #endregion

    #region Методы

    /// <summary>
    /// Очищает некорректные символы в имени файла.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <returns>Корректное для файловой системы имя файла.</returns>
    public static string NormalizeFileName(string fileName)
    {
      // Вырежем некорректные символы и проверим на совпадение с зарезервированными именами.
      // Источник: http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
      var invalidFileNameChars = Path.GetInvalidFileNameChars();
      var validFileName = string.Join(InvalidCharInFileNameReplacement, fileName.Split(invalidFileNameChars, StringSplitOptions.RemoveEmptyEntries));
      if (reservedWords.Any(w => string.Equals(w, validFileName, StringComparison.OrdinalIgnoreCase)))
        validFileName = $"{validFileName}{InvalidCharInFileNameReplacement}";
      return validFileName;
    }

    /// <summary>
    /// Замена xml файла с простой проверкой на различие.
    /// </summary>
    /// <param name="config">Xml-файл.</param>
    /// <param name="configPath">Путь к файлу.</param>
    public static void ReplaceXmlFile(XDocument config, string configPath)
    {
      var newContent = ToBytes(config);
      if (GetFileContent(configPath).SafeSequenceEqual(newContent))
        return;

      try
      {
        File.WriteAllBytes(configPath, newContent);
      }
      catch (UnauthorizedAccessException uaex)
      {
        // TODO Добавить логирование.
        // log.Value.Error($"UnauthorizedAccessException: Path {configPath}", uaex);
        throw;
      }
    }

    /// <summary>
    /// Преобразовать xml-конфиг в набор байт.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    /// <returns>Набор байт.</returns>
    public static byte[] ToBytes(XDocument config)
    {
      using (var stream = new MemoryStream())
      {
        config.Save(stream);
        return stream.ToArray();
      }
    }

    /// <summary>
    /// Получить содержимое файла.
    /// </summary>
    /// <param name="newConfig">Путь к файлу.</param>
    /// <returns>Null, если файл не найден.</returns>
    public static byte[] GetFileContent(string newConfig)
    {
      if (File.Exists(newConfig))
        return File.ReadAllBytes(newConfig);

      return new byte[0];
    }

    /// <summary>
    /// Открыть и получить содержимое файла.
    /// </summary>
    /// <param name="path">Полное имя файла.</param>
    /// <returns>Поток данных из файла.</returns>
    public static FileStream OpenRead(string path)
    {
      return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>
    /// Открыть и получить содержимое редактируемого файла.
    /// </summary>
    /// <param name="path">Полное имя файла.</param>
    /// <returns>Поток данных из файла.</returns>
    public static FileStream OpenEditableRead(string path)
    {
      return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    /// <summary>
    /// Проверить заблокированность файла на чтение.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>Признак заблокированности файла для чтения.</returns>
    public static bool IsLockedForRead(string path)
    {
      try
      {
        using (OpenRead(path))
          return false;
      }
      catch (UnauthorizedAccessException)
      {
        return !File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);
      }
      catch (IOException e)
      {
        if (File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
          return false;
        return HasIOExceptionError(e);
      }
    }

    /// <summary>
    /// Проверить, содержит ли исключение ввода-вывода реальную ошибку.
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>True - если есть ошибка.</returns>
    public static bool HasIOExceptionError(Exception exception)
    {
      int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
      return errorCode == 32 || errorCode == 33;
    }

    /// <summary>
    /// Получить путь к файлу относительно указанной папки.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу.</param>
    /// <param name="rootFolderPath">Путь к корневой папке, в которой лежит файл.</param>
    /// <returns>Относительный путь к файлу.</returns>
    public static string GetRelativePath(string filePath, string rootFolderPath)
    {
      return filePath.Replace(rootFolderPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Принудительно создать файл.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Поток.</returns>
    /// <remarks>Создаёт подпапки в случае их отсутствия.</remarks>
    public static FileStream ForceCreate(string filePath)
    {
      var directoryName = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directoryName))
        Directory.CreateDirectory(directoryName);
      return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    /// <summary>
    /// Скопировать файл с созданием папки, если ее еще нет.
    /// </summary>
    /// <param name="source">Источник.</param>
    /// <param name="destination">Назначение.</param>
    public static void CopyFile(string source, string destination)
    {
      var destDirectory = Path.GetDirectoryName(destination);
      if (!Directory.Exists(destDirectory))
        Directory.CreateDirectory(destDirectory);
      File.Copy(source, destination, true);
    }

    #endregion
  }
}
