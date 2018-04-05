namespace ConfigSettings.Settings.Patch
{
  public class VariableValue
  {
    public string Value { get; }

    public string FilePath { get; }

    public VariableValue(string value, string filePath)
    {
      this.Value = value;
      this.FilePath = filePath;
    }
  }
}
