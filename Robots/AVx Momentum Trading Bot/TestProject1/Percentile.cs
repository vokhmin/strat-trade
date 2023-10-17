// Percentile.cs
using System;
using System.Linq;

class Percentile
{
    private double[] data;

    public Percentile(double[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data array must not be empty or null.");
        }
        this.data = data;
        // First, sort the data array
        Array.Sort(data);
    }

    /**
     * Calculates the Percentile (0.0 .. 1.0) for the Value (double) by data.
     */
    public double forValue(double value)
    {
        int index = Array.FindIndex(data, (double x) => x >= value);
        double percentile = (double)index / data.Length;
        return percentile;
    }

    /**
      * Calculates the Value (double) for the required Percentile (0.0 .. 1.0).
      */
    public double valueFor(double percentile)
    {
        if (percentile < 0.0 || percentile > 1.0)
        {
            throw new ArgumentOutOfRangeException("Percentile must be between 0.00 and 1.00");
        }
        // Calculate the index of the percentile value
        double index = percentile * (data.Length - 1);
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

}