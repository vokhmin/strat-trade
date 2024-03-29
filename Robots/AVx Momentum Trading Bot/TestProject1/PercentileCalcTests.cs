// PercentileCalcTests.cs
using NUnit.Framework;
using System;

[TestFixture]
public class PercentileCalcTests
{
    [Test]
    public void CalculatePercentile_EmptyData_ThrowsArgumentException()
    {
        double[] data = { };
        Assert.Throws<ArgumentException>(() => PercentileCalculator.CalculatePercentile(data, 0.5));
    }

    [Test]
    public void CalculatePercentile_InvalidPercentile_ThrowsArgumentOutOfRangeException()
    {
        double[] data = { 1, 2, 3 };
        Assert.Throws<ArgumentOutOfRangeException>(() => PercentileCalculator.CalculatePercentile(data, -1));
    }

    [Test]
    public void CalculatePercentile_RealTest() {
        double[] data = { 45, 50, 50, 60, 60, 70, 75, 80, 80, 90 };
        Random random = new Random();

        Assert.That(PercentileCalculator.CalculateValuePercentile(data, 65), Is.EqualTo(0.5));
        Assert.That(PercentileCalculator.CalculatePercentile(data, 0.50), Is.EqualTo(65));
        Assert.That(PercentileCalculator.CalculatePercentile(data, 0.80), Is.EqualTo(80));
        Assert.That(PercentileCalculator.CalculatePercentile(data, 0.90), Is.EqualTo(81));

    }

    [Test]
    public void CalculatePercentile_PercentileForConstantSet()
    {
        double[] data = { 2, 2, 2, 2, 2 };
        Random random = new Random();

        Assert.That(PercentileCalculator.CalculatePercentile(data, 0), Is.EqualTo(2));
        Assert.That(PercentileCalculator.CalculatePercentile(data, ((double) random.Next(1, 100))/100), Is.EqualTo(2));
        Assert.That(PercentileCalculator.CalculatePercentile(data, 1.00), Is.EqualTo(2));
    }

    [Test]
    public void CalculatePercentile_PercentileZero_ReturnsMinimumValue()
    {
        double[] data = { 1, 2, 3, 4, 5 };
        int percentile = 0;

        double result = PercentileCalculator.CalculatePercentile(data, percentile);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void CalculatePercentile_PercentileFifty_ReturnsMedianValue()
    {
        double[] data = { 1, 2, 3, 4, 5 };
        double percentile = 0.5;

        double result = PercentileCalculator.CalculatePercentile(data, percentile);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void CalculatePercentile_PercentileHundred_ReturnsMaximumValue()
    {
        double[] data = { 1, 2, 3, 4, 5 };
        double percentile = 1.00;

        double result = PercentileCalculator.CalculatePercentile(data, percentile);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void CalculatePercentile_PercentileInterpolation_ReturnsInterpolatedValue()
    {
        double result = PercentileCalculator.CalculatePercentile(new double[] { 1, 2, 3, 4, 5 }, 0.75);
        Assert.That(result, Is.EqualTo(4.0));
    }
}
