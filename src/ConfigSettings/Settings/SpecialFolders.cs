using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConfigSettings.Internal
{
  /// <summary>
  /// Класс предопределенных специальных папок платформы.
  /// </summary>
  public static class SpecialFolders
  {
    #region Вложенные типы

    /// <summary>
    /// Информация о продукте.
    /// </summary>
    private class ProductInfo
    {
      /// <summary>
      /// Имя компании.
      /// </summary>
      public string CompanyName { get; }

      /// <summary>
      /// Имя продукта.
      /// </summary>
      public string ProductName { get; }

      /// <summary>
      /// Конструктор по умолчанию.
      /// </summary>
      /// <param name="assemblyWithInfo">Сборка содержащая информацию о продукте.</param>
      public ProductInfo(Assembly assemblyWithInfo)
      {
        var customAttributesData = assemblyWithInfo.GetCustomAttributesData();
        this.CompanyName = (string)customAttributesData.Single(ca => ca.AttributeType == typeof(AssemblyCompanyAttribute)).ConstructorArguments.Single().Value;
        this.ProductName = (string)customAttributesData.Single(ca => ca.AttributeType == typeof(AssemblyProductAttribute)).ConstructorArguments.Single().Value;
      }
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Информация о продукте для главной сборки.
    /// </summary>
    private static readonly Lazy<ProductInfo> mainAssemblyProductInfo = new Lazy<ProductInfo>(() => new ProductInfo(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()));

    #endregion

    #region Методы

    /// <summary>
    /// Создать и вернуть подпапку в папке продукта профиля пользователя.
    /// </summary>
    /// <param name="productInfo">Информация о продукте.</param>
    /// <param name="subpath">Путь к подпапкам.</param>
    /// <returns>Папка.</returns>
    private static string ProductUserApplicationData(ProductInfo productInfo, params string[] subpath)
    {
      var subpaths = Path.Combine(subpath);
      return Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), productInfo.CompanyName, productInfo.ProductName, subpaths)).FullName;
    }

    /// <summary>
    /// Создать и вернуть подпапку в папке продукта профиля пользователя.
    /// </summary>
    /// <param name="subpath">Путь к подпапкам.</param>
    /// <returns>Папка.</returns>
    public static string ProductUserApplicationData(params string[] subpath)
    {
      return ProductUserApplicationData(mainAssemblyProductInfo.Value, subpath);
    }

    #endregion
  }
}
