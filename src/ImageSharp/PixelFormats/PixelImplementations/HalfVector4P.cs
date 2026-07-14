// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Packed pixel type containing four associated 16-bit floating-point values.
/// </summary>
/// <remarks>
/// Packed and vector values use associated alpha representation.
/// </remarks>
public partial struct HalfVector4P : IPixel<HalfVector4P>, IPackedVector<ulong>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HalfVector4P"/> struct.
    /// </summary>
    /// <param name="x">The associated x-component.</param>
    /// <param name="y">The associated y-component.</param>
    /// <param name="z">The associated z-component.</param>
    /// <param name="w">The alpha component.</param>
    public HalfVector4P(float x, float y, float z, float w)
        : this(new Vector4(x, y, z, w))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HalfVector4P"/> struct.
    /// </summary>
    /// <param name="vector">The vector containing the associated component values.</param>
    public HalfVector4P(Vector4 vector) => this.PackedValue = Pack(vector);

    /// <inheritdoc />
    public ulong PackedValue { get; set; }

    /// <summary>
    /// Compares two <see cref="HalfVector4P"/> values for equality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public static bool operator ==(HalfVector4P left, HalfVector4P right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="HalfVector4P"/> values for inequality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are not equal.</returns>
    public static bool operator !=(HalfVector4P left, HalfVector4P right) => !left.Equals(right);

    /// <inheritdoc />
    public readonly Rgba32 ToRgba32()
    {
        Vector4 vector = this.ToScaledVector4();
        Numerics.UnPremultiply(ref vector);
        return Rgba32.FromScaledVector4(vector);
    }

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
        HalfTypeHelper.Unpack((ushort)this.PackedValue),
        HalfTypeHelper.Unpack((ushort)(this.PackedValue >> 0x10)),
        HalfTypeHelper.Unpack((ushort)(this.PackedValue >> 0x20)),
        HalfTypeHelper.Unpack((ushort)(this.PackedValue >> 0x30)));

    /// <inheritdoc />
    public static PixelTypeInfo GetPixelTypeInfo()
        => PixelTypeInfo.Create<HalfVector4P>(
            PixelComponentInfo.Create<HalfVector4P>(4, 16, 16, 16, 16),
            PixelColorType.RGB | PixelColorType.Alpha,
            PixelAlphaRepresentation.Associated);

    /// <inheritdoc />
    public static PixelOperations<HalfVector4P> CreatePixelOperations() => new PixelOperations();

    /// <inheritdoc />
    public static HalfVector4P FromScaledVector4(Vector4 source)
    {
        source *= 2F;
        source -= Vector4.One;
        return FromVector4(source);
    }

    /// <inheritdoc />
    public static HalfVector4P FromVector4(Vector4 source) => new() { PackedValue = Pack(source) };

    /// <inheritdoc />
    public static HalfVector4P FromAbgr32(Abgr32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromArgb32(Argb32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromBgra5551(Bgra5551 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromBgr24(Bgr24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromBgra32(Bgra32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromL8(L8 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromL16(L16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromLa16(La16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromLa32(La32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromRgb24(Rgb24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromRgba32(Rgba32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromRgb48(Rgb48 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static HalfVector4P FromRgba64(Rgba64 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is HalfVector4P other && this.Equals(other);

    /// <inheritdoc />
    public readonly bool Equals(HalfVector4P other) => this.PackedValue.Equals(other.PackedValue);

    /// <inheritdoc />
    public override readonly int GetHashCode() => this.PackedValue.GetHashCode();

    /// <inheritdoc />
    public override readonly string ToString()
    {
        Vector4 vector = this.ToVector4();
        return FormattableString.Invariant($"HalfVector4P({vector.X:#0.##}, {vector.Y:#0.##}, {vector.Z:#0.##}, {vector.W:#0.##})");
    }

    /// <summary>
    /// Converts an unassociated scaled vector to associated representation.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated pixel.</returns>
    private static HalfVector4P FromUnassociatedScaledVector4(Vector4 source)
        => FromScaledVector4(Associate(source));

    /// <summary>
    /// Converts an unassociated scaled vector to the associated representation of a half-precision destination.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector4 Associate(Vector4 source)
    {
        // Quantize alpha through the destination's native half representation before RGB is associated with it.
        float nativeAlpha = (source.W * 2F) - 1F;
        source.W = (HalfTypeHelper.Unpack(HalfTypeHelper.Pack(nativeAlpha)) + 1F) / 2F;
        Numerics.Premultiply(ref source);
        return source;
    }

    /// <summary>
    /// Converts unassociated scaled vectors to the associated representation of a half-precision destination.
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

        float nativeAlpha = (alpha * 2F) - 1F;
        float storedAlpha = (HalfTypeHelper.Unpack(HalfTypeHelper.Pack(nativeAlpha)) + 1F) / 2F;

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
    /// Packs native half-precision components into a 64-bit value.
    /// </summary>
    /// <param name="vector">The native component values.</param>
    /// <returns>The packed value.</returns>
    private static ulong Pack(Vector4 vector)
    {
        ulong x = HalfTypeHelper.Pack(vector.X);
        ulong y = (ulong)HalfTypeHelper.Pack(vector.Y) << 0x10;
        ulong z = (ulong)HalfTypeHelper.Pack(vector.Z) << 0x20;
        ulong w = (ulong)HalfTypeHelper.Pack(vector.W) << 0x30;
        return x | y | z | w;
    }
}
