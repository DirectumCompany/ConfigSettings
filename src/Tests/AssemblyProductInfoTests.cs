using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ConfigSettings.Internal;
using ConfigSettings.Utils;
using NUnit.Framework;

namespace ConfigSettingsTests
{
  [TestFixture]
  public class AssemblyProductInfoTests
  {
    [Test]
    public void TestAssemblyProductInfo()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var companyName = ((AssemblyCompanyAttribute)assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute)).Single()).Company;
      var productName = ((AssemblyProductAttribute)assembly.GetCustomAttributes(typeof(AssemblyProductAttribute)).Single()).Product;
      var assemblyProductInfo = new AssemblyProductInfo(assembly);
      Assert.AreEqual(assemblyProductInfo.CompanyName, companyName);
      Assert.AreEqual(assemblyProductInfo.ProductName, productName);
      Assert.AreEqual(assemblyProductInfo.NormalizedProductName, this.NormalizeProductName(productName));
      Assert.IsTrue(!assemblyProductInfo.NormalizedProductName.Contains(" "));
    }

    private string NormalizeProductName(string productName)
    {
      return productName.Replace(" ", string.Empty);
    }
  }
}