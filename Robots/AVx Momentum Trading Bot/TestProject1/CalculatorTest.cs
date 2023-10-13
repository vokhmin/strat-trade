// CalculatorTests.cs
using NUnit.Framework;
using System;

[TestFixture]
public class CalculatorTests
{
    [Test]
    public void TestAdd()
    {
        // given:
        Calculator calculator = new Calculator();
        // then:
        int result = calculator.Add(3, 4);
        Assert.That(result, Is.EqualTo(7));
    }
}
