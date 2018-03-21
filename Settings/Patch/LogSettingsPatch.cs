using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CommonLibrary.Settings.Patch
{
  /// <summary>
  /// Класс для коррекции LogSettings конфига.
  /// TODO: Вынести логику в ConfigPatch.
  /// </summary>
  public class LogSettingsPatch
  {
    #region Поля и свойства

    /// <summary>
    /// Xml-конфиг.
    /// </summary>
    private readonly XDocument config;

    /// <summary>
    /// Оригинальное имя файла.
    /// </summary>
    private readonly string currentConfigPath;

    #endregion

    #region Методы

    /// <summary>
    /// Скорректировать конфиг.
    /// </summary>
    public void Patch()
    {
      var root = this.config.Root;
      if (root == null || root.Name.LocalName != "nlog")
        return;

      var element = root.Elements().FirstOrDefault(e => e.Name.LocalName == "extensions");
      if (element == null)
        return;

      foreach (var elem in element.Elements())
      {
        if (elem.Name.LocalName != "add")
          continue;

        var assembylFileAttribute = elem.Attribute("assemblyFile");
        if (assembylFileAttribute == null)
          continue;

        // Разворачиванием полное имя до файла.
        // Иначе при смене папки, в которой лежит наш конфиг, все отсносительные ссылки будут битые.
        assembylFileAttribute.Value = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.currentConfigPath), assembylFileAttribute.Value));
      }
    }

    #endregion

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="config">Xml-конфиг.</param>
    /// <param name="currentConfigPath">Путь к конфигу.</param>
    public LogSettingsPatch(XDocument config, string currentConfigPath)
    {
      this.config = config;
      this.currentConfigPath = currentConfigPath;
    }

    #endregion
  }
}
