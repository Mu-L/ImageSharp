// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Benchmarks.Bulk;

[Config(typeof(Config.Short))]
public class ToVector4_Bgra32 : ToVector4<Bgra32>
{
    [Benchmark(Baseline = true)]
    public void PixelOperations_Base()
    {
        new PixelOperations<Bgra32>().ToVector4(
            this.Configuration,
            this.Source.GetSpan(),
            this.Destination.GetSpan());
    }

    // BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8737/25H2/2025Update/HudsonValley2)
    // AMD RYZEN AI MAX+ 395 w/ Radeon 8060S 3.00GHz, 1 CPU, 32 logical and 16 physical cores
    // .NET 8.0.28, X64 RyuJIT x86-64-v4
    //
    // | Method                      | Count | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated |
    // |---------------------------- |------ |------------:|------------:|----------:|------:|--------:|----------:|
    // | PixelOperations_Base        | 64    |    58.30 ns |    20.13 ns |  1.103 ns |  1.00 |    0.02 |         - |
    // | PixelOperations_Specialized | 64    |    58.95 ns |    23.68 ns |  1.298 ns |  1.01 |    0.03 |         - |
    // | PixelOperations_Base        | 256   |   226.95 ns |    84.03 ns |  4.606 ns |  1.00 |    0.02 |         - |
    // | PixelOperations_Specialized | 256   |   229.70 ns |    40.00 ns |  2.192 ns |  1.01 |    0.02 |         - |
    // | PixelOperations_Base        | 2048  | 1,795.42 ns | 1,465.95 ns | 80.354 ns |  1.00 |    0.05 |         - |
    // | PixelOperations_Specialized | 2048  |   291.89 ns |   109.98 ns |  6.028 ns |  0.16 |    0.01 |         - |
}

/// <summary>
/// Measures bulk conversion from premultiplied <see cref="Bgra32P"/> pixels to <see cref="System.Numerics.Vector4"/> values.
/// </summary>
[Config(typeof(Config.Analysis))]
public class ToVector4_Bgra32P : ToVector4<Bgra32P>
{
    /// <summary>
    /// Measures the scalar implementation used as the comparison baseline.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void PixelOperations_Base()
    {
        new AssociatedAlphaPixelOperations<Bgra32P>().ToVector4(
            this.Configuration,
            this.Source.GetSpan(),
            this.Destination.GetSpan());
    }

    // BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8737/25H2/2025Update/HudsonValley2)
    // AMD RYZEN AI MAX+ 395 w/ Radeon 8060S 3.00GHz, 1 CPU, 32 logical and 16 physical cores
    // .NET 8.0.28, X64 RyuJIT x86-64-v4
    //
    // | Method                      | Count | Mean        | Error      | StdDev   | Ratio | Code Size | Allocated |
    // |---------------------------- |------ |------------:|-----------:|---------:|------:|----------:|----------:|
    // | PixelOperations_Base        | 64    |    58.08 ns |  10.122 ns | 0.555 ns |  1.00 |     932 B |         - |
    // | PixelOperations_Specialized | 64    |    79.63 ns |   9.080 ns | 0.498 ns |  1.37 |   3,688 B |         - |
    // | PixelOperations_Base        | 256   |   224.99 ns |  46.742 ns | 2.562 ns |  1.00 |     958 B |         - |
    // | PixelOperations_Specialized | 256   |    99.41 ns |   9.728 ns | 0.533 ns |  0.44 |   3,688 B |         - |
    // | PixelOperations_Base        | 2048  | 1,745.77 ns | 143.244 ns | 7.852 ns |  1.00 |     958 B |         - |
    // | PixelOperations_Specialized | 2048  |   293.00 ns |  44.137 ns | 2.419 ns |  0.17 |   3,704 B |         - |
}
