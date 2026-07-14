// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats.PixelBlenders;

namespace SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Provides bulk operations for pixel formats that store associated alpha.
/// </summary>
/// <typeparam name="TPixel">The associated-alpha pixel format.</typeparam>
internal class AssociatedAlphaPixelOperations<TPixel> : PixelOperations<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    /// <inheritdoc />
    public override PixelBlender<TPixel> GetPixelBlender(PixelColorBlendingMode colorMode, PixelAlphaCompositionMode alphaMode)
        => AssociatedAlphaPixelBlenders<TPixel>.GetPixelBlender(colorMode, alphaMode);

    /// <inheritdoc />
    internal override Vector4 ToUnassociatedScaledVector4(TPixel source)
    {
        Vector4 vector = source.ToScaledVector4();
        Numerics.UnPremultiply(ref vector);
        return vector;
    }

    /// <inheritdoc />
    internal override TPixel FromUnassociatedScaledVector4(Vector4 source) => TPixel.FromScaledVector4(Associate(source));

    /// <summary>
    /// Converts an associated scaled vector to a destination pixel after associating RGB with the alpha value the destination stores.
    /// </summary>
    /// <param name="source">The associated scaled vector.</param>
    /// <returns>The destination pixel.</returns>
    public virtual TPixel FromAssociatedScaledVector4(Vector4 source) => TPixel.FromScaledVector4(Reassociate(source));

    /// <inheritdoc />
    internal override void ToUnassociatedScaledVector4(
        Configuration configuration,
        ReadOnlySpan<TPixel> source,
        Span<Vector4> destination)
    {
        this.ToVector4(configuration, source, destination, PixelConversionModifiers.Scale);
        Numerics.UnPremultiply(destination[..source.Length]);
    }

    /// <inheritdoc />
    internal override void ToAssociatedScaledVector4(
        Configuration configuration,
        ReadOnlySpan<TPixel> source,
        Span<Vector4> destination)
        => this.ToVector4(configuration, source, destination, PixelConversionModifiers.Scale);

    /// <inheritdoc />
    internal override void FromUnassociatedScaledVector4(
        Configuration configuration,
        Span<Vector4> source,
        Span<TPixel> destination)
    {
        source = source[..destination.Length];

        for (int i = 0; i < source.Length; i++)
        {
            source[i] = Associate(source[i]);
        }

        this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
    }

    /// <summary>
    /// Converts associated scaled vectors to destination pixels after associating RGB with the alpha values the destination stores.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="source">The associated scaled vectors.</param>
    /// <param name="destination">The destination pixels.</param>
    public virtual void FromAssociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<TPixel> destination)
    {
        source = source[..destination.Length];

        for (int i = 0; i < source.Length; i++)
        {
            source[i] = Reassociate(source[i]);
        }

        this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
    }

    /// <inheritdoc />
    public override void From<TSourcePixel>(
        Configuration configuration,
        ReadOnlySpan<TSourcePixel> source,
        Span<TPixel> destination)
    {
        const int sliceLength = 1024;
        int numberOfSlices = source.Length / sliceLength;

        using IMemoryOwner<Vector4> tempVectors = configuration.MemoryAllocator.Allocate<Vector4>(sliceLength);
        Span<Vector4> vectorSpan = tempVectors.GetSpan();

        // Convert through unassociated vectors so the destination operation can quantize alpha to its own storage before associating RGB.
        for (int i = 0; i < numberOfSlices; i++)
        {
            int start = i * sliceLength;
            ReadOnlySpan<TSourcePixel> sourceSlice = source.Slice(start, sliceLength);
            Span<TPixel> destinationSlice = destination.Slice(start, sliceLength);
            PixelOperations<TSourcePixel>.Instance.ToUnassociatedScaledVector4(configuration, sourceSlice, vectorSpan);
            this.FromUnassociatedScaledVector4(configuration, vectorSpan, destinationSlice);
        }

        int endOfCompleteSlices = numberOfSlices * sliceLength;
        int remainder = source.Length - endOfCompleteSlices;

        if (remainder > 0)
        {
            ReadOnlySpan<TSourcePixel> sourceSlice = source[endOfCompleteSlices..];
            Span<TPixel> destinationSlice = destination[endOfCompleteSlices..];
            vectorSpan = vectorSpan[..remainder];
            PixelOperations<TSourcePixel>.Instance.ToUnassociatedScaledVector4(configuration, sourceSlice, vectorSpan);
            this.FromUnassociatedScaledVector4(configuration, vectorSpan, destinationSlice);
        }
    }

    /// <inheritdoc />
    public override void FromVector4Destructive(
        Configuration configuration,
        Span<Vector4> sourceVectors,
        Span<TPixel> destination,
        PixelConversionModifiers modifiers)
        => base.FromVector4Destructive(configuration, sourceVectors, destination, modifiers.Remove(PixelConversionModifiers.Premultiply));

    /// <inheritdoc />
    public override void ToVector4(
        Configuration configuration,
        ReadOnlySpan<TPixel> source,
        Span<Vector4> destinationVectors,
        PixelConversionModifiers modifiers)
        => base.ToVector4(configuration, source, destinationVectors, modifiers.Remove(PixelConversionModifiers.Premultiply));

    /// <inheritdoc />
    public override void ToArgb32(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Argb32> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToAbgr32(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Abgr32> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToBgr24(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Bgr24> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToBgra32(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Bgra32> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToL8(Configuration configuration, ReadOnlySpan<TPixel> source, Span<L8> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToL16(Configuration configuration, ReadOnlySpan<TPixel> source, Span<L16> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToLa16(Configuration configuration, ReadOnlySpan<TPixel> source, Span<La16> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToLa32(Configuration configuration, ReadOnlySpan<TPixel> source, Span<La32> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToRgb24(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Rgb24> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToRgba32(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Rgba32> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToRgb48(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Rgb48> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToRgba64(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Rgba64> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <inheritdoc />
    public override void ToBgra5551(Configuration configuration, ReadOnlySpan<TPixel> source, Span<Bgra5551> destination)
        => this.ConvertToUnassociated(configuration, source, destination);

    /// <summary>
    /// Converts associated source pixels to an unassociated destination format.
    /// </summary>
    /// <typeparam name="TDestinationPixel">The destination pixel format.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="source">The source pixels.</param>
    /// <param name="destination">The destination pixels.</param>
    private void ConvertToUnassociated<TDestinationPixel>(
        Configuration configuration,
        ReadOnlySpan<TPixel> source,
        Span<TDestinationPixel> destination)
        where TDestinationPixel : unmanaged, IPixel<TDestinationPixel>
    {
        Guard.NotNull(configuration, nameof(configuration));
        Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

        ref TPixel sourceBase = ref MemoryMarshal.GetReference(source);
        ref TDestinationPixel destinationBase = ref MemoryMarshal.GetReference(destination);

        for (nuint i = 0; i < (uint)source.Length; i++)
        {
            Vector4 vector = this.ToUnassociatedScaledVector4(Unsafe.Add(ref sourceBase, i));
            Unsafe.Add(ref destinationBase, i) = TDestinationPixel.FromScaledVector4(vector);
        }
    }

    /// <summary>
    /// Converts an unassociated scaled vector to the associated representation of the destination pixel type.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 Associate(Vector4 source)
    {
        // Round-trip alpha through TPixel so the generic fallback associates RGB with the value the destination actually stores.
        source.W = TPixel.FromScaledVector4(new Vector4(0, 0, 0, source.W)).ToScaledVector4().W;
        Numerics.Premultiply(ref source);
        return source;
    }

    /// <summary>
    /// Reassociates a scaled vector with the alpha value the destination pixel can store.
    /// </summary>
    /// <param name="source">The associated scaled vector.</param>
    /// <returns>The reassociated scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 Reassociate(Vector4 source)
    {
        float alpha = source.W;

        if (alpha == 0)
        {
            return Vector4.Zero;
        }

        float storedAlpha = TPixel.FromScaledVector4(new Vector4(0, 0, 0, alpha)).ToScaledVector4().W;

        // Associated RGB scales by the same ratio as alpha. Applying that ratio directly avoids the extra division and multiplication of an unpremultiply/premultiply round trip and preserves exact midpoints when alpha needs no quantization.
        source *= storedAlpha / alpha;
        source.W = storedAlpha;
        return source;
    }
}
