using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigSettings.Patch
{
  /// <summary>
  /// Парсер функций.
  /// </summary>
  public class ExpressionEvaluator
  {
    /// <summary>
    /// Префикс имени функции.
    /// </summary>
    private const string FunctionPrefix = "$";

    /// <summary>
    /// Имя функции идентичности (тождественности), всегда возвращающей без изменений значение своего первого параметра.
    /// </summary>
    private const string IdentityFunctionName = "id";

    /// <summary>
    /// Имя функции.
    /// </summary>
    private string FunctionName { get; set; }

    /// <summary>
    /// Аргументы функции (формальные).
    /// </summary>
    private string[] FunctionArguments { get; set; }

    /// <summary>
    /// Разбить аргументы функции на части.
    /// </summary>
    /// <param name="arguments">Аргументы.</param>
    /// <returns>Список аргументов.</returns>
    private static string[] ParseFunctionArguments(string arguments)
    {
      var result = new List<string>();
      var insideStringLiteral = false;
      var functionNestedLevel = 0;
      var currentPosition = 0;
      for (int i = 0; i < arguments.Length; i++)
      {
        var symbol = arguments[i];
        if (symbol == '"')
          insideStringLiteral = !insideStringLiteral;
        else if (symbol == '(' && !insideStringLiteral)
          functionNestedLevel++;
        else if (symbol == ')' && !insideStringLiteral)
          functionNestedLevel--;
        else if (symbol == ',' && !insideStringLiteral && functionNestedLevel == 0)
        {
          if (currentPosition < i)
            result.Add(arguments.Substring(currentPosition, i - currentPosition));
          currentPosition = i + 1;
        }
      }
      if (currentPosition < arguments.Length)
        result.Add(arguments.Substring(currentPosition));
      return result.Select(s => s.Trim()).ToArray();
    }

    /// <summary>
    /// Распарсить выражение вызова функции - определить имя функции и её формальные аргументы.
    /// </summary>
    /// <param name="functionExpression">Строка с выражением - вызовом функции.</param>
    /// <remarks>Например, "$replace("abc",PROTOCOL)".</remarks>
    private void ParseFunctionExpression(string functionExpression)
    {
      var firstParenthesisIndex = functionExpression.IndexOf('(');
      var lastParenthesisIndex = functionExpression.LastIndexOf(')');
      if (firstParenthesisIndex < 0 || lastParenthesisIndex < 0)
        return;
      this.FunctionName = functionExpression.Substring(FunctionPrefix.Length, firstParenthesisIndex - FunctionPrefix.Length);
      this.FunctionArguments = ParseFunctionArguments(functionExpression.Substring(firstParenthesisIndex + 1, lastParenthesisIndex - firstParenthesisIndex - 1));
    }

    /// <summary>
    /// Получить фактическое значение для установки.
    /// </summary>
    /// <param name="currentValue">Текущее значение.</param>
    /// <param name="configSettingsParser">Настройки конфига.</param>
    /// <returns>Фактическое (новое) значение.</returns>
    public string EvaluateValue(string currentValue, ConfigSettingsParser configSettingsParser)
    {
      if (string.IsNullOrEmpty(this.FunctionName))
        return null;
      var actualArgumentsValues = this.FunctionArguments.Select(arg => this.GetActualArgumentValue(arg, configSettingsParser)).ToArray();
      if (actualArgumentsValues.Any(v => v == null))
        return null;
      if (this.FunctionName == IdentityFunctionName)
        return actualArgumentsValues[0];
      if (this.FunctionName == "replace" && actualArgumentsValues.Length == 2)
        return currentValue.Replace(actualArgumentsValues[0], actualArgumentsValues[1]);
      if (this.FunctionName == "replace-if" && actualArgumentsValues.Length == 3 && actualArgumentsValues[0].ToUpperInvariant() == "TRUE")
        return currentValue.Replace(actualArgumentsValues[1], actualArgumentsValues[2]);
      if (this.FunctionName == "replace-if-not" && actualArgumentsValues.Length == 3 && actualArgumentsValues[0].ToUpperInvariant() != "TRUE")
        return currentValue.Replace(actualArgumentsValues[1], actualArgumentsValues[2]);
      if (this.FunctionName == "concat")
        return string.Join(string.Empty, actualArgumentsValues);
      if (this.FunctionName == "equals" && actualArgumentsValues.Length == 2)
        return string.Equals(actualArgumentsValues[0], actualArgumentsValues[1], StringComparison.OrdinalIgnoreCase) ? "true" : "false";
      if (this.FunctionName == "not" && actualArgumentsValues.Length == 1)
        return string.Equals(actualArgumentsValues[0], "false", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
      return null;
    }

    /// <summary>
    /// Получить фактическое значение аргумента.
    /// </summary>
    /// <param name="argumentExpression">Строка с выражением-аргументом.</param>
    /// <param name="configSettingsParser">Настройки конфига.</param>
    /// <returns>Фактическое значение аргумента.</returns>
    private string GetActualArgumentValue(string argumentExpression, ConfigSettingsParser configSettingsParser)
    {
      if (argumentExpression.StartsWith(FunctionPrefix, StringComparison.Ordinal))
        return new ExpressionEvaluator(argumentExpression).EvaluateValue(string.Empty, configSettingsParser);
      if (argumentExpression.Length >= 2 && argumentExpression[0] == '"' && argumentExpression[argumentExpression.Length - 1] == '"')
        return argumentExpression.Substring(1, argumentExpression.Length - 2);
      return configSettingsParser.GetVariable(argumentExpression)?.Value ??
             configSettingsParser.GetBlock(argumentExpression)?.IsEnabled?.ToString().ToLower();
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="setterExpression">Выражение для установки значения.</param>
    public ExpressionEvaluator(string setterExpression)
    {
      if (setterExpression.StartsWith(FunctionPrefix, StringComparison.Ordinal))
      {
        this.ParseFunctionExpression(setterExpression);
      }
      else
      {
        this.FunctionName = IdentityFunctionName;
        this.FunctionArguments = new[] { setterExpression };
      }
    }
  }
}
