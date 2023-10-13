namespace TestProject1;

using System.Diagnostics;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Debug.WriteLine("Setup");
    }

    [Test]
    public void Test1()
    {
        Debug.WriteLine("Test1");
        Assert.Pass();
    }
}