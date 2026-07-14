// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using SixLabors.ImageSharp.Common.Helpers;

namespace SixLabors.ImageSharp.PixelFormats.Utils;

/// <content>
/// Contains <see cref="AssociatedRgbaCompatible"/>.
/// </content>
internal static partial class Vector4Converters
{
    /// <summary>
    /// Provides efficient batched conversion for four-byte pixel types that store premultiplied alpha.
    /// </summary>
    public static class AssociatedRgbaCompatible
    {
        /// <summary>
        /// Converts associated RGBA byte components to an unassociated scaled vector.
        /// </summary>
        /// <param name="red">The associated red component.</param>
        /// <param name="green">The associated green component.</param>
        /// <param name="blue">The associated blue component.</param>
        /// <param name="alpha">The alpha component.</param>
        /// <returns>The unassociated scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 ToUnassociatedVector4(byte red, byte green, byte blue, byte alpha)
        {
            // Divide the original byte magnitudes before normalization so a floating-point intermediate cannot move an exact byte conversion across its rounding midpoint.
            Vector4 vector = new(red, green, blue, alpha);

            if (alpha == 0)
            {
                // Numerics.UnPremultiply preserves RGB when alpha is zero. Normalize the stored components because they already are the unassociated value in this case.
                return vector / byte.MaxValue;
            }

            Numerics.UnPremultiply(ref vector);
            vector.W /= byte.MaxValue;
            return vector;
        }

        /// <summary>
        /// Converts an unassociated scaled vector to the associated representation of an unsigned-byte destination.
        /// </summary>
        /// <param name="source">The unassociated scaled vector.</param>
        /// <returns>The associated scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Associate(Vector4 source)
        {
            float storedAlpha = (byte)Numerics.Clamp((source.W * byte.MaxValue) + 0.5F, 0, byte.MaxValue);

            // Associate in byte magnitude before normalization. Multiplying by a normalized alpha and later restoring byte magnitude can move an exact half-byte across its rounding boundary.
            source *= storedAlpha;
            source.W = storedAlpha;
            return source / byte.MaxValue;
        }

        /// <summary>
        /// Converts unassociated scaled vectors to the associated representation of an unsigned-byte destination.
        /// </summary>
        /// <param name="source">The vectors to convert in place.</param>
        internal static void Associate(Span<Vector4> source)
        {
            ref Vector4 sourceBase = ref MemoryMarshal.GetReference(source);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Unsafe.Add(ref sourceBase, i) = Associate(Unsafe.Add(ref sourceBase, i));
            }
        }

        /// <summary>
        /// Converts an associated scaled vector to associated byte magnitudes using the alpha byte the destination stores.
        /// </summary>
        /// <param name="source">The associated scaled vector.</param>
        /// <returns>The associated byte magnitudes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 ReassociateToByte(Vector4 source)
        {
            float alpha = source.W;

            if (alpha == 0)
            {
                return Vector4.Zero;
            }

            float byteAlpha = alpha * byte.MaxValue;
            float storedAlpha = (byte)Numerics.Clamp(byteAlpha + 0.5F, 0, byte.MaxValue);

            if (byteAlpha == storedAlpha)
            {
                // Quantization leaves alpha unchanged, so RGB is already associated correctly. Avoiding division preserves exact byte midpoints produced by scaling stored components.
                source *= byte.MaxValue;
                source.W = storedAlpha;
                return source;
            }

            // Recover straight RGB before multiplying by the stored alpha byte. Keeping the association in byte magnitude preserves exact half-byte rounding boundaries.
            Numerics.UnPremultiply(ref source);
            source *= storedAlpha;
            source.W = storedAlpha;
            return source;
        }

        /// <summary>
        /// Converts associated scaled vectors to associated byte magnitudes using the alpha bytes the destination stores.
        /// </summary>
        /// <param name="source">The vectors to convert in place.</param>
        private static void ReassociateToByte(Span<Vector4> source)
        {
            if (Avx512F.IsSupported)
            {
                ref Vector512<float> vectorSourceBase = ref Unsafe.As<Vector4, Vector512<float>>(ref MemoryMarshal.GetReference(source));
                ref Vector512<float> sourceEnd = ref Unsafe.Add(ref vectorSourceBase, (uint)source.Length / 4u);

                while (Unsafe.IsAddressLessThan(ref vectorSourceBase, ref sourceEnd))
                {
                    vectorSourceBase = ReassociateToByte(vectorSourceBase);
                    vectorSourceBase = ref Unsafe.Add(ref vectorSourceBase, 1);
                }

                source = source[(source.Length & ~3)..];
            }
            else if (Avx.IsSupported)
            {
                ref Vector256<float> vectorSourceBase = ref Unsafe.As<Vector4, Vector256<float>>(ref MemoryMarshal.GetReference(source));
                ref Vector256<float> sourceEnd = ref Unsafe.Add(ref vectorSourceBase, (uint)source.Length / 2u);

                while (Unsafe.IsAddressLessThan(ref vectorSourceBase, ref sourceEnd))
                {
                    vectorSourceBase = ReassociateToByte(vectorSourceBase);
                    vectorSourceBase = ref Unsafe.Add(ref vectorSourceBase, 1);
                }

                source = source[(source.Length & ~1)..];
            }

            ref Vector4 sourceBase = ref MemoryMarshal.GetReference(source);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Unsafe.Add(ref sourceBase, i) = ReassociateToByte(Unsafe.Add(ref sourceBase, i));
            }
        }

        /// <summary>
        /// Converts four associated scaled vectors to associated byte magnitudes using the alpha bytes the destination stores.
        /// </summary>
        /// <param name="source">The associated scaled vectors.</param>
        /// <returns>The associated byte magnitudes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> ReassociateToByte(Vector512<float> source)
        {
            Vector512<float> zero = Vector512<float>.Zero;
            Vector512<float> byteMax = Vector512.Create((float)byte.MaxValue);
            Vector512<float> alpha = Vector512_.ShuffleNative(source, 0b_11_11_11_11);
            Vector512<float> byteAlpha = alpha * byteMax;
            Vector512<float> storedAlpha = Vector512.Floor(Vector512.Min(Vector512.Max(byteAlpha + Vector512.Create(.5F), zero), byteMax));
            Vector512<float> result = (source / alpha) * storedAlpha;

            // Exact byte alpha values need no reassociation. Multiplying by 255 directly preserves RGB values that already lie on byte midpoints.
            result = Vector512.ConditionalSelect(Vector512.Equals(byteAlpha, storedAlpha), source * byteMax, result);
            Vector512<float> alphaMask = Vector512.Create(0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            result = Vector512.ConditionalSelect(alphaMask, storedAlpha, result);
            return Vector512.ConditionalSelect(Vector512.Equals(alpha, zero), zero, result);
        }

        /// <summary>
        /// Converts two associated scaled vectors to associated byte magnitudes using the alpha bytes the destination stores.
        /// </summary>
        /// <param name="source">The associated scaled vectors.</param>
        /// <returns>The associated byte magnitudes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> ReassociateToByte(Vector256<float> source)
        {
            Vector256<float> zero = Vector256<float>.Zero;
            Vector256<float> byteMax = Vector256.Create((float)byte.MaxValue);
            Vector256<float> alpha = Avx.Permute(source, 0b_11_11_11_11);
            Vector256<float> byteAlpha = alpha * byteMax;
            Vector256<float> storedAlpha = Avx.Floor(Avx.Min(Avx.Max(byteAlpha + Vector256.Create(.5F), zero), byteMax));
            Vector256<float> result = (source / alpha) * storedAlpha;

            // Exact byte alpha values need no reassociation. Multiplying by 255 directly preserves RGB values that already lie on byte midpoints.
            result = Avx.BlendVariable(result, source * byteMax, Avx.CompareEqual(byteAlpha, storedAlpha));
            result = Avx.Blend(result, storedAlpha, 0b_1000_1000);
            return Avx.BlendVariable(result, zero, Avx.CompareEqual(alpha, zero));
        }

        /// <summary>
        /// Converts an associated scaled vector to an <see cref="Rgba32P"/> pixel.
        /// </summary>
        /// <param name="source">The associated scaled vector.</param>
        /// <returns>The associated pixel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rgba32P FromAssociatedVector4ToRgba32P(Vector4 source)
        {
            source = ReassociateToByte(source);
            return new Rgba32P(ConvertToByte(source.X), ConvertToByte(source.Y), ConvertToByte(source.Z), ConvertToByte(source.W));
        }

        /// <summary>
        /// Converts an associated scaled vector to a <see cref="Bgra32P"/> pixel.
        /// </summary>
        /// <param name="source">The associated scaled vector.</param>
        /// <returns>The associated pixel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Bgra32P FromAssociatedVector4ToBgra32P(Vector4 source)
        {
            source = ReassociateToByte(source);
            return new Bgra32P(ConvertToByte(source.X), ConvertToByte(source.Y), ConvertToByte(source.Z), ConvertToByte(source.W));
        }

        /// <summary>
        /// Converts an associated scaled vector to an <see cref="Argb32P"/> pixel.
        /// </summary>
        /// <param name="source">The associated scaled vector.</param>
        /// <returns>The associated pixel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Argb32P FromAssociatedVector4ToArgb32P(Vector4 source)
        {
            source = ReassociateToByte(source);
            return new Argb32P(ConvertToByte(source.X), ConvertToByte(source.Y), ConvertToByte(source.Z), ConvertToByte(source.W));
        }

        /// <summary>
        /// Converts an associated scaled vector to an <see cref="Abgr32P"/> pixel.
        /// </summary>
        /// <param name="source">The associated scaled vector.</param>
        /// <returns>The associated pixel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Abgr32P FromAssociatedVector4ToAbgr32P(Vector4 source)
        {
            source = ReassociateToByte(source);
            return new Abgr32P(ConvertToByte(source.X), ConvertToByte(source.Y), ConvertToByte(source.Z), ConvertToByte(source.W));
        }

        /// <summary>
        /// Converts premultiplied RGBA pixels to vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToVector4(
            ReadOnlySpan<Rgba32P> source,
            Span<Vector4> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            SimdUtils.ByteToNormalizedFloat(MemoryMarshal.Cast<Rgba32P, byte>(source), MemoryMarshal.Cast<Vector4, float>(destination));
            ApplyForwardConversionModifiers(destination, modifiers.Remove(PixelConversionModifiers.Premultiply));
        }

        /// <summary>
        /// Converts premultiplied RGBA pixels to unassociated scaled vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        internal static void ToUnassociatedVector4(ReadOnlySpan<Rgba32P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            ref Rgba32P sourceBase = ref MemoryMarshal.GetReference(source);
            ref Vector4 destinationBase = ref MemoryMarshal.GetReference(destination);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Rgba32P pixel = Unsafe.Add(ref sourceBase, i);
                Unsafe.Add(ref destinationBase, i) = ToUnassociatedVector4(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        /// <summary>
        /// Converts vectors to premultiplied RGBA pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FromVector4(
            Span<Vector4> source,
            Span<Rgba32P> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ApplyBackwardConversionModifiers(source, modifiers.Remove(PixelConversionModifiers.Premultiply));
            SimdUtils.NormalizedFloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), MemoryMarshal.Cast<Rgba32P, byte>(destination));
        }

        /// <summary>
        /// Converts associated scaled vectors to premultiplied RGBA pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        internal static void FromAssociatedVector4(Span<Vector4> source, Span<Rgba32P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ReassociateToByte(source);
            SimdUtils.FloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), MemoryMarshal.Cast<Rgba32P, byte>(destination));
        }

        /// <summary>
        /// Converts premultiplied BGRA pixels to vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToVector4(
            ReadOnlySpan<Bgra32P> source,
            Span<Vector4> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];

            if (source.IsEmpty)
            {
                return;
            }

            // ByteToNormalizedFloat preserves component order, so the packed BGRA bytes must first be shuffled to RGBA.
            // Reuse the unwritten end of the larger Vector4 destination as staging to avoid a temporary allocation.
            // The final vector overlaps that staging, so its source pixel is converted after the staged bytes are consumed.
            int lastIndex = source.Length - 1;
            Span<Rgba32> temporary = MemoryMarshal.Cast<Vector4, Rgba32>(destination).Slice((3 * source.Length) + 1, lastIndex);
            PixelConverter.FromBgra32.ToRgba32(MemoryMarshal.Cast<Bgra32P, byte>(source[..lastIndex]), MemoryMarshal.Cast<Rgba32, byte>(temporary));
            SimdUtils.ByteToNormalizedFloat(MemoryMarshal.Cast<Rgba32, byte>(temporary), MemoryMarshal.Cast<Vector4, float>(destination[..lastIndex]));
            destination[lastIndex] = source[lastIndex].ToVector4();
            ApplyForwardConversionModifiers(destination, modifiers.Remove(PixelConversionModifiers.Premultiply));
        }

        /// <summary>
        /// Converts premultiplied BGRA pixels to unassociated scaled vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        internal static void ToUnassociatedVector4(ReadOnlySpan<Bgra32P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            ref Bgra32P sourceBase = ref MemoryMarshal.GetReference(source);
            ref Vector4 destinationBase = ref MemoryMarshal.GetReference(destination);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Bgra32P pixel = Unsafe.Add(ref sourceBase, i);
                Unsafe.Add(ref destinationBase, i) = ToUnassociatedVector4(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        /// <summary>
        /// Converts vectors to premultiplied BGRA pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FromVector4(
            Span<Vector4> source,
            Span<Bgra32P> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ApplyBackwardConversionModifiers(source, modifiers.Remove(PixelConversionModifiers.Premultiply));

            // NormalizedFloatToByteSaturate emits RGBA bytes, which can be shuffled in place to avoid a temporary pixel buffer.
            Span<byte> destinationBytes = MemoryMarshal.Cast<Bgra32P, byte>(destination);
            SimdUtils.NormalizedFloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToBgra32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts associated scaled vectors to premultiplied BGRA pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        internal static void FromAssociatedVector4(Span<Vector4> source, Span<Bgra32P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ReassociateToByte(source);

            Span<byte> destinationBytes = MemoryMarshal.Cast<Bgra32P, byte>(destination);
            SimdUtils.FloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToBgra32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts premultiplied ARGB pixels to vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToVector4(
            ReadOnlySpan<Argb32P> source,
            Span<Vector4> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];

            if (source.IsEmpty)
            {
                return;
            }

            // ByteToNormalizedFloat preserves component order, so the packed ARGB bytes must first be shuffled to RGBA.
            // Reuse the unwritten end of the larger Vector4 destination as staging to avoid a temporary allocation.
            // The final vector overlaps that staging, so its source pixel is converted after the staged bytes are consumed.
            int lastIndex = source.Length - 1;
            Span<Rgba32> temporary = MemoryMarshal.Cast<Vector4, Rgba32>(destination).Slice((3 * source.Length) + 1, lastIndex);
            PixelConverter.FromArgb32.ToRgba32(MemoryMarshal.Cast<Argb32P, byte>(source[..lastIndex]), MemoryMarshal.Cast<Rgba32, byte>(temporary));
            SimdUtils.ByteToNormalizedFloat(MemoryMarshal.Cast<Rgba32, byte>(temporary), MemoryMarshal.Cast<Vector4, float>(destination[..lastIndex]));
            destination[lastIndex] = source[lastIndex].ToVector4();
            ApplyForwardConversionModifiers(destination, modifiers.Remove(PixelConversionModifiers.Premultiply));
        }

        /// <summary>
        /// Converts premultiplied ARGB pixels to unassociated scaled vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        internal static void ToUnassociatedVector4(ReadOnlySpan<Argb32P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            ref Argb32P sourceBase = ref MemoryMarshal.GetReference(source);
            ref Vector4 destinationBase = ref MemoryMarshal.GetReference(destination);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Argb32P pixel = Unsafe.Add(ref sourceBase, i);
                Unsafe.Add(ref destinationBase, i) = ToUnassociatedVector4(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        /// <summary>
        /// Converts vectors to premultiplied ARGB pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FromVector4(
            Span<Vector4> source,
            Span<Argb32P> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ApplyBackwardConversionModifiers(source, modifiers.Remove(PixelConversionModifiers.Premultiply));

            // NormalizedFloatToByteSaturate emits RGBA bytes, which can be shuffled in place to avoid a temporary pixel buffer.
            Span<byte> destinationBytes = MemoryMarshal.Cast<Argb32P, byte>(destination);
            SimdUtils.NormalizedFloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToArgb32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts associated scaled vectors to premultiplied ARGB pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        internal static void FromAssociatedVector4(Span<Vector4> source, Span<Argb32P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ReassociateToByte(source);

            Span<byte> destinationBytes = MemoryMarshal.Cast<Argb32P, byte>(destination);
            SimdUtils.FloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToArgb32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts premultiplied ABGR pixels to vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToVector4(
            ReadOnlySpan<Abgr32P> source,
            Span<Vector4> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];

            if (source.IsEmpty)
            {
                return;
            }

            // ByteToNormalizedFloat preserves component order, so the packed ABGR bytes must first be shuffled to RGBA.
            // Reuse the unwritten end of the larger Vector4 destination as staging to avoid a temporary allocation.
            // The final vector overlaps that staging, so its source pixel is converted after the staged bytes are consumed.
            int lastIndex = source.Length - 1;
            Span<Rgba32> temporary = MemoryMarshal.Cast<Vector4, Rgba32>(destination).Slice((3 * source.Length) + 1, lastIndex);
            PixelConverter.FromAbgr32.ToRgba32(MemoryMarshal.Cast<Abgr32P, byte>(source[..lastIndex]), MemoryMarshal.Cast<Rgba32, byte>(temporary));
            SimdUtils.ByteToNormalizedFloat(MemoryMarshal.Cast<Rgba32, byte>(temporary), MemoryMarshal.Cast<Vector4, float>(destination[..lastIndex]));
            destination[lastIndex] = source[lastIndex].ToVector4();
            ApplyForwardConversionModifiers(destination, modifiers.Remove(PixelConversionModifiers.Premultiply));
        }

        /// <summary>
        /// Converts premultiplied ABGR pixels to unassociated scaled vectors.
        /// </summary>
        /// <param name="source">The source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        internal static void ToUnassociatedVector4(ReadOnlySpan<Abgr32P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            ref Abgr32P sourceBase = ref MemoryMarshal.GetReference(source);
            ref Vector4 destinationBase = ref MemoryMarshal.GetReference(destination);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Abgr32P pixel = Unsafe.Add(ref sourceBase, i);
                Unsafe.Add(ref destinationBase, i) = ToUnassociatedVector4(pixel.R, pixel.G, pixel.B, pixel.A);
            }
        }

        /// <summary>
        /// Converts vectors to premultiplied ABGR pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="modifiers">The conversion modifier flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FromVector4(
            Span<Vector4> source,
            Span<Abgr32P> destination,
            PixelConversionModifiers modifiers)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ApplyBackwardConversionModifiers(source, modifiers.Remove(PixelConversionModifiers.Premultiply));

            // NormalizedFloatToByteSaturate emits RGBA bytes, which can be shuffled in place to avoid a temporary pixel buffer.
            Span<byte> destinationBytes = MemoryMarshal.Cast<Abgr32P, byte>(destination);
            SimdUtils.NormalizedFloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToAbgr32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts associated scaled vectors to premultiplied ABGR pixels.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        internal static void FromAssociatedVector4(Span<Vector4> source, Span<Abgr32P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            ReassociateToByte(source);

            Span<byte> destinationBytes = MemoryMarshal.Cast<Abgr32P, byte>(destination);
            SimdUtils.FloatToByteSaturate(MemoryMarshal.Cast<Vector4, float>(source), destinationBytes);
            PixelConverter.FromRgba32.ToAbgr32(destinationBytes, destinationBytes);
        }

        /// <summary>
        /// Converts a floating-point byte magnitude to a byte.
        /// </summary>
        /// <param name="value">The byte magnitude.</param>
        /// <returns>The rounded and clamped byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ConvertToByte(float value) => (byte)Numerics.Clamp(value + .5F, 0, byte.MaxValue);
    }
}
