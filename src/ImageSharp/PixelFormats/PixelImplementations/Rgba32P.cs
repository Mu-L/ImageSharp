// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using SixLabors.ImageSharp.PixelFormats.Utils;

namespace SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Packed pixel type containing associated red, green, blue, and alpha components as 8-bit unsigned normalized values.
/// Components are stored in red, green, blue, and alpha order from least to most significant byte.
/// </summary>
/// <remarks>
/// Component, packed, and vector values use associated alpha representation.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public partial struct Rgba32P : IPixel<Rgba32P>, IPackedVector<uint>
{
    private const float ByteScale = 1F / byte.MaxValue;

    private static readonly Vector4 Half = new(0.5F);
    private static readonly Vector4 MaxBytes = new(byte.MaxValue);

    /// <summary>
    /// Gets or sets the associated red component.
    /// </summary>
    public byte R;

    /// <summary>
    /// Gets or sets the associated green component.
    /// </summary>
    public byte G;

    /// <summary>
    /// Gets or sets the associated blue component.
    /// </summary>
    public byte B;

    /// <summary>
    /// Gets or sets the alpha component.
    /// </summary>
    public byte A;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from associated components.
    /// </summary>
    /// <param name="r">The associated red component.</param>
    /// <param name="g">The associated green component.</param>
    /// <param name="b">The associated blue component.</param>
    public Rgba32P(byte r, byte g, byte b)
        : this(r, g, b, byte.MaxValue)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from associated components.
    /// </summary>
    /// <param name="r">The associated red component.</param>
    /// <param name="g">The associated green component.</param>
    /// <param name="b">The associated blue component.</param>
    /// <param name="a">The alpha component.</param>
    public Rgba32P(byte r, byte g, byte b, byte a)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from associated components.
    /// </summary>
    /// <param name="r">The associated red component.</param>
    /// <param name="g">The associated green component.</param>
    /// <param name="b">The associated blue component.</param>
    public Rgba32P(float r, float g, float b)
        : this(new Vector4(r, g, b, 1F))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from associated components.
    /// </summary>
    /// <param name="r">The associated red component.</param>
    /// <param name="g">The associated green component.</param>
    /// <param name="b">The associated blue component.</param>
    /// <param name="a">The alpha component.</param>
    public Rgba32P(float r, float g, float b, float a)
        : this(new Vector4(r, g, b, a))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from an associated vector.
    /// </summary>
    /// <param name="vector">The associated vector.</param>
    public Rgba32P(Vector3 vector)
        : this(new Vector4(vector, 1F))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from an associated vector.
    /// </summary>
    /// <param name="vector">The associated vector.</param>
    public Rgba32P(Vector4 vector)
        : this() => this = FromScaledVector4(vector);

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba32P"/> struct from a packed associated value.
    /// </summary>
    /// <param name="packed">The packed associated value.</param>
    public Rgba32P(uint packed)
        : this() => this.PackedValue = packed;

    /// <inheritdoc />
    public uint PackedValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Unsafe.As<Rgba32P, uint>(ref Unsafe.AsRef(in this));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Unsafe.As<Rgba32P, uint>(ref this) = value;
    }

    /// <summary>
    /// Compares two <see cref="Rgba32P"/> values for equality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public static bool operator ==(Rgba32P left, Rgba32P right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="Rgba32P"/> values for inequality.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the values are not equal.</returns>
    public static bool operator !=(Rgba32P left, Rgba32P right) => !left.Equals(right);

    /// <inheritdoc />
    public readonly Rgba32 ToRgba32()
        => Rgba32.FromScaledVector4(Vector4Converters.AssociatedRgbaCompatible.ToUnassociatedVector4(this.R, this.G, this.B, this.A));

    /// <inheritdoc />
    public readonly Vector4 ToScaledVector4() => new Vector4(this.R, this.G, this.B, this.A) * ByteScale;

    /// <inheritdoc />
    public readonly Vector4 ToVector4() => this.ToScaledVector4();

    /// <inheritdoc />
    public static PixelTypeInfo GetPixelTypeInfo()
        => PixelTypeInfo.Create<Rgba32P>(
            PixelComponentInfo.Create<Rgba32P>(4, 8, 8, 8, 8),
            PixelColorType.RGB | PixelColorType.Alpha,
            PixelAlphaRepresentation.Associated);

    /// <inheritdoc />
    public static PixelOperations<Rgba32P> CreatePixelOperations() => new PixelOperations();

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rgba32P FromScaledVector4(Vector4 source) => Pack(source);

    /// <inheritdoc />
    public static Rgba32P FromVector4(Vector4 source) => FromScaledVector4(source);

    /// <inheritdoc />
    public static Rgba32P FromAbgr32(Abgr32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromArgb32(Argb32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromBgra5551(Bgra5551 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromBgr24(Bgr24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromBgra32(Bgra32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromL8(L8 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromL16(L16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromLa16(La16 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromLa32(La32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromRgb24(Rgb24 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromRgba32(Rgba32 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromRgb48(Rgb48 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public static Rgba32P FromRgba64(Rgba64 source) => FromUnassociatedScaledVector4(source.ToScaledVector4());

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is Rgba32P other && this.Equals(other);

    /// <inheritdoc />
    public readonly bool Equals(Rgba32P other) => this.PackedValue.Equals(other.PackedValue);

    /// <inheritdoc />
    public override readonly int GetHashCode() => this.PackedValue.GetHashCode();

    /// <inheritdoc />
    public override readonly string ToString() => $"Rgba32P({this.R}, {this.G}, {this.B}, {this.A})";

    /// <summary>
    /// Converts an unassociated scaled vector to associated representation.
    /// </summary>
    /// <param name="source">The unassociated scaled vector.</param>
    /// <returns>The associated pixel.</returns>
    private static Rgba32P FromUnassociatedScaledVector4(Vector4 source)
        => FromScaledVector4(Vector4Converters.AssociatedRgbaCompatible.Associate(source));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rgba32P Pack(Vector4 vector)
    {
        vector *= MaxBytes;
        vector += Half;
        vector = Numerics.Clamp(vector, Vector4.Zero, MaxBytes);

        Vector128<byte> result = Vector128.ConvertToInt32(vector.AsVector128()).AsByte();
        return new Rgba32P(result.GetElement(0), result.GetElement(4), result.GetElement(8), result.GetElement(12));
    }
}
