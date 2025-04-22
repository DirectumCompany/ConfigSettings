using System;
using System.IO;
using System.Reflection;
using ConfigSettings.Utils;

namespace ConfigSettings.Internal
{
  /// <summary>
  /// Класс предопределенных специальных папок платформы.
  /// </summary>
  internal static class SpecialFolders
  {
    #region Константы

    /// <summary>
    /// Имя переменной окружения, содержащей имя компании.
    /// </summary>
    private const string CompanyNameEnvironmentVariableName = "COMPANY_NAME";

    /// <summary>
    /// Имя переменной окружения, содержащей имя продукта. 
    /// </summary>
    private const string ProductNameEnvironmentVariableName = "PRODUCT_NAME";
    
    #endregion
    
    #region Поля и свойства

    /// <summary>
    /// Информация о продукте из главной сборки.
    /// </summary>
    private static readonly Lazy<AssemblyProductInfo> mainAssemblyProductInfo = new Lazy<AssemblyProductInfo>(() => new AssemblyProductInfo(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()));

    /// <summary>
    /// Имя компании.
    /// </summary>
    private static string CompanyName => Environment.GetEnvironmentVariable(CompanyNameEnvironmentVariableName) ?? mainAssemblyProductInfo.Value.CompanyName;

    /// <summary>
    /// Имя продукта.
    /// </summary>
    private static string ProductName => Environment.GetEnvironmentVariable(ProductNameEnvironmentVariableName) ?? mainAssemblyProductInfo.Value.NormalizedProductName;

    #endregion

    #region Методы

    /// <summary>
    /// Создать и вернуть подпапку в папке продукта профиля пользователя.
    /// </summary>
    /// <param name="subpath">Путь к подпапкам.</param>
    /// <returns>Папка.</returns>
    internal static string ProductUserApplicationData(params string[] subpath)
    {
      var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      if (string.IsNullOrEmpty(applicationData))
        applicationData = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

      var subpaths = Path.Combine(subpath);
      return Directory.CreateDirectory(Path.Combine(applicationData, CompanyName, ProductName, subpaths)).FullName;
    }

    #endregion
  }
}
