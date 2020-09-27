using System;
using System.Collections.Generic;
using System.Reflection;

namespace ConfigSettings.CommandLine
{
  public static class ReflectionUtils
  {
    public static IReadOnlyList<MethodInfo> GetPublicMethods(Type type)
    {
      return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
    }
  }
}
