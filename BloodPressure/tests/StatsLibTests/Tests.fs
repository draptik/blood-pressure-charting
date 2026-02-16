namespace StatsLibTests

open Xunit
open Swensen.Unquote

// Reference dataset and expected output are taken from the Apache Commons Math
// LoessInterpolatorTest.testMath296withoutWeights(), where yref is "Output from R".
// Tolerance is 0.02, same as the original test.
module LoessSmootherTests =

  let inline approxEqual (tol: float) (a: float) (b: float) = abs (a - b) <= tol

  [<Fact>]
  let ``LOESS matches R reference on Math-296 dataset (within tolerance)`` () =
    // x/y values
    let x = [|
      0.1
      0.2
      0.3
      0.4
      0.5
      0.6
      0.7
      0.8
      0.9
      1.0
      1.1
      1.2
      1.3
      1.4
      1.5
      1.6
      1.7
      1.8
      1.9
      2.0
    |]

    let y = [|
      0.47
      0.48
      0.55
      0.56
      -0.08
      -0.04
      -0.07
      -0.07
      -0.56
      -0.46
      -0.56
      -0.52
      -3.03
      -3.08
      -3.09
      -3.04
      3.54
      3.46
      3.36
      3.35
    |]

    // Reference output (from R), rounded to .001
    let yRef = [|
      0.461
      0.499
      0.541
      0.308
      0.175
      -0.042
      -0.072
      -0.196
      -0.311
      -0.446
      -0.557
      -1.497
      -2.133
      -3.08
      -3.09
      -0.621
      0.982
      3.449
      3.389
      3.336
    |]

    // Same parameters as the original Java test: bandwidth=0.3, robustnessIters=4, accuracy=1e-12
    let smoother =
      StatsLib.LoessSmoother(bandwidth = 0.3, robustnessIters = 4, accuracy = 1e-12)

    let actual = smoother.Smooth(x, y)

    test <@ actual.Length = x.Length @>

    // Compare elementwise within tolerance (0.02)
    let tol = 0.02

    for i in 0 .. actual.Length - 1 do
      test <@ approxEqual tol yRef[i] actual[i] @>