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
  /// ������ ������.
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
    /// �������� ��� ��� ����, ������� � ������ generic enumerable ����.
    /// </summary>
    /// <typeparam name="T">���.</typeparam>
    /// <returns>��� ��������� ����. Null, ���� �������� ��� �� generic enumerable.</returns>
    private static string GetUnderlyingTypeNameOfGenericEnumerable<T>()
    {
      return GetUnderlyingTypeNameOfGenericEnumerable(typeof(T));
    }

    /// <summary>
    /// ���������������� ������ �����.
    /// </summary>
    /// <param name="input">������.</param>
    /// <returns>������ � ��������� ������ ������.</returns>
    public static string FirstCharToUpper(this string input)
    {
      if (string.IsNullOrEmpty(input))
        return input;

      return input.First().ToString().ToUpper() + input.Substring(1);
    }

    /// <summary>
    /// �������� ElementName �� ��������� XmlRoot, ���� �� �����.
    /// </summary>
    /// <param name="type">����.</param>
    /// <returns>������ � ������ ���� �� xml ���������.</returns>
    public static string GetElementNameFromXmlRoot(Type type)
    {
      return FirstCharToUpper(type.GetCustomAttribute<XmlRootAttribute>()?.ElementName);
    }

    /// <summary>
    /// �������� ��� ��� ����, ������� � ������ generic enumerable ����.
    /// </summary>
    /// <param name="type">���.</param>
    /// <returns>��� ��������� ����. Null, ���� �������� ��� �� generic enumerable.</returns>
    private static string GetUnderlyingTypeNameOfGenericEnumerable(Type type)
    {
      var underlyingType = GetUnderlyingTypeOfGenericEnumerable(type);
      if (underlyingType == null)
        return null;

      var childType = GetUnderlyingTypeOfGenericEnumerable(underlyingType);
      // �������� ���.
      if (childType == null)
      {
        var typeNameFromXmlRoot = GetElementNameFromXmlRoot(underlyingType);
        if (!string.IsNullOrEmpty(typeNameFromXmlRoot))
          return typeNameFromXmlRoot;

        return underlyingType.Name;
      }

      return $"ArrayOf{GetUnderlyingTypeNameOfGenericEnumerable(underlyingType)}";
    }

    /// <summary>
    /// �������� ��� ����, �������� � ������ generic enumerable ����.
    /// </summary>
    /// <param name="type">���.</param>
    /// <returns>���. Null, ���� �������� ��� �� generic enumerable.</returns>
    private static Type GetUnderlyingTypeOfGenericEnumerable(Type type)
    {
      if (!ImplementsGenericInterface(type, typeof(IEnumerable<>)))
        return null;

      return type.GenericTypeArguments.FirstOrDefault();
    }

    /// <summary>
    /// ������������� ������ � �����.
    /// </summary>
    /// <param name="content">������.</param>
    /// <returns>�����.</returns>
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
    /// ��������������� ����
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content">���������� ����� � ����������. (��� ArrayOfT � ������ �������).</param>
    /// <returns>��������� ����.</returns>
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
    /// ������������� ��� � xml ������ � ������� �����. ��� �����-������� ��������� ����������.
    /// </summary>
    /// <typeparam name="T">���.</typeparam>
    /// <param name="value">���������.</param>
    /// <returns>������.</returns>
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
