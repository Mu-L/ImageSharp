// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using SixLabors.ImageSharp.Tests.TestUtilities;

namespace SixLabors.ImageSharp.Tests.Processing.Filters;

[Trait("Category", "Processors")]
public class GrayscaleTest : BaseImageOperationsExtensionTest
{
    public static IEnumerable<object[]> ModeTheoryData =
    [
        [new TestType<GrayscaleBt709Processor>(), GrayscaleMode.Bt709]
    ];

    [Theory]
    [MemberData(nameof(ModeTheoryData))]
    public void Grayscale_mode_CorrectProcessor<T>(TestType<T> testType, GrayscaleMode mode)
        where T : IImageProcessor
    {
        this.operations.Grayscale(mode);
        this.Verify<T>();
    }

    [Theory]
    [MemberData(nameof(ModeTheoryData))]
    public void Grayscale_mode_rect_CorrectProcessor<T>(TestType<T> testType, GrayscaleMode mode)
        where T : IImageProcessor
    {
        this.operations.Grayscale(mode, this.rect);
        this.Verify<T>(this.rect);
    }

    [Fact]
    public void Grayscale_rect_CorrectProcessor()
    {
        this.operations.Grayscale(this.rect);
        this.Verify<GrayscaleBt709Processor>(this.rect);
    }
}
