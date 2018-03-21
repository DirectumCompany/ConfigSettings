using System.Xml.Linq;

namespace CommonLibrary.Settings.Patch
{
  /// <summary>
  /// Базовый класс для установки значений элементов xml-конфига.
  /// </summary>
  public abstract class BaseSetter
  {
    private readonly ExpressionEvaluator expressionEvaluator;

    /// <summary>
    /// Получить фактическое значение для установки.
    /// </summary>
    /// <param name="currentValue">Текущее значение.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    /// <returns>Фактическое (новое) значение.</returns>
    public string EvaluateValue(string currentValue, ConfigSettings configSettings)
    {
      return this.expressionEvaluator.EvaluateValue(currentValue, configSettings);
    }

    /// <summary>
    /// Применить установку значения к переданному xml-элементу.
    /// </summary>
    /// <param name="element">Xml-элемент.</param>
    /// <param name="configSettings">Настройки конфига.</param>
    /// <param name="isAncestorSetter">Признак, что правило установки является унаследованным (с предыдущих уровней xml-документа).</param>
    public abstract void Apply(XElement element, ConfigSettings configSettings, bool isAncestorSetter);

    #region Конструкторы

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="setterExpression">Выражение для установки значения.</param>
    protected BaseSetter(string setterExpression)
    {
      this.expressionEvaluator = new ExpressionEvaluator(setterExpression);
    }

    #endregion
  }
}
