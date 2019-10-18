using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using ConfigSettings.Utils;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Парсер блоков.
  /// </summary>
  public static class BlockParser
  {
    private static bool ImplementsGenericInterface(Type type, Type interfaceType)
    {
      return type
        .GetTypeInfo()
        .ImplementedInterfaces
        .Any(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == interfaceType);
    }

    /// <summary>
    /// Получить имя тип типа, лежащее в основе generic enumerable типа.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <returns>Имя основного типа. Null, если исходный тип не generic enumerable.</returns>
    private static string GetUnderlyingTypeNameOfGenericEnumerable<T>()
    {
      var type = typeof(T);
      if (!ImplementsGenericInterface(type, typeof(IEnumerable<>)))
        return null;

      return type.GenericTypeArguments.FirstOrDefault()?.Name;
    }

    /// <summary>
    /// Преобразовать строку в поток.
    /// </summary>
    /// <param name="content">Строка.</param>
    /// <returns>Поток.</returns>
    private static Stream ToStream(string content)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      writer.Write(content);
      writer.Flush();
      stream.Position = 0;
      return stream;
    }

    /// <summary>
    /// Десериализовать блок
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content">Содержимое блока с заголовком. (Без ArrayOfT в случае массива).</param>
    /// <returns>Экземпляр типа.</returns>
    public static T Deserialize<T>(string content) where T : class
    {
      if (string.IsNullOrEmpty(content))
        return null;

      content = content.Trim();

      var typeName = GetUnderlyingTypeNameOfGenericEnumerable<T>();
      if (!string.IsNullOrEmpty(typeName))
        content = $"<ArrayOf{typeName}>{content}</ArrayOf{typeName}>";

      var ns = new XmlSerializerNamespaces();
      ns.Add(string.Empty, string.Empty);

      using (var reader = XmlReader.Create(ToStream(content)))
        return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
    }
    
    /// <summary>
    /// Сериализовать тип в xml строку в формате блока. Для типов-списков заголовки вырезаются.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="value">Экземпляр.</param>
    /// <returns>Строка.</returns>
    public static string Serialize<T>(T value) where T : class
    {
      if (value == null)
        return null;

      var ns = new XmlSerializerNamespaces();
      ns.Add(string.Empty, string.Empty);

      var stringWriter = new StringWriter();
      using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true}))
      {
        new XmlSerializer(typeof(T)).Serialize(writer, value, ns);
        var result = stringWriter.ToString();
        var typeName = GetUnderlyingTypeNameOfGenericEnumerable<T>();
        if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(result))
        {
          result = StringUtils.ReplaceFirstOccurrence(result, $"<ArrayOf{typeName}>", string.Empty);
          result = StringUtils.ReplaceLastOccurrence(result, $"</ArrayOf{typeName}>", string.Empty);
          result = result.Trim();
        }

        return result;
      }
    }
  }
}
