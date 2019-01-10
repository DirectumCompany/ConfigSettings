using System;
using System.IO;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Класс-расширение для работы с каталогами.
  /// </summary>
  public static class DirectoryUtils
  {
    #region Методы

    /// <summary>
    /// Рекурсивное удаление каталога с обработкой исключения доступа.
    /// </summary>
    /// <param name="path">Имя каталога, который необходимо удалить.</param>
    /// <returns>True, если удаление прошло успешно.</returns>
    public static bool TryDeleteDirectory(string path)
    {
      return InternalTryDeleteDirectory(path, true);
    }

    /// <summary>
    /// Внутренняя реализация рекурсивного удаления каталога с обработкой исключений при доступе к каталогу или его файлам.
    /// </summary>
    /// <param name="path">Имя каталога, который необходимо удалить.</param>
    /// <param name="isFirstAttempt">Признак того, что это первая попытка удаления каталога.</param>
    /// <returns>True, если удаление каталога прошло успешно.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
      Justification = "Семантика метода не предполагает выброса исключений.")]
    private static bool InternalTryDeleteDirectory(string path, bool isFirstAttempt)
    {
      try
      {
        Directory.Delete(path, true);
        return true;
      }
      catch (Exception e)
      {
        if (!isFirstAttempt)
          return false;

        if (e is UnauthorizedAccessException || (e is IOException && Directory.Exists(path)))
        {
          RemoveDirectoryAttribute(path, FileAttributes.ReadOnly);
          RemoveAllFileAttributeInDirectory(path, true);
        }
        return InternalTryDeleteDirectory(path, false);
      }
    }

    /// <summary>
    /// Убрать атрибут каталога.
    /// </summary>
    /// <param name="path">Имя каталога.</param>
    /// <param name="attributes">Атрибут, который необходимо удалить.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Перехват исключений в данном случае необходим.")]
    public static void RemoveDirectoryAttribute(string path, FileAttributes attributes)
    {
      try
      {
        var dirInfo = new DirectoryInfo(path);
        var currentAttributes = dirInfo.Attributes;
        var newAttributes = currentAttributes & ~attributes;
        if (currentAttributes != newAttributes)
          dirInfo.Attributes = newAttributes;
      }
      catch (Exception)
      {
        // Do nothing.
      }
    }

    /// <summary>
    /// Удалить атрибуты у всех файлов в каталоге.
    /// </summary>
    /// <param name="path">Имя каталога.</param>
    /// <param name="recursive">Признак, нужно ли удалять атрибуты файлов во вложенных каталогах.</param>
    private static void RemoveAllFileAttributeInDirectory(string path, bool recursive)
    {
      foreach (var file in Directory.EnumerateFiles(path))
        FileUtils.RemoveFileAttribute(file, FileAttributes.ReadOnly);

      if (!recursive)
        return;

      foreach (var subDir in Directory.EnumerateDirectories(path))
      {
        RemoveDirectoryAttribute(subDir, FileAttributes.ReadOnly);
        RemoveAllFileAttributeInDirectory(subDir, true);
      }
    }


    #endregion
  }
}