using System;
using System.IO;
using System.Reflection;
using ConfigSettings.Utils;

namespace ConfigSettings.Internal
{
  /// <summary>
  /// Класс предопределенных специальных папок платформы.
  /// </summary>
  public static class SpecialFolders
  {
    #region Поля и свойства

    /// <summary>
    /// Информация о продукте для главной сборки.
    /// </summary>
    private static readonly Lazy<AssemblyProductInfo> mainAssemblyProductInfo = new Lazy<AssemblyProductInfo>(() => new AssemblyProductInfo(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()));

    #endregion

    #region Методы

    /// <summary>
    /// Создать и вернуть подпапку в папке продукта профиля пользователя.
    /// </summary>
    /// <param name="productInfo">Информация о продукте.</param>
    /// <param name="subpath">Путь к подпапкам.</param>
    /// <returns>Папка.</returns>
    public static string ProductUserApplicationData(AssemblyProductInfo productInfo, params string[] subpath)
    {
      var subpaths = Path.Combine(subpath);
      return Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), productInfo.CompanyName, productInfo.NormalizedProductName, subpaths)).FullName;
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
