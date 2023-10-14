// Calculator.cs
using System;
using System.Linq;


public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}

public class PercentileCalculator
{
    public static double CalculatePercentile(double[] data, double percentile)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data array must not be empty or null.");
        }

        if (percentile < 0 || percentile > 100)
        {
            throw new ArgumentOutOfRangeException("Percentile must be between 0 and 100.");
        }

        // First, sort the data array
        Array.Sort(data);

        // Calculate the index of the percentile value
        double index = (percentile / 100) * data.Length;

        // Interpolate the percentile value if it's not an integer index
        if (Math.Floor(index) != index)
        {
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            double lowerValue = data[lowerIndex];
            double upperValue = data[upperIndex];

            return lowerValue + (upperValue - lowerValue) * (index - lowerIndex);
        }
        else
        {
            return data[(int)index];
        }
    }

    public static double CalculateValuePercentile(double[] data, double value)
    {
        Array.Sort(data);
        int index = Array.FindIndex(data, (double x) => x >= value);
        double percentile = (double)index / data.Length;
        return percentile;
    }

}
