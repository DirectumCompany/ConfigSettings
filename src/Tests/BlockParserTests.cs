using System.Collections.Generic;
using ConfigSettings.Patch;
using FluentAssertions;
using NUnit.Framework;

namespace ConfigSettingsTests
{
  public class T1
  {
    public string Name { get; set; }
  }

  [TestFixture]
  public class BlockParserTests
  {
    [Test]
    public void SerializeArray()
    {
      BlockParser.Serialize(new List<T1> {new T1 { Name = "a1" }, new T1 { Name = "a2" }}).Should().Be(@"<T1>
    <Name>a1</Name>
  </T1>
  <T1>
    <Name>a2</Name>
  </T1>");
    }   

    [Test]
    public void DeserializeArray()
    {
      var t = BlockParser.Deserialize<List<T1>>(@"<T1>
    <Name>a1</Name>
  </T1>
  <T1>
    <Name>a2</Name>
  </T1>");
      t.Should().HaveCount(2);
      t[0].Name.Should().Be("a1");
      t[1].Name.Should().Be("a2");
    }
    
    [Test]
    public void SerializePoco()
    {
      BlockParser.Serialize(new T1 { Name = "a1" }).Should().Be(@"<T1>
  <Name>a1</Name>
</T1>");
    }

    [Test]
    public void DeserializePoco()
    {
      BlockParser.Deserialize<T1>(@"<T1>
  <Name>a1</Name>
</T1>").Name.Should().Be("a1");
    } 
  }
}
