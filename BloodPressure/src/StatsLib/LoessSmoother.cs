// Derived from Apache Commons Math "LoessInterpolator" (Apache License 2.0).
// Original source: org/apache/commons/math3/analysis/interpolation/LoessInterpolator.java
// Keep the Apache 2.0 license notice in your project when redistributing.
// See: https://www.apache.org/licenses/LICENSE-2.0

using System.Diagnostics.CodeAnalysis;

namespace StatsLib;

/// <summary>
///   LOWESS/LOESS smoother (local weighted linear regression with tricube kernel),
///   ported from Apache Commons Math LoessInterpolator.smooth(...).
/// </summary>
/// <remarks>
///   Requirements:
///   - x must be strictly increasing (no duplicates)
///   - x, y (and optional weights) must have equal length
///   - all values must be finite (no NaN/Infinity)
/// </remarks>
public sealed class LoessSmoother
{
  private const double DefaultBandwidth = 0.3;
  private const int DefaultRobustnessIters = 2;
  private const double DefaultAccuracy = 1e-12;

  /// <summary>
  ///   Ctor
  /// </summary>
  /// <param name="bandwidth"></param>
  /// <param name="robustnessIters"></param>
  /// <param name="accuracy"></param>
  /// <exception cref="ArgumentOutOfRangeException"></exception>
  public LoessSmoother(
    double bandwidth = DefaultBandwidth,
    int robustnessIters = DefaultRobustnessIters,
    double accuracy = DefaultAccuracy)
  {
    if (bandwidth is < 0d or > 1d)
    {
      throw new ArgumentOutOfRangeException(nameof(bandwidth), "Bandwidth must be in [0, 1].");
    }

    if (robustnessIters < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(robustnessIters), "Robustness iterations must be >= 0.");
    }

    if (!IsFinite(accuracy) || accuracy < 0d)
    {
      throw new ArgumentOutOfRangeException(nameof(accuracy), "Accuracy must be finite and >= 0.");
    }

    Bandwidth = bandwidth;
    RobustnessIters = robustnessIters;
    Accuracy = accuracy;
  }

  /// <summary>
  ///   Fraction of points used in each local regression window (0..1).
  ///   Typical values: 0.25..0.5.
  /// </summary>
  public double Bandwidth { get; }

  /// <summary>
  ///   Number of robustness iterations (>= 0). Typical values: 0..4.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public int RobustnessIters { get; }

  /// <summary>
  ///   Early-stop threshold for robustness iterations.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public double Accuracy { get; }

  /// <summary>
  ///   Smooth values at the original x positions using unit weights.
  /// </summary>
  public double[] Smooth(double[] x, double[] y)
  {
    ArgumentNullException.ThrowIfNull(x);
    ArgumentNullException.ThrowIfNull(y);

    var w = new double[x.Length];
    Array.Fill(w, 1.0);
    return Smooth(x, y, w);
  }

  /// <summary>
  ///   Smooth values at the original x positions using provided point weights.
  /// </summary>
  /// <param name="x">Strictly increasing x values.</param>
  /// <param name="y">y values.</param>
  /// <param name="weights">
  ///   Point weights multiplied into the kernel weights (can contain zeros to exclude points).
  ///   Must be finite and same length as x/y.
  /// </param>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public double[] Smooth(double[] x, double[] y, double[] weights)
  {
    ArgumentNullException.ThrowIfNull(x);
    ArgumentNullException.ThrowIfNull(y);
    ArgumentNullException.ThrowIfNull(weights);

    if (x.Length != y.Length)
    {
      throw new ArgumentException("x and y must have the same length.");
    }

    if (x.Length != weights.Length)
    {
      throw new ArgumentException("weights must have the same length as x and y.");
    }

    var n = x.Length;
    if (n == 0)
    {
      throw new ArgumentException("Input arrays must not be empty.");
    }

    CheckAllFinite(x, nameof(x));
    CheckAllFinite(y, nameof(y));
    CheckAllFinite(weights, nameof(weights));
    CheckStrictlyIncreasing(x);

    if (n == 1)
    {
      return [y[0]];
    }

    if (n == 2)
    {
      return [y[0], y[1]];
    }

    var bandwidthInPoints = (int)(Bandwidth * n);
    if (bandwidthInPoints < 2)
    {
      throw new ArgumentException(nameof(Bandwidth),
        $"Bandwidth too small for n={n}. Need at least 2 points in window; got {bandwidthInPoints}.");
    }

    var result = new double[n];
    var residuals = new double[n];
    var sortedResiduals = new double[n];
    var robustnessWeights = new double[n];
    Array.Fill(robustnessWeights, 1.0);

    // Initial fit + robustness iterations
    for (var iter = 0; iter <= RobustnessIters; iter++)
    {
      // inclusive interval [left, right]
      var left = 0;
      var right = bandwidthInPoints - 1;

      for (var i = 0; i < n; i++)
      {
        var xi = x[i];

        if (i > 0)
        {
          UpdateBandwidthInterval(x, weights, i, ref left, ref right);
        }

        var edge = xi - x[left] > x[right] - xi ? left : right;

        var sumWeights = 0.0;
        var sumX = 0.0;
        var sumXSq = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;

        var denom = Math.Abs(1.0 / (x[edge] - xi));

        for (var k = left; k <= right; k++)
        {
          var xk = x[k];
          var yk = y[k];

          var dist = k < i ? xi - xk : xk - xi;
          var w = Tricube(dist * denom) * robustnessWeights[k] * weights[k];

          var xkw = xk * w;

          sumWeights += w;
          sumX += xkw;
          sumXSq += xk * xkw;
          sumY += yk * w;
          sumXY += yk * xkw;
        }

        var meanX = sumX / sumWeights;
        var meanY = sumY / sumWeights;
        var meanXY = sumXY / sumWeights;
        var meanXSq = sumXSq / sumWeights;

        var beta = Math.Sqrt(Math.Abs(meanXSq - meanX * meanX)) < Accuracy
          ? 0.0
          : (meanXY - meanX * meanY) / (meanXSq - meanX * meanX);

        var alpha = meanY - beta * meanX;
        result[i] = beta * xi + alpha;
        residuals[i] = Math.Abs(y[i] - result[i]);
      }

      if (iter == RobustnessIters)
      {
        break;
      }

      // Recompute robustness weights from median residual
      Array.Copy(residuals, sortedResiduals, n);
      Array.Sort(sortedResiduals);
      var medianResidual = sortedResiduals[n / 2];

      if (Math.Abs(medianResidual) < Accuracy)
      {
        break;
      }

      for (var i = 0; i < n; i++)
      {
        var arg = residuals[i] / (6.0 * medianResidual);
        if (arg >= 1.0)
        {
          robustnessWeights[i] = 0.0;
        }
        else
        {
          var w = 1.0 - arg * arg;
          robustnessWeights[i] = w * w;
        }
      }
    }

    return result;
  }

  private static void UpdateBandwidthInterval(double[] x, double[] weights, int i, ref int left, ref int right)
  {
    var nextRight = NextNonzero(weights, right);
    if (nextRight < x.Length && x[nextRight] - x[i] < x[i] - x[left])
    {
      var nextLeft = NextNonzero(weights, left);
      left = nextLeft;
      right = nextRight;
    }
  }

  private static int NextNonzero(double[] weights, int i)
  {
    var j = i + 1;
    while (j < weights.Length && weights[j] == 0.0)
    {
      j++;
    }

    return j;
  }

  private static double Tricube(double x)
  {
    var absX = Math.Abs(x);
    if (absX >= 1.0)
    {
      return 0.0;
    }

    var tmp = 1.0 - absX * absX * absX;
    return tmp * tmp * tmp;
  }

  private static void CheckStrictlyIncreasing(double[] x)
  {
    for (var i = 1; i < x.Length; i++)
    {
      if (!(x[i] > x[i - 1]))
      {
        throw new ArgumentException("x must be strictly increasing (no duplicates).", nameof(x));
      }
    }
  }

  private static void CheckAllFinite(double[] values, string paramName)
  {
    if (values.Any(t => !IsFinite(t)))
    {
      throw new ArgumentException($"All elements of {paramName} must be finite (no NaN/Infinity).", paramName);
    }
  }

  private static bool IsFinite(double v)
  {
    return double.IsFinite(v);
  }
}