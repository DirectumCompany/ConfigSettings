using System;

namespace ConfigSettings.Tests
{
  /// <summary>
  /// Расширения для работы с делегатами.
  /// </summary>
  public static class DelegateExtensions
  {
   

    /// <summary>
    /// Получить исключение.
    /// </summary>
    /// <param name="method">Метод.</param>
    /// <returns>Исключение.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
      Justification = "В тестах можно")]
    public static Exception GetException(this MethodThatThrows method)
    {
      Exception exception = null;
 
      try
      {
        method();
      }
      catch (Exception e)
      {
        exception = e;
      }
 
      return exception;
    }
  }
  
  /// <summary>
  /// Метод, который бросает исключение.
  /// </summary>
  public delegate void MethodThatThrows();
}