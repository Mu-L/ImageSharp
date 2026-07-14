// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.ImageSharp.PixelFormats.PixelBlenders;

/// <summary>
/// Provides the vector representation used to blend pixels that store associated alpha.
/// </summary>
/// <typeparam name="TPixel">The associated-alpha pixel format.</typeparam>
internal abstract class AssociatedAlphaPixelBlender<TPixel> : PixelBlender<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    // Associated blenders are created only by AssociatedAlphaPixelOperations, so the cached cast establishes the format-specific output boundary once per closed pixel type.
    private static readonly AssociatedAlphaPixelOperations<TPixel> Operations = (AssociatedAlphaPixelOperations<TPixel>)PixelOperations<TPixel>.Instance;

    /// <inheritdoc />
    protected override void ToBlendVector4<TPixelSource>(
        Configuration configuration,
        ReadOnlySpan<TPixelSource> source,
        Span<Vector4> destination)
    {
        // Selecting the source representation once per row avoids a format check for every blended pixel.
        PixelOperations<TPixelSource>.Instance.ToAssociatedScaledVector4(configuration, source, destination);
    }

    /// <inheritdoc />
    protected override Vector4 ToBlendVector4(TPixel source) => source.ToScaledVector4();

    /// <summary>
    /// Converts an associated blend result to the destination pixel representation.
    /// </summary>
    /// <param name="source">The associated blend result.</param>
    /// <returns>The destination pixel.</returns>
    public static TPixel FromBlendVector4(Vector4 source) => Operations.FromAssociatedScaledVector4(source);

    /// <inheritdoc />
    protected override void FromBlendVector4(
        Configuration configuration,
        Span<Vector4> source,
        Span<TPixel> destination)
    {
        Operations.FromAssociatedScaledVector4(configuration, source, destination);
    }
}
