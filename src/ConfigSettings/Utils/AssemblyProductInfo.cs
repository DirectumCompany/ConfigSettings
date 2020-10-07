using System.Linq;
using System.Reflection;

namespace ConfigSettings.Utils
{
  /// <summary>
  /// Информация о продукте.
  /// </summary>
  public class AssemblyProductInfo
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
    /// Нормализованное (без пробелов) имя продукта.
    /// </summary>
    public string NormalizedProductName => this.ProductName.Replace(" ", string.Empty);

    /// <summary>
    /// Конструктор по умолчанию.
    /// </summary>
    /// <param name="assemblyWithInfo">Сборка, содержащая информацию о продукте.</param>
    public AssemblyProductInfo(Assembly assemblyWithInfo)
    {
      var customAttributesData = assemblyWithInfo.GetCustomAttributesData();
      this.CompanyName = (string)customAttributesData.Single(ca => ca.AttributeType == typeof(AssemblyCompanyAttribute)).ConstructorArguments.Single().Value;
      this.ProductName = (string)customAttributesData.Single(ca => ca.AttributeType == typeof(AssemblyProductAttribute)).ConstructorArguments.Single().Value;
    }
  }
}
