// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.PixelFormats.PixelBlenders;

namespace SixLabors.ImageSharp.Tests.PixelFormats;

/// <summary>
/// Verifies the shared contract for pixel formats that store associated alpha.
/// </summary>
/// <typeparam name="TPixel">The associated-alpha pixel format.</typeparam>
[Trait("Category", "PixelFormats")]
public abstract class AssociatedAlphaPixelTests<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    private static readonly ApproximateFloatComparer VectorComparer = new(.005F);

    /// <summary>
    /// Gets the color channels described by the pixel format.
    /// </summary>
    protected virtual PixelColorType ExpectedColorType => PixelColorType.RGB | PixelColorType.Alpha;

    [Fact]
    public void PixelInformationDescribesAssociatedAlpha()
    {
        PixelTypeInfo info = TPixel.GetPixelTypeInfo();
        PixelComponentInfo componentInfo = info.ComponentInfo.Value;
        int expectedComponentPrecision = (Unsafe.SizeOf<TPixel>() * 8) / 4;

        Assert.Equal(Unsafe.SizeOf<TPixel>() * 8, info.BitsPerPixel);
        Assert.Equal(PixelAlphaRepresentation.Associated, info.AlphaRepresentation);
        Assert.Equal(this.ExpectedColorType, info.ColorType);
        Assert.Equal(4, componentInfo.ComponentCount);
        Assert.Equal(0, componentInfo.Padding);

        for (int i = 0; i < componentInfo.ComponentCount; i++)
        {
            Assert.Equal(expectedComponentPrecision, componentInfo.GetComponentPrecision(i));
        }
    }

    [Fact]
    public void ScaledVectorConversionsUseAssociatedComponents()
    {
        Vector4 associated = new(.25F, .125F, .0625F, .5F);

        TPixel pixel = TPixel.FromScaledVector4(associated);

        Assert.Equal(associated, pixel.ToScaledVector4(), VectorComparer);
    }

    [Fact]
    public void FromRgba32AssociatesColorComponents()
    {
        Rgba32 source = new(192, 128, 64, 128);
        Vector4 expected = source.ToScaledVector4();
        Numerics.Premultiply(ref expected);

        TPixel pixel = TPixel.FromRgba32(source);

        Assert.Equal(expected, pixel.ToScaledVector4(), VectorComparer);
    }

    [Fact]
    public void ToRgba32ReturnsUnassociatedColorComponents()
    {
        Rgba32 expected = new(192, 128, 64, 128);
        TPixel pixel = TPixel.FromRgba32(expected);

        Rgba32 actual = pixel.ToRgba32();

        AssertRgba32Equal(expected, actual, 3);
    }

    [Fact]
    public void ColorConversionsPreserveUnassociatedColor()
    {
        Rgba32 expected = new(192, 128, 64, 128);
        TPixel source = TPixel.FromRgba32(expected);

        Color color = Color.FromPixel(source);
        Rgba32 actual = color.ToPixel<Rgba32>();
        TPixel roundTrip = color.ToPixel<TPixel>();

        AssertRgba32Equal(expected, actual, 3);
        Assert.Equal(source.ToScaledVector4(), roundTrip.ToScaledVector4(), VectorComparer);
    }

    [Fact]
    public void ScalarBlendingUsesUnassociatedColorValues()
    {
        TPixel background = TPixel.FromRgba32(new Rgba32(200, 40, 80, 160));
        TPixel source = TPixel.FromRgba32(new Rgba32(20, 180, 100, 96));
        PixelBlender<TPixel> associatedBlender = PixelOperations<TPixel>.Instance.GetPixelBlender(PixelColorBlendingMode.Normal, PixelAlphaCompositionMode.SrcOver);
        PixelBlender<Rgba32> unassociatedBlender = new DefaultPixelBlenders<Rgba32>.NormalSrcOver();

        Rgba32 expected = unassociatedBlender.Blend(background.ToRgba32(), source.ToRgba32(), .75F);
        Rgba32 actual = associatedBlender.Blend(background, source, .75F).ToRgba32();

        AssertRgba32Equal(expected, actual, 4);
    }

    private static void AssertRgba32Equal(Rgba32 expected, Rgba32 actual, int tolerance)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, tolerance);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, tolerance);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, tolerance);
        Assert.InRange(Math.Abs(expected.A - actual.A), 0, tolerance);
    }
}

/// <summary>
/// Tests the <see cref="Rgba32P"/> pixel format.
/// </summary>
public class Rgba32PTests : AssociatedAlphaPixelTests<Rgba32P>
{
    [Fact]
    public void ByteLayoutAndPackedValue()
    {
        Rgba32P[] pixels = [new(1, 2, 3, 4)];

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
        Assert.Equal(0x04030201U, pixels[0].PackedValue);
    }
}

/// <summary>
/// Tests the <see cref="Bgra32P"/> pixel format.
/// </summary>
public class Bgra32PTests : AssociatedAlphaPixelTests<Bgra32P>
{
    /// <inheritdoc />
    protected override PixelColorType ExpectedColorType => PixelColorType.BGR | PixelColorType.Alpha;

    [Fact]
    public void ByteLayoutAndPackedValue()
    {
        Bgra32P[] pixels = [new(1, 2, 3, 4)];

        Assert.Equal(new byte[] { 3, 2, 1, 4 }, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
        Assert.Equal(0x04010203U, pixels[0].PackedValue);
    }
}

/// <summary>
/// Tests the <see cref="Argb32P"/> pixel format.
/// </summary>
public class Argb32PTests : AssociatedAlphaPixelTests<Argb32P>
{
    [Fact]
    public void ByteLayoutAndPackedValue()
    {
        Argb32P[] pixels = [new(1, 2, 3, 4)];

        Assert.Equal(new byte[] { 4, 1, 2, 3 }, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
        Assert.Equal(0x03020104U, pixels[0].PackedValue);
    }
}

/// <summary>
/// Tests the <see cref="Abgr32P"/> pixel format.
/// </summary>
public class Abgr32PTests : AssociatedAlphaPixelTests<Abgr32P>
{
    /// <inheritdoc />
    protected override PixelColorType ExpectedColorType => PixelColorType.BGR | PixelColorType.Alpha;

    [Fact]
    public void ByteLayoutAndPackedValue()
    {
        Abgr32P[] pixels = [new(1, 2, 3, 4)];

        Assert.Equal(new byte[] { 4, 3, 2, 1 }, MemoryMarshal.AsBytes(pixels.AsSpan()).ToArray());
        Assert.Equal(0x01020304U, pixels[0].PackedValue);
    }
}

/// <summary>
/// Tests the <see cref="NormalizedByte4P"/> pixel format.
/// </summary>
public class NormalizedByte4PTests : AssociatedAlphaPixelTests<NormalizedByte4P>
{
    [Fact]
    public void PackedValueMatchesNormalizedByte4ForAssociatedVector()
    {
        Vector4 associated = new(.1F, -.3F, .5F, -.7F);

        Assert.Equal(new NormalizedByte4(associated).PackedValue, new NormalizedByte4P(associated).PackedValue);
    }
}

/// <summary>
/// Tests the <see cref="HalfVector4P"/> pixel format.
/// </summary>
public class HalfVector4PTests : AssociatedAlphaPixelTests<HalfVector4P>
{
    [Fact]
    public void PackedValueMatchesHalfVector4ForAssociatedVector()
    {
        Vector4 associated = new(.1F, -.3F, .5F, -.7F);

        Assert.Equal(new HalfVector4(associated).PackedValue, new HalfVector4P(associated).PackedValue);
    }
}

/// <summary>
/// Tests conversion between associated-alpha packed byte layouts.
/// </summary>
public class AssociatedAlphaPackedPixelConversionTests
{
    [Fact]
    public void Rgba32PToBgra32PRoundTripIsLossless() => AssertLosslessRoundTrip<Bgra32P>();

    [Fact]
    public void Rgba32PToArgb32PRoundTripIsLossless() => AssertLosslessRoundTrip<Argb32P>();

    [Fact]
    public void Rgba32PToAbgr32PRoundTripIsLossless() => AssertLosslessRoundTrip<Abgr32P>();

    [Fact]
    public void Rgba32PScalarAndBulkAssociatedVectorsAreEqual() => AssertScalarAndBulkAssociatedVectorsAreEqual((r, g, b, a) => new Rgba32P(r, g, b, a));

    [Fact]
    public void Bgra32PScalarAndBulkAssociatedVectorsAreEqual() => AssertScalarAndBulkAssociatedVectorsAreEqual((r, g, b, a) => new Bgra32P(r, g, b, a));

    [Fact]
    public void Argb32PScalarAndBulkAssociatedVectorsAreEqual() => AssertScalarAndBulkAssociatedVectorsAreEqual((r, g, b, a) => new Argb32P(r, g, b, a));

    [Fact]
    public void Abgr32PScalarAndBulkAssociatedVectorsAreEqual() => AssertScalarAndBulkAssociatedVectorsAreEqual((r, g, b, a) => new Abgr32P(r, g, b, a));

    [Fact]
    public void Rgba32PScalarAndBulkFromAssociatedVectorsAreEqual() => AssertScalarAndBulkFromAssociatedVectorsAreEqual<Rgba32P>();

    [Fact]
    public void Bgra32PScalarAndBulkFromAssociatedVectorsAreEqual() => AssertScalarAndBulkFromAssociatedVectorsAreEqual<Bgra32P>();

    [Fact]
    public void Argb32PScalarAndBulkFromAssociatedVectorsAreEqual() => AssertScalarAndBulkFromAssociatedVectorsAreEqual<Argb32P>();

    [Fact]
    public void Abgr32PScalarAndBulkFromAssociatedVectorsAreEqual() => AssertScalarAndBulkFromAssociatedVectorsAreEqual<Abgr32P>();

    private static void AssertLosslessRoundTrip<TIntermediate>()
        where TIntermediate : unmanaged, IPixel<TIntermediate>
    {
        Rgba32P[] expected =
        [
            new(0, 0, 0, 0),
            new(1, 2, 3, 4),
            new(31, 63, 95, 127),
            new(64, 128, 192, 255),
            new(255, 255, 255, 255),
        ];
        TIntermediate[] intermediate = new TIntermediate[expected.Length];
        Rgba32P[] actual = new Rgba32P[expected.Length];

        PixelOperations<TIntermediate>.Instance.From<Rgba32P>(Configuration.Default, expected, intermediate);
        PixelOperations<Rgba32P>.Instance.From<TIntermediate>(Configuration.Default, intermediate, actual);

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].ToScaledVector4(), intermediate[i].ToScaledVector4());
        }

        Assert.Equal(expected, actual);
    }

    private static void AssertScalarAndBulkAssociatedVectorsAreEqual<TPixel>(Func<byte, byte, byte, byte, TPixel> createPixel)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        TPixel[] pixels = new TPixel[64];
        Vector4[] actual = new Vector4[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            int component = i * 4;
            pixels[i] = createPixel((byte)component, (byte)(component + 1), (byte)(component + 2), (byte)(component + 3));
        }

        PixelOperations<TPixel>.Instance.ToAssociatedScaledVector4(Configuration.Default, pixels, actual);

        for (int i = 0; i < pixels.Length; i++)
        {
            Assert.Equal(pixels[i].ToScaledVector4(), actual[i]);
        }
    }

    private static void AssertScalarAndBulkFromAssociatedVectorsAreEqual<TPixel>()
        where TPixel : unmanaged, IPixel<TPixel>
    {
        const int count = 259;
        Vector4[] vectors = new Vector4[count];
        TPixel[] expected = new TPixel[count];
        TPixel[] actual = new TPixel[count];
        AssociatedAlphaPixelOperations<TPixel> operations = (AssociatedAlphaPixelOperations<TPixel>)PixelOperations<TPixel>.Instance;

        for (int i = 0; i < vectors.Length; i++)
        {
            // Alternating exact byte alpha values with fractional values covers both reassociation branches. The odd length also exercises the SIMD remainder.
            float alpha = (i & 1) == 0 ? (i % 256) / 255F : ((i % 255) + .375F) / 255F;
            vectors[i] = new Vector4(((i * 37) % 256) / 255F, ((i * 73) % 256) / 255F, ((i * 109) % 256) / 255F, 1F) * alpha;
            vectors[i].W = alpha;
            expected[i] = operations.FromAssociatedScaledVector4(vectors[i]);
        }

        operations.FromAssociatedScaledVector4(Configuration.Default, vectors, actual);

        Assert.Equal(expected, actual);
    }
}

/// <summary>
/// Tests conversion between associated and unassociated packed byte formats.
/// </summary>
public class AssociatedToUnassociatedPackedPixelConversionTests
{
    [Fact]
    public void Rgba32PToRgba32ScalarRoundTripPreservesEveryValidAssociatedComponent()
    {
        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            // Associated components greater than alpha are invalid, so the exhaustive domain is triangular rather than 256 squared.
            for (int associated = 0; associated <= alpha; associated++)
            {
                // This integer expression is the exact nearest 8-bit unassociated value for associated * 255 / alpha.
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);
                Rgba32P source = new((byte)associated, 0, 0, (byte)alpha);
                Rgba32 expectedUnassociated = new(unassociated, 0, 0, (byte)alpha);

                Rgba32 actualUnassociated = source.ToRgba32();
                Rgba32P actualRoundTrip = Rgba32P.FromRgba32(actualUnassociated);

                Assert.Equal(expectedUnassociated, actualUnassociated);
                Assert.Equal(source, actualRoundTrip);
            }
        }
    }

    [Fact]
    public void ColorFromRgba32PPreservesEveryValidAssociatedComponent()
    {
        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            for (int associated = 0; associated <= alpha; associated++)
            {
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);
                Rgba32P source = new((byte)associated, 0, 0, (byte)alpha);
                Color color = Color.FromPixel(source);

                Assert.Equal(new Rgba32(unassociated, 0, 0, (byte)alpha), color.ToPixel<Rgba32>());
                Assert.Equal(source, color.ToPixel<Rgba32P>());
            }
        }
    }

    [Fact]
    public void Rgba32PToBgra32RoundTripPreservesEveryValidAssociatedComponent()
        => AssertUnsignedByteBulkRoundTrip((red, green, blue, alpha) => new Rgba32P(red, green, blue, alpha));

    [Fact]
    public void Bgra32PToBgra32RoundTripPreservesEveryValidAssociatedComponent()
        => AssertUnsignedByteBulkRoundTrip((red, green, blue, alpha) => new Bgra32P(red, green, blue, alpha));

    [Fact]
    public void Argb32PToBgra32RoundTripPreservesEveryValidAssociatedComponent()
        => AssertUnsignedByteBulkRoundTrip((red, green, blue, alpha) => new Argb32P(red, green, blue, alpha));

    [Fact]
    public void Abgr32PToBgra32RoundTripPreservesEveryValidAssociatedComponent()
        => AssertUnsignedByteBulkRoundTrip((red, green, blue, alpha) => new Abgr32P(red, green, blue, alpha));

    [Fact]
    public void NormalizedByte4PToRgba32ScalarRoundTripPreservesEveryValidAssociatedComponent()
    {
        for (int alpha = 0; alpha < byte.MaxValue; alpha++)
        {
            for (int associated = 0; associated <= alpha; associated++)
            {
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);
                byte unassociatedAlpha = (byte)(((alpha * byte.MaxValue) + 127) / 254);
                NormalizedByte4P source = CreateNormalizedByte4P(associated, 0, 0, alpha);

                Rgba32 actualUnassociated = source.ToRgba32();
                NormalizedByte4P actualRoundTrip = NormalizedByte4P.FromRgba32(actualUnassociated);

                Assert.Equal(new Rgba32(unassociated, 0, 0, unassociatedAlpha), actualUnassociated);
                Assert.Equal(source, actualRoundTrip);
            }
        }
    }

    [Fact]
    public void ColorFromNormalizedByte4PPreservesEveryValidAssociatedComponent()
    {
        for (int alpha = 0; alpha < byte.MaxValue; alpha++)
        {
            for (int associated = 0; associated <= alpha; associated++)
            {
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);
                byte unassociatedAlpha = (byte)(((alpha * byte.MaxValue) + 127) / 254);
                NormalizedByte4P source = CreateNormalizedByte4P(associated, 0, 0, alpha);
                Color color = Color.FromPixel(source);

                Assert.Equal(new Rgba32(unassociated, 0, 0, unassociatedAlpha), color.ToPixel<Rgba32>());
                Assert.Equal(source, color.ToPixel<NormalizedByte4P>());
            }
        }
    }

    [Fact]
    public void NormalizedByte4PToBgra32RoundTripPreservesEveryValidAssociatedComponent()
    {
        const int pairCount = 32640;
        const int channelCount = 3;
        NormalizedByte4P[] source = new NormalizedByte4P[pairCount * channelCount];
        Bgra32[] expectedUnassociated = new Bgra32[source.Length];
        Bgra32[] actualUnassociated = new Bgra32[source.Length];
        NormalizedByte4P[] actualRoundTrip = new NormalizedByte4P[source.Length];
        int index = 0;

        for (int alpha = 0; alpha < byte.MaxValue; alpha++)
        {
            for (int associated = 0; associated <= alpha; associated++)
            {
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);
                byte unassociatedAlpha = (byte)(((alpha * byte.MaxValue) + 127) / 254);

                source[index] = CreateNormalizedByte4P(associated, 0, 0, alpha);
                expectedUnassociated[index++] = new Bgra32(unassociated, 0, 0, unassociatedAlpha);
                source[index] = CreateNormalizedByte4P(0, associated, 0, alpha);
                expectedUnassociated[index++] = new Bgra32(0, unassociated, 0, unassociatedAlpha);
                source[index] = CreateNormalizedByte4P(0, 0, associated, alpha);
                expectedUnassociated[index++] = new Bgra32(0, 0, unassociated, unassociatedAlpha);
            }
        }

        PixelOperations<Bgra32>.Instance.From<NormalizedByte4P>(Configuration.Default, source, actualUnassociated);
        PixelOperations<NormalizedByte4P>.Instance.From<Bgra32>(Configuration.Default, actualUnassociated, actualRoundTrip);

        Assert.Equal(expectedUnassociated, actualUnassociated);
        Assert.Equal(source, actualRoundTrip);
    }

    private static void AssertUnsignedByteBulkRoundTrip<TPixel>(Func<byte, byte, byte, byte, TPixel> createPixel)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        const int pairCount = 32896;
        const int channelCount = 3;
        TPixel[] source = new TPixel[pairCount * channelCount];
        Bgra32[] expectedUnassociated = new Bgra32[source.Length];
        Bgra32[] actualUnassociated = new Bgra32[source.Length];
        TPixel[] actualRoundTrip = new TPixel[source.Length];
        int index = 0;

        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            // Associated components greater than alpha are invalid, so the exhaustive domain is triangular rather than 256 squared.
            for (int associated = 0; associated <= alpha; associated++)
            {
                // This integer expression is the exact nearest 8-bit unassociated value for associated * 255 / alpha.
                byte unassociated = alpha == 0 ? (byte)0 : (byte)(((associated * byte.MaxValue) + (alpha / 2)) / alpha);

                source[index] = createPixel((byte)associated, 0, 0, (byte)alpha);
                expectedUnassociated[index++] = new Bgra32(unassociated, 0, 0, (byte)alpha);
                source[index] = createPixel(0, (byte)associated, 0, (byte)alpha);
                expectedUnassociated[index++] = new Bgra32(0, unassociated, 0, (byte)alpha);
                source[index] = createPixel(0, 0, (byte)associated, (byte)alpha);
                expectedUnassociated[index++] = new Bgra32(0, 0, unassociated, (byte)alpha);
            }
        }

        PixelOperations<Bgra32>.Instance.From<TPixel>(Configuration.Default, source, actualUnassociated);
        PixelOperations<TPixel>.Instance.From<Bgra32>(Configuration.Default, actualUnassociated, actualRoundTrip);

        Assert.Equal(expectedUnassociated, actualUnassociated);
        Assert.Equal(source, actualRoundTrip);
    }

    private static NormalizedByte4P CreateNormalizedByte4P(int red, int green, int blue, int alpha)
    {
        uint packed = (byte)(red - 127)
            | ((uint)(byte)(green - 127) << 8)
            | ((uint)(byte)(blue - 127) << 16)
            | ((uint)(byte)(alpha - 127) << 24);

        return new NormalizedByte4P { PackedValue = packed };
    }
}

/// <summary>
/// Tests that associated destinations use their stored alpha value when associating color components.
/// </summary>
public class AssociatedDestinationAlphaQuantizationTests
{
    [Fact]
    public void Rgba32PQuantizesDestinationAlphaBeforeAssociation() => AssertUnsignedByteDestinationQuantizesAlphaBeforeAssociation<Rgba32P>();

    [Fact]
    public void Bgra32PQuantizesDestinationAlphaBeforeAssociation() => AssertUnsignedByteDestinationQuantizesAlphaBeforeAssociation<Bgra32P>();

    [Fact]
    public void Argb32PQuantizesDestinationAlphaBeforeAssociation() => AssertUnsignedByteDestinationQuantizesAlphaBeforeAssociation<Argb32P>();

    [Fact]
    public void Abgr32PQuantizesDestinationAlphaBeforeAssociation() => AssertUnsignedByteDestinationQuantizesAlphaBeforeAssociation<Abgr32P>();

    [Fact]
    public void NormalizedByte4PQuantizesDestinationAlphaBeforeAssociation()
    {
        ReadOnlySpan<byte> components = [64, 127, 191];
        Rgba64[] source = new Rgba64[(ushort.MaxValue + 1) * components.Length];
        NormalizedByte4P[] actualBulk = new NormalizedByte4P[source.Length];
        int index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            foreach (byte component in components)
            {
                source[index++] = new Rgba64((ushort)(component * 257), 0, 0, (ushort)alpha);
            }
        }

        PixelOperations<NormalizedByte4P>.Instance.From<Rgba64>(Configuration.Default, source, actualBulk);
        index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            int expectedAlpha = ((alpha * 254) + 32767) / ushort.MaxValue;

            foreach (byte component in components)
            {
                int expectedRed = ((component * expectedAlpha) + 127) / byte.MaxValue;
                NormalizedByte4P actualScalar = NormalizedByte4P.FromRgba64(source[index]);
                int scalarRed = (sbyte)actualScalar.PackedValue + 127;
                int scalarAlpha = (sbyte)(actualScalar.PackedValue >> 24) + 127;
                int bulkRed = (sbyte)actualBulk[index].PackedValue + 127;
                int bulkAlpha = (sbyte)(actualBulk[index].PackedValue >> 24) + 127;

                Assert.Equal(expectedRed, scalarRed);
                Assert.Equal(expectedAlpha, scalarAlpha);
                Assert.Equal(expectedRed, bulkRed);
                Assert.Equal(expectedAlpha, bulkAlpha);
                index++;
            }
        }
    }

    [Fact]
    public void HalfVector4PQuantizesDestinationAlphaBeforeAssociation()
    {
        ReadOnlySpan<byte> components = [64, 127, 191];
        Rgba64[] source = new Rgba64[(ushort.MaxValue + 1) * components.Length];
        HalfVector4P[] actualBulk = new HalfVector4P[source.Length];
        int index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            foreach (byte component in components)
            {
                source[index++] = new Rgba64((ushort)(component * 257), 0, 0, (ushort)alpha);
            }
        }

        PixelOperations<HalfVector4P>.Instance.From<Rgba64>(Configuration.Default, source, actualBulk);
        index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            float nativeAlpha = ((alpha / (float)ushort.MaxValue) * 2F) - 1F;
            ushort expectedAlpha = BitConverter.HalfToUInt16Bits((Half)nativeAlpha);
            float storedAlpha = ((float)BitConverter.UInt16BitsToHalf(expectedAlpha) + 1F) / 2F;

            foreach (byte component in components)
            {
                float associatedRed = ((component / (float)byte.MaxValue) * storedAlpha * 2F) - 1F;
                ushort expectedRed = BitConverter.HalfToUInt16Bits((Half)associatedRed);
                HalfVector4P actualScalar = HalfVector4P.FromRgba64(source[index]);

                Assert.Equal(expectedRed, (ushort)actualScalar.PackedValue);
                Assert.Equal(expectedAlpha, (ushort)(actualScalar.PackedValue >> 48));
                Assert.Equal(expectedRed, (ushort)actualBulk[index].PackedValue);
                Assert.Equal(expectedAlpha, (ushort)(actualBulk[index].PackedValue >> 48));
                index++;
            }
        }
    }

    [Fact]
    public void ColorToRgba32PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<Rgba32P>();

    [Fact]
    public void ColorToBgra32PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<Bgra32P>();

    [Fact]
    public void ColorToArgb32PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<Argb32P>();

    [Fact]
    public void ColorToAbgr32PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<Abgr32P>();

    [Fact]
    public void ColorToNormalizedByte4PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<NormalizedByte4P>();

    [Fact]
    public void ColorToHalfVector4PUsesDestinationAlphaRepresentation() => AssertColorUsesDestinationAlphaRepresentation<HalfVector4P>();

    /// <summary>
    /// Verifies the unsigned-byte destination grid through scalar and bulk conversion entry points.
    /// </summary>
    /// <typeparam name="TPixel">The associated unsigned-byte pixel format.</typeparam>
    private static void AssertUnsignedByteDestinationQuantizesAlphaBeforeAssociation<TPixel>()
        where TPixel : unmanaged, IPixel<TPixel>
    {
        ReadOnlySpan<byte> components = [64, 127, 191];
        Rgba64[] source = new Rgba64[(ushort.MaxValue + 1) * components.Length];
        TPixel[] actualBulk = new TPixel[source.Length];
        int index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            foreach (byte component in components)
            {
                source[index++] = new Rgba64((ushort)(component * 257), 0, 0, (ushort)alpha);
            }
        }

        PixelOperations<TPixel>.Instance.From<Rgba64>(Configuration.Default, source, actualBulk);
        index = 0;

        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            int expectedAlpha = ((alpha * byte.MaxValue) + 32767) / ushort.MaxValue;

            foreach (byte component in components)
            {
                int expectedRed = ((component * expectedAlpha) + 127) / byte.MaxValue;
                Vector4 expected = new Vector4(expectedRed, 0, 0, expectedAlpha) * (1F / byte.MaxValue);

                Assert.Equal(expected, TPixel.FromRgba64(source[index]).ToScaledVector4());
                Assert.Equal(expected, actualBulk[index].ToScaledVector4());
                index++;
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Color"/> delegates association to the destination pixel operations.
    /// </summary>
    /// <typeparam name="TPixel">The associated destination pixel format.</typeparam>
    private static void AssertColorUsesDestinationAlphaRepresentation<TPixel>()
        where TPixel : unmanaged, IPixel<TPixel>
    {
        for (int alpha = 0; alpha <= ushort.MaxValue; alpha++)
        {
            ushort component = (ushort)((byte)alpha * 257);
            Rgba64 source = new(component, 0, 0, (ushort)alpha);
            TPixel expected = TPixel.FromRgba64(source);
            TPixel actual = Color.FromPixel(source).ToPixel<TPixel>();

            Assert.Equal(expected, actual);
        }
    }
}
