﻿using System;
using System.IO;
using System.Xml.Linq;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Класс-расширение для работы с файлами.
  /// </summary>
  public static class FileUtils
  {
    #region Методы

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

    #endregion
  }
}
