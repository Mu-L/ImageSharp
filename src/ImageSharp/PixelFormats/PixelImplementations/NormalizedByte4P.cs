// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Packed pixel type containing four associated 8-bit signed normalized values ranging from -1 to 1.
/// </summary>
/// <remarks>
/// Packed and vector values use associated alpha representation.
/// </remarks>
public partial struct NormalizedByte4P : IPixel<NormalizedByte4P>, IPackedVector<uint>
{
    private const float MaxPos = 127F;
    private const float ScaledMagnitude = MaxPos * 2F;
    private static readonly Vector4 Half = Vector128.Create(MaxPos).AsVector4();
    private static readonly Vector4 MinusOne = Vector128.Create(-1F).AsVector4();

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizedByte4P"/> struct.
    /// </summary>
    /// <param name="x">The associated x-component.</param>
    /// <param name="y">The associated y-component.</param>
    /// <param name="z">The associated z-component.</param>
    /// <param name="w">The alpha component.</param>
    public NormalizedByte4P(float x, float y, float z, float w)
        : this(new Vector4(x, y, z, w))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizedByte4P"/> struct.
    /// </summary>
    /// <param name="vector">The vector containing the associated component values.</param>
    public NormalizedByte4P(Vector4 vector) => this.PackedValue = Pack(vector);

    /// <inheritdoc />
    public uint PackedValue { get; set; }

    /// <summary>
    /// Compares two <see cref="NormalizedByte4P"/> values for equality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public static bool operator ==(NormalizedByte4P left, NormalizedByte4P right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="NormalizedByte4P"/> values for inequality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are not equal.</returns>
    public static bool operator !=(NormalizedByte4P left, NormalizedByte4P right) => !left.Equals(right);

    /// <inheritdoc />
    public readonly Rgba32 ToRgba32() => Rgba32.FromScaledVector4(ToUnassociatedScaledVector4(this));

    /// <inheritdoc />
    public readonly Vector4 ToScaledVector4()
    {
        Vector4 scaled = this.ToVector4();
        scaled += Vector4.One;
        scaled /= 2F;
        return scaled;
    }

    /// <inheritdoc />
    public readonly Vector4 ToVector4() => new(
        (sbyte)((this.PackedValue >> 0) & 0xFF) / MaxPos,
        (sbyte)((this.PackedValue >> 8) & 0xFF) / MaxPos,
        (sbyte)((this.PackedValue >> 16) & 0xFF) / MaxPos,
        (sbyte)((this.PackedValue >> 24) & 0xFF) / MaxPos);

    /// <inheritdoc />
    public static PixelTypeInfo GetPixelTypeInfo()
        => PixelTypeInfo.Create<NormalizedByte4P>(
            PixelComponentInfo.Create<NormalizedByte4P>(4, 8, 8, 8, 8),
            PixelColorType.RGB | PixelColorType.Alpha,
            PixelAlphaRepresentation.Associated);

    /// <inheritdoc />
    public static PixelOperations<NormalizedByte4P> CreatePixelOperations() => new PixelOperations();

    /// <inheritdoc />
    public static NormalizedByte4P FromScaledVector4(Vector4 source)
    {
        source *= 2F;
        source -= Vector4.One;
        return FromVector4(source);
    }

    /// <inheritdoc />
    public static NormalizedByte4P FromVector4(Vector4 source) => new() { PackedValue = Pack(source) };

    /// <inheritdoc />
    public static NormalizedByte4P FromAbgr32(Abgr32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromArgb32(Argb32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromBgra5551(Bgra5551 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromBgr24(Bgr24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromBgra32(Bgra32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromL8(L8 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromL16(L16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromLa16(La16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromLa32(La32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromRgb24(Rgb24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromRgba32(Rgba32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromRgb48(Rgb48 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static NormalizedByte4P FromRgba64(Rgba64 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is NormalizedByte4P other && this.Equals(other);

    /// <inheritdoc />
    public readonly bool Equals(NormalizedByte4P other) => this.PackedValue.Equals(other.PackedValue);

    /// <inheritdoc />
    public override readonly int GetHashCode() => this.PackedValue.GetHashCode();

    /// <inheritdoc />
    public override readonly string ToString()
    {
        Vector4 vector = this.ToVector4();
        return FormattableString.Invariant($"NormalizedByte4P({vector.X:#0.##}, {vector.Y:#0.##}, {vector.Z:#0.##}, {vector.W:#0.##})");
    }

    /// <summary>
    /// Converts an unassociated scaled vector to associated representation.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated pixel.</returns>
    private static NormalizedByte4P FromUnassociatedScaledVector4(Vector4 source) => FromScaledVector4(Associate(source));

    /// <summary>
    /// Converts an unassociated scaled vector to the associated representation of a signed-normalized-byte destination.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 Associate(Vector4 source)
    {
        // Reproduce the signed-normalized packer's alpha quantization, then associate RGB with the exact alpha that will be stored.
        float nativeAlpha = Numerics.Clamp((source.W * 2F) - 1F, -1F, 1F);
        float storedAlpha = MathF.Round(nativeAlpha * MaxPos);
        source.W = (storedAlpha + MaxPos) / ScaledMagnitude;
        Numerics.Premultiply(ref source);
        return source;
    }

    /// <summary>
    /// Converts the stored associated components to an unassociated scaled vector.
    /// </summary>
    /// <param name="source">The associated pixel.</param>
    /// <returns>The unassociated scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 ToUnassociatedScaledVector4(NormalizedByte4P source)
    {
        // Offset signed storage into exact nonnegative byte magnitudes before division so the quotient retains the destination's byte-rounding midpoint.
        Vector4 vector = new(
            (sbyte)(source.PackedValue >> 0) + MaxPos,
            (sbyte)(source.PackedValue >> 8) + MaxPos,
            (sbyte)(source.PackedValue >> 16) + MaxPos,
            (sbyte)(source.PackedValue >> 24) + MaxPos);

        if (vector.W == 0F)
        {
            // Numerics.UnPremultiply preserves RGB when alpha is zero. Normalize the stored components because they already are the unassociated value in this case.
            return vector / ScaledMagnitude;
        }

        Numerics.UnPremultiply(ref vector);
        vector.W /= ScaledMagnitude;
        return vector;
    }

    /// <summary>
    /// Converts unassociated scaled vectors to the associated representation of a signed-normalized-byte destination.
    /// </summary>
    /// <param name="source">The vectors to convert in place.</param>
    private static void Associate(Span<Vector4> source)
    {
        ref Vector4 sourceBase = ref MemoryMarshal.GetReference(source);

        for (nuint i = 0; i < (uint)source.Length; i++)
        {
            Unsafe.Add(ref sourceBase, i) = Associate(Unsafe.Add(ref sourceBase, i));
        }
    }

    /// <summary>
    /// Reassociates a scaled vector with the alpha value the destination stores.
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

        float nativeAlpha = Numerics.Clamp((alpha * 2F) - 1F, -1F, 1F);
        float storedAlpha = (MathF.Round(nativeAlpha * MaxPos) + MaxPos) / ScaledMagnitude;

        // Associated RGB scales by the same ratio as alpha. Applying that ratio directly avoids the extra division and multiplication of an unpremultiply/premultiply round trip and preserves exact midpoints when alpha needs no quantization.
        source *= storedAlpha / alpha;
        source.W = storedAlpha;
        return source;
    }

    /// <summary>
    /// Reassociates scaled vectors with the alpha values the destination stores.
    /// </summary>
    /// <param name="source">The vectors to convert in place.</param>
    private static void Reassociate(Span<Vector4> source)
    {
        ref Vector4 sourceBase = ref MemoryMarshal.GetReference(source);

        for (nuint i = 0; i < (uint)source.Length; i++)
        {
            Unsafe.Add(ref sourceBase, i) = Reassociate(Unsafe.Add(ref sourceBase, i));
        }
    }

    /// <summary>
    /// Packs native signed normalized components into a 32-bit value.
    /// </summary>
    /// <param name="vector">The native component values.</param>
    /// <returns>The packed value.</returns>
    private static uint Pack(Vector4 vector)
    {
        vector = Numerics.Clamp(vector, MinusOne, Vector4.One) * Half;

        uint byte4 = ((uint)Convert.ToInt16(MathF.Round(vector.X)) & 0xFF) << 0;
        uint byte3 = ((uint)Convert.ToInt16(MathF.Round(vector.Y)) & 0xFF) << 8;
        uint byte2 = ((uint)Convert.ToInt16(MathF.Round(vector.Z)) & 0xFF) << 16;
        uint byte1 = ((uint)Convert.ToInt16(MathF.Round(vector.W)) & 0xFF) << 24;

        return byte4 | byte3 | byte2 | byte1;
    }
}
