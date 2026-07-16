// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using SixLabors.ImageSharp.Common.Helpers;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides optimized overrides for bulk operations.
/// </content>
public partial struct HalfVector4P
{
    /// <summary>
    /// Provides optimized bulk operations for <see cref="HalfVector4P"/>.
    /// </summary>
    internal class PixelOperations : AssociatedAlphaPixelOperations<HalfVector4P>
    {
        /// <inheritdoc />
        protected override void ToUnassociatedVector4(Configuration configuration, ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            UnpackUnassociated(source, destination[..source.Length], false);
        }

        /// <inheritdoc />
        protected override void ToAssociatedVector4(Configuration configuration, ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            Unpack(source, destination[..source.Length], false);
        }

        /// <inheritdoc />
        protected override void FromUnassociatedVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            PackUnassociated(source, destination, false);
        }

        /// <inheritdoc />
        protected override void FromAssociatedVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            PackAssociated(source, destination, false);
        }

        /// <inheritdoc />
        protected override void ToUnassociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            UnpackUnassociated(source, destination[..source.Length], true);
        }

        /// <inheritdoc />
        protected override void ToAssociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            Unpack(source, destination[..source.Length], true);
        }

        /// <inheritdoc />
        protected override void FromUnassociatedScaledVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            PackUnassociated(source, destination, true);
        }

        /// <inheritdoc />
        protected override void FromAssociatedScaledVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            PackAssociated(source, destination, true);
        }

        /// <summary>
        /// Unpacks native or scaled binary16 components into vectors.
        /// </summary>
        /// <param name="source">The packed source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="scaled">Whether to produce scaled vectors.</param>
        internal static void Unpack(ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination, bool scaled)
        {
            ref ushort sourceBase = ref Unsafe.As<HalfVector4P, ushort>(ref MemoryMarshal.GetReference(source));
            ref float destinationBase = ref Unsafe.As<Vector4, float>(ref MemoryMarshal.GetReference(destination));
            int componentCount = source.Length * Vector128<float>.Count;
            int i = 0;

            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector512<ushort>.Count; i += Vector512<ushort>.Count)
                {
                    Vector512<ushort> packed = Vector512.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector512<float> lower, Vector512<float> upper) = HalfTypeHelper.Unpack(packed);

                    if (scaled)
                    {
                        lower = ToScaled(lower);
                        upper = ToScaled(upper);
                    }

                    Vector512.StoreUnsafe(lower, ref destinationBase, (nuint)i);
                    Vector512.StoreUnsafe(upper, ref destinationBase, (nuint)(i + Vector512<float>.Count));
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
                {
                    Vector256<ushort> packed = Vector256.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector256<float> lower, Vector256<float> upper) = HalfTypeHelper.Unpack(packed);

                    if (scaled)
                    {
                        lower = ToScaled(lower);
                        upper = ToScaled(upper);
                    }

                    Vector256.StoreUnsafe(lower, ref destinationBase, (nuint)i);
                    Vector256.StoreUnsafe(upper, ref destinationBase, (nuint)(i + Vector256<float>.Count));
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
                {
                    Vector128<ushort> packed = Vector128.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector128<float> lower, Vector128<float> upper) = HalfTypeHelper.Unpack(packed);

                    if (scaled)
                    {
                        lower = ToScaled(lower);
                        upper = ToScaled(upper);
                    }

                    Vector128.StoreUnsafe(lower, ref destinationBase, (nuint)i);
                    Vector128.StoreUnsafe(upper, ref destinationBase, (nuint)(i + Vector128<float>.Count));
                }

                if (i < componentCount)
                {
                    // A pixel contains four halves, so the only possible remainder is one complete pixel.
                    ulong remainder = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref sourceBase, (uint)i)));
                    Vector128<ushort> packed = Vector128.CreateScalarUnsafe(remainder).AsUInt16();
                    Vector128<float> vector = HalfTypeHelper.Unpack(packed).Lower;

                    if (scaled)
                    {
                        vector = ToScaled(vector);
                    }

                    Vector128.StoreUnsafe(vector, ref destinationBase, (nuint)i);
                }

                return;
            }

            ref HalfVector4P pixelBase = ref Unsafe.As<ushort, HalfVector4P>(ref sourceBase);
            ref Vector4 vectorBase = ref Unsafe.As<float, Vector4>(ref destinationBase);

            for (int pixelIndex = 0; pixelIndex < source.Length; pixelIndex++)
            {
                HalfVector4P pixel = Unsafe.Add(ref pixelBase, (uint)pixelIndex);
                Unsafe.Add(ref vectorBase, (uint)pixelIndex) = scaled ? pixel.ToScaledVector4() : pixel.ToVector4();
            }
        }

        /// <summary>
        /// Unpacks associated binary16 storage directly into unassociated native or scaled vectors.
        /// </summary>
        /// <param name="source">The packed source pixels.</param>
        /// <param name="destination">The destination vectors.</param>
        /// <param name="scaled">Whether to produce scaled vectors.</param>
        private static void UnpackUnassociated(ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination, bool scaled)
        {
            ref ushort sourceBase = ref Unsafe.As<HalfVector4P, ushort>(ref MemoryMarshal.GetReference(source));
            ref float destinationBase = ref Unsafe.As<Vector4, float>(ref MemoryMarshal.GetReference(destination));
            int componentCount = source.Length * Vector128<float>.Count;
            int i = 0;

            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector512<ushort>.Count; i += Vector512<ushort>.Count)
                {
                    Vector512<ushort> packed = Vector512.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector512<float> lower, Vector512<float> upper) = HalfTypeHelper.Unpack(packed);
                    Vector512.StoreUnsafe(ToUnassociated(lower, scaled), ref destinationBase, (nuint)i);
                    Vector512.StoreUnsafe(ToUnassociated(upper, scaled), ref destinationBase, (nuint)(i + Vector512<float>.Count));
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
                {
                    Vector256<ushort> packed = Vector256.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector256<float> lower, Vector256<float> upper) = HalfTypeHelper.Unpack(packed);
                    Vector256.StoreUnsafe(ToUnassociated(lower, scaled), ref destinationBase, (nuint)i);
                    Vector256.StoreUnsafe(ToUnassociated(upper, scaled), ref destinationBase, (nuint)(i + Vector256<float>.Count));
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
                {
                    Vector128<ushort> packed = Vector128.LoadUnsafe(ref sourceBase, (nuint)i);
                    (Vector128<float> lower, Vector128<float> upper) = HalfTypeHelper.Unpack(packed);
                    Vector128.StoreUnsafe(ToUnassociated(lower, scaled), ref destinationBase, (nuint)i);
                    Vector128.StoreUnsafe(ToUnassociated(upper, scaled), ref destinationBase, (nuint)(i + Vector128<float>.Count));
                }

                if (i < componentCount)
                {
                    // A pixel contains four halves, so the only possible remainder is one complete pixel.
                    ulong remainder = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref sourceBase, (uint)i)));
                    Vector128<ushort> packed = Vector128.CreateScalarUnsafe(remainder).AsUInt16();
                    Vector128<float> vector = HalfTypeHelper.Unpack(packed).Lower;
                    Vector128.StoreUnsafe(ToUnassociated(vector, scaled), ref destinationBase, (nuint)i);
                }

                return;
            }

            ref HalfVector4P pixelBase = ref Unsafe.As<ushort, HalfVector4P>(ref sourceBase);
            ref Vector4 vectorBase = ref Unsafe.As<float, Vector4>(ref destinationBase);

            for (int pixelIndex = 0; pixelIndex < source.Length; pixelIndex++)
            {
                HalfVector4P pixel = Unsafe.Add(ref pixelBase, (uint)pixelIndex);
                Unsafe.Add(ref vectorBase, (uint)pixelIndex) = scaled ? pixel.ToUnassociatedScaledVector4() : pixel.ToUnassociatedVector4();
            }
        }

        /// <summary>
        /// Packs native or scaled vectors into binary16 storage.
        /// </summary>
        /// <param name="source">The source vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        internal static void Pack(Span<Vector4> source, Span<HalfVector4P> destination, bool scaled)
        {
            ref float sourceBase = ref Unsafe.As<Vector4, float>(ref MemoryMarshal.GetReference(source));
            ref ushort destinationBase = ref Unsafe.As<HalfVector4P, ushort>(ref MemoryMarshal.GetReference(destination));
            int componentCount = source.Length * Vector128<float>.Count;
            int i = 0;

            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector512<ushort>.Count; i += Vector512<ushort>.Count)
                {
                    Vector512<float> lower = Vector512.LoadUnsafe(ref sourceBase, (nuint)i);
                    Vector512<float> upper = Vector512.LoadUnsafe(ref sourceBase, (nuint)(i + Vector512<float>.Count));
                    Vector512<ushort> packed = scaled ? PackScaled(lower, upper) : HalfTypeHelper.Pack(lower, upper);
                    Vector512.StoreUnsafe(packed, ref destinationBase, (nuint)i);
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
                {
                    Vector256<float> lower = Vector256.LoadUnsafe(ref sourceBase, (nuint)i);
                    Vector256<float> upper = Vector256.LoadUnsafe(ref sourceBase, (nuint)(i + Vector256<float>.Count));
                    Vector256<ushort> packed = scaled ? PackScaled(lower, upper) : HalfTypeHelper.Pack(lower, upper);
                    Vector256.StoreUnsafe(packed, ref destinationBase, (nuint)i);
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
                {
                    Vector128<float> lower = Vector128.LoadUnsafe(ref sourceBase, (nuint)i);
                    Vector128<float> upper = Vector128.LoadUnsafe(ref sourceBase, (nuint)(i + Vector128<float>.Count));
                    Vector128<ushort> packed = scaled ? PackScaled(lower, upper) : HalfTypeHelper.Pack(lower, upper);
                    Vector128.StoreUnsafe(packed, ref destinationBase, (nuint)i);
                }

                if (i < componentCount)
                {
                    // Duplicate the final vector to use the two-input narrowing primitive, then store only its lower pixel.
                    Vector128<float> vector = Vector128.LoadUnsafe(ref sourceBase, (nuint)i);
                    Vector128<ushort> packed = scaled ? PackScaled(vector, vector) : HalfTypeHelper.Pack(vector, vector);
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref destinationBase, (uint)i)), packed.AsUInt64().GetElement(0));
                }

                return;
            }

            ref Vector4 vectorBase = ref Unsafe.As<float, Vector4>(ref sourceBase);
            ref HalfVector4P pixelBase = ref Unsafe.As<ushort, HalfVector4P>(ref destinationBase);

            for (int pixelIndex = 0; pixelIndex < source.Length; pixelIndex++)
            {
                Vector4 vector = Unsafe.Add(ref vectorBase, (uint)pixelIndex);
                Unsafe.Add(ref pixelBase, (uint)pixelIndex) = scaled ? FromScaledVector4(vector) : FromVector4(vector);
            }
        }

        /// <summary>
        /// Associates unassociated native or scaled vectors and packs them into binary16 storage in one pass.
        /// </summary>
        /// <param name="source">The unassociated vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        private static void PackUnassociated(Span<Vector4> source, Span<HalfVector4P> destination, bool scaled)
        {
            ref float sourceBase = ref Unsafe.As<Vector4, float>(ref MemoryMarshal.GetReference(source));
            ref ushort destinationBase = ref Unsafe.As<HalfVector4P, ushort>(ref MemoryMarshal.GetReference(destination));
            int componentCount = source.Length * Vector128<float>.Count;
            int i = 0;

            // Quantize alpha to the binary16 value the destination will store before associating RGB. The kernels replace W separately
            // so alpha is stored unchanged instead of being multiplied by itself.
            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector512<ushort>.Count; i += Vector512<ushort>.Count)
                {
                    Vector512<float> lower = Associate(Vector512.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector512<float> upper = Associate(Vector512.LoadUnsafe(ref sourceBase, (nuint)(i + Vector512<float>.Count)), scaled);
                    Vector512.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
                {
                    Vector256<float> lower = Associate(Vector256.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector256<float> upper = Associate(Vector256.LoadUnsafe(ref sourceBase, (nuint)(i + Vector256<float>.Count)), scaled);
                    Vector256.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
                {
                    Vector128<float> lower = Associate(Vector128.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector128<float> upper = Associate(Vector128.LoadUnsafe(ref sourceBase, (nuint)(i + Vector128<float>.Count)), scaled);
                    Vector128.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }

                if (i < componentCount)
                {
                    // Duplicate the final vector to use the two-input narrowing primitive, then store only its lower pixel.
                    Vector128<float> vector = Associate(Vector128.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector128<ushort> packed = PackScaled(vector, vector);
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref destinationBase, (uint)i)), packed.AsUInt64().GetElement(0));
                }

                return;
            }

            ref Vector4 vectorBase = ref Unsafe.As<float, Vector4>(ref sourceBase);
            ref HalfVector4P pixelBase = ref Unsafe.As<ushort, HalfVector4P>(ref destinationBase);

            for (int pixelIndex = 0; pixelIndex < source.Length; pixelIndex++)
            {
                Vector4 vector = Unsafe.Add(ref vectorBase, (uint)pixelIndex);

                // Qualify the containing pixel type because this nested class also declares bulk overloads with these names.
                Unsafe.Add(ref pixelBase, (uint)pixelIndex) = scaled ? HalfVector4P.FromUnassociatedScaledVector4(vector) : HalfVector4P.FromUnassociatedVector4(vector);
            }
        }

        /// <summary>
        /// Reassociates associated native or scaled vectors and packs them into binary16 storage in one pass.
        /// </summary>
        /// <param name="source">The associated vectors.</param>
        /// <param name="destination">The destination pixels.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        private static void PackAssociated(Span<Vector4> source, Span<HalfVector4P> destination, bool scaled)
        {
            ref float sourceBase = ref Unsafe.As<Vector4, float>(ref MemoryMarshal.GetReference(source));
            ref ushort destinationBase = ref Unsafe.As<HalfVector4P, ushort>(ref MemoryMarshal.GetReference(destination));
            int componentCount = source.Length * Vector128<float>.Count;
            int i = 0;

            // Preserve straight color across binary16 alpha quantization by scaling RGB by storedAlpha / inputAlpha. The kernels replace
            // W, clamp RGB to stored alpha, and clear zero-alpha vectors so the packed result remains valid associated color.
            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector512<ushort>.Count; i += Vector512<ushort>.Count)
                {
                    Vector512<float> lower = Reassociate(Vector512.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector512<float> upper = Reassociate(Vector512.LoadUnsafe(ref sourceBase, (nuint)(i + Vector512<float>.Count)), scaled);
                    Vector512.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }
            }

            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
                {
                    Vector256<float> lower = Reassociate(Vector256.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector256<float> upper = Reassociate(Vector256.LoadUnsafe(ref sourceBase, (nuint)(i + Vector256<float>.Count)), scaled);
                    Vector256.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= componentCount - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
                {
                    Vector128<float> lower = Reassociate(Vector128.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector128<float> upper = Reassociate(Vector128.LoadUnsafe(ref sourceBase, (nuint)(i + Vector128<float>.Count)), scaled);
                    Vector128.StoreUnsafe(PackScaled(lower, upper), ref destinationBase, (nuint)i);
                }

                if (i < componentCount)
                {
                    // Duplicate the final vector to use the two-input narrowing primitive, then store only its lower pixel.
                    Vector128<float> vector = Reassociate(Vector128.LoadUnsafe(ref sourceBase, (nuint)i), scaled);
                    Vector128<ushort> packed = PackScaled(vector, vector);
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref destinationBase, (uint)i)), packed.AsUInt64().GetElement(0));
                }

                return;
            }

            ref Vector4 vectorBase = ref Unsafe.As<float, Vector4>(ref sourceBase);
            ref HalfVector4P pixelBase = ref Unsafe.As<ushort, HalfVector4P>(ref destinationBase);

            for (int pixelIndex = 0; pixelIndex < source.Length; pixelIndex++)
            {
                Vector4 vector = Unsafe.Add(ref vectorBase, (uint)pixelIndex);

                // Qualify the containing pixel type because this nested class also declares bulk overloads with these names.
                Unsafe.Add(ref pixelBase, (uint)pixelIndex) = scaled ? HalfVector4P.FromAssociatedScaledVector4(vector) : HalfVector4P.FromAssociatedVector4(vector);
            }
        }

        /// <summary>
        /// Converts native binary16 values to the normalized range used by pixel conversions.
        /// </summary>
        /// <param name="source">The native values.</param>
        /// <returns>The scaled values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> ToScaled(Vector128<float> source) => (source + Vector128<float>.One) / Vector128.Create(2F);

        /// <summary>
        /// Converts native binary16 values to the normalized range used by pixel conversions.
        /// </summary>
        /// <param name="source">The native values.</param>
        /// <returns>The scaled values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> ToScaled(Vector256<float> source) => (source + Vector256<float>.One) / Vector256.Create(2F);

        /// <summary>
        /// Converts native binary16 values to the normalized range used by pixel conversions.
        /// </summary>
        /// <param name="source">The native values.</param>
        /// <returns>The scaled values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> ToScaled(Vector512<float> source) => (source + Vector512<float>.One) / Vector512.Create(2F);

        /// <summary>
        /// Converts associated native binary16 values to unassociated native or scaled vectors.
        /// </summary>
        /// <param name="source">The associated native values.</param>
        /// <param name="scaled">Whether to produce scaled vectors.</param>
        /// <returns>The unassociated vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> ToUnassociated(Vector128<float> source, bool scaled)
        {
            source = ToScaled(source);
            Vector128<float> alpha = Vector128_.ShuffleNative(source, 0b_11_11_11_11);
            Vector128<float> result = Vector128.ConditionalSelect(Vector128.Equals(alpha, Vector128<float>.Zero), source, source / alpha);
            Vector128<float> alphaMask = Vector128.Create(0, 0, 0, -1).AsSingle();
            result = Vector128.ConditionalSelect(alphaMask, alpha, result);

            // Binary16-native W is an affine encoding rather than opacity, so map back only after unassociation in scaled space.
            return scaled ? result : (result * Vector128.Create(2F)) - Vector128<float>.One;
        }

        /// <summary>
        /// Converts associated native binary16 values to unassociated native or scaled vectors.
        /// </summary>
        /// <param name="source">The associated native values.</param>
        /// <param name="scaled">Whether to produce scaled vectors.</param>
        /// <returns>The unassociated vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> ToUnassociated(Vector256<float> source, bool scaled)
        {
            source = ToScaled(source);
            Vector256<float> alpha = Vector256_.ShuffleNative(source, 0b_11_11_11_11);
            Vector256<float> result = Vector256.ConditionalSelect(Vector256.Equals(alpha, Vector256<float>.Zero), source, source / alpha);
            Vector256<float> alphaMask = Vector256.Create(0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            result = Vector256.ConditionalSelect(alphaMask, alpha, result);

            // Binary16-native W is an affine encoding rather than opacity, so map back only after unassociation in scaled space.
            return scaled ? result : (result * Vector256.Create(2F)) - Vector256<float>.One;
        }

        /// <summary>
        /// Converts associated native binary16 values to unassociated native or scaled vectors.
        /// </summary>
        /// <param name="source">The associated native values.</param>
        /// <param name="scaled">Whether to produce scaled vectors.</param>
        /// <returns>The unassociated vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> ToUnassociated(Vector512<float> source, bool scaled)
        {
            source = ToScaled(source);
            Vector512<float> alpha = Vector512_.ShuffleNative(source, 0b_11_11_11_11);
            Vector512<float> result = Vector512.ConditionalSelect(Vector512.Equals(alpha, Vector512<float>.Zero), source, source / alpha);
            Vector512<float> alphaMask = Vector512.Create(0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            result = Vector512.ConditionalSelect(alphaMask, alpha, result);

            // Binary16-native W is an affine encoding rather than opacity, so map back only after unassociation in scaled space.
            return scaled ? result : (result * Vector512.Create(2F)) - Vector512<float>.One;
        }

        /// <summary>
        /// Converts unassociated native or scaled vectors to the destination's associated scaled representation.
        /// </summary>
        /// <param name="source">The unassociated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The associated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Associate(Vector128<float> source, bool scaled)
        {
            Vector128<float> zero = Vector128<float>.Zero;
            Vector128<float> one = Vector128<float>.One;

            if (!scaled)
            {
                // Binary16-native W is an affine encoding rather than opacity, so association must happen after mapping to scaled space.
                source = ToScaled(source);
            }

            source = Vector128.Min(Vector128.Max(source, zero), one);
            Vector128<float> alpha = Vector128_.ShuffleNative(source, 0b_11_11_11_11);
            Vector128<float> storedAlpha = (HalfTypeHelper.RoundToHalf((alpha * Vector128.Create(2F)) - one) + one) / Vector128.Create(2F);
            Vector128<float> result = source * storedAlpha;
            Vector128<float> alphaMask = Vector128.Create(0, 0, 0, -1).AsSingle();
            return Vector128.ConditionalSelect(alphaMask, storedAlpha, result);
        }

        /// <summary>
        /// Converts unassociated native or scaled vectors to the destination's associated scaled representation.
        /// </summary>
        /// <param name="source">The unassociated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The associated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> Associate(Vector256<float> source, bool scaled)
        {
            Vector256<float> zero = Vector256<float>.Zero;
            Vector256<float> one = Vector256<float>.One;

            if (!scaled)
            {
                // Binary16-native W is an affine encoding rather than opacity, so association must happen after mapping to scaled space.
                source = ToScaled(source);
            }

            source = Vector256.Min(Vector256.Max(source, zero), one);
            Vector256<float> alpha = Vector256_.ShuffleNative(source, 0b_11_11_11_11);
            Vector256<float> storedAlpha = (HalfTypeHelper.RoundToHalf((alpha * Vector256.Create(2F)) - one) + one) / Vector256.Create(2F);
            Vector256<float> result = source * storedAlpha;
            Vector256<float> alphaMask = Vector256.Create(0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            return Vector256.ConditionalSelect(alphaMask, storedAlpha, result);
        }

        /// <summary>
        /// Converts unassociated native or scaled vectors to the destination's associated scaled representation.
        /// </summary>
        /// <param name="source">The unassociated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The associated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> Associate(Vector512<float> source, bool scaled)
        {
            Vector512<float> zero = Vector512<float>.Zero;
            Vector512<float> one = Vector512<float>.One;

            if (!scaled)
            {
                // Binary16-native W is an affine encoding rather than opacity, so association must happen after mapping to scaled space.
                source = ToScaled(source);
            }

            source = Vector512.Min(Vector512.Max(source, zero), one);
            Vector512<float> alpha = Vector512_.ShuffleNative(source, 0b_11_11_11_11);
            Vector512<float> storedAlpha = (HalfTypeHelper.RoundToHalf((alpha * Vector512.Create(2F)) - one) + one) / Vector512.Create(2F);
            Vector512<float> result = source * storedAlpha;
            Vector512<float> alphaMask = Vector512.Create(0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            return Vector512.ConditionalSelect(alphaMask, storedAlpha, result);
        }

        /// <summary>
        /// Reassociates native or scaled vectors with the scaled alpha values the destination stores.
        /// </summary>
        /// <param name="source">The associated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The reassociated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> Reassociate(Vector128<float> source, bool scaled)
        {
            Vector128<float> zero = Vector128<float>.Zero;
            Vector128<float> one = Vector128<float>.One;

            if (!scaled)
            {
                // Convert every component together so RGB and alpha enter reassociation in the same scaled coordinate system.
                source = ToScaled(source);
            }

            Vector128<float> alpha = Vector128_.ShuffleNative(source, 0b_11_11_11_11);
            Vector128<float> nativeAlpha = (alpha * Vector128.Create(2F)) - one;
            Vector128<float> clampedNativeAlpha = Vector128.Min(Vector128.Max(nativeAlpha, -one), one);

            // Hardware Min/Max replace NaN on .NET 8, whereas the scalar clamp preserves it. Restore those lanes before half quantization.
            nativeAlpha = Vector128.ConditionalSelect(Vector128.Equals(nativeAlpha, nativeAlpha), clampedNativeAlpha, nativeAlpha);

            // Clamp before binary16 quantization because the scaled alpha contract cannot represent opacity outside [0, 1].
            Vector128<float> storedAlpha = (HalfTypeHelper.RoundToHalf(nativeAlpha) + one) / Vector128.Create(2F);
            Vector128<float> result = source * (storedAlpha / alpha);
            Vector128<float> alphaMask = Vector128.Create(0, 0, 0, -1).AsSingle();
            result = Vector128.ConditionalSelect(alphaMask, storedAlpha, result);
            result = Vector128.Min(Vector128.Max(result, zero), storedAlpha);
            return Vector128.ConditionalSelect(Vector128.LessThanOrEqual(alpha, zero), zero, result);
        }

        /// <summary>
        /// Reassociates native or scaled vectors with the scaled alpha values the destination stores.
        /// </summary>
        /// <param name="source">The associated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The reassociated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> Reassociate(Vector256<float> source, bool scaled)
        {
            Vector256<float> zero = Vector256<float>.Zero;
            Vector256<float> one = Vector256<float>.One;

            if (!scaled)
            {
                // Convert every component together so RGB and alpha enter reassociation in the same scaled coordinate system.
                source = ToScaled(source);
            }

            Vector256<float> alpha = Vector256_.ShuffleNative(source, 0b_11_11_11_11);
            Vector256<float> nativeAlpha = (alpha * Vector256.Create(2F)) - one;
            Vector256<float> clampedNativeAlpha = Vector256.Min(Vector256.Max(nativeAlpha, -one), one);

            // Hardware Min/Max replace NaN on .NET 8, whereas the scalar clamp preserves it. Restore those lanes before half quantization.
            nativeAlpha = Vector256.ConditionalSelect(Vector256.Equals(nativeAlpha, nativeAlpha), clampedNativeAlpha, nativeAlpha);

            // Clamp before binary16 quantization because the scaled alpha contract cannot represent opacity outside [0, 1].
            Vector256<float> storedAlpha = (HalfTypeHelper.RoundToHalf(nativeAlpha) + one) / Vector256.Create(2F);
            Vector256<float> result = source * (storedAlpha / alpha);
            Vector256<float> alphaMask = Vector256.Create(0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            result = Vector256.ConditionalSelect(alphaMask, storedAlpha, result);
            result = Vector256.Min(Vector256.Max(result, zero), storedAlpha);
            return Vector256.ConditionalSelect(Vector256.LessThanOrEqual(alpha, zero), zero, result);
        }

        /// <summary>
        /// Reassociates native or scaled vectors with the scaled alpha values the destination stores.
        /// </summary>
        /// <param name="source">The associated vectors.</param>
        /// <param name="scaled">Whether the source contains scaled vectors.</param>
        /// <returns>The reassociated scaled vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<float> Reassociate(Vector512<float> source, bool scaled)
        {
            Vector512<float> zero = Vector512<float>.Zero;
            Vector512<float> one = Vector512<float>.One;

            if (!scaled)
            {
                // Convert every component together so RGB and alpha enter reassociation in the same scaled coordinate system.
                source = ToScaled(source);
            }

            Vector512<float> alpha = Vector512_.ShuffleNative(source, 0b_11_11_11_11);
            Vector512<float> nativeAlpha = (alpha * Vector512.Create(2F)) - one;
            Vector512<float> clampedNativeAlpha = Vector512.Min(Vector512.Max(nativeAlpha, -one), one);

            // Hardware Min/Max replace NaN on .NET 8, whereas the scalar clamp preserves it. Restore those lanes before half quantization.
            nativeAlpha = Vector512.ConditionalSelect(Vector512.Equals(nativeAlpha, nativeAlpha), clampedNativeAlpha, nativeAlpha);

            // Clamp before binary16 quantization because the scaled alpha contract cannot represent opacity outside [0, 1].
            Vector512<float> storedAlpha = (HalfTypeHelper.RoundToHalf(nativeAlpha) + one) / Vector512.Create(2F);
            Vector512<float> result = source * (storedAlpha / alpha);
            Vector512<float> alphaMask = Vector512.Create(0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1, 0, 0, 0, -1).AsSingle();
            result = Vector512.ConditionalSelect(alphaMask, storedAlpha, result);
            result = Vector512.Min(Vector512.Max(result, zero), storedAlpha);
            return Vector512.ConditionalSelect(Vector512.LessThanOrEqual(alpha, zero), zero, result);
        }

        /// <summary>
        /// Converts two scaled vectors to binary16 storage while preserving scalar operation order.
        /// </summary>
        /// <param name="lower">The lower scaled values.</param>
        /// <param name="upper">The upper scaled values.</param>
        /// <returns>The packed binary16 values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> PackScaled(Vector128<float> lower, Vector128<float> upper)
        {
            lower *= Vector128.Create(2F);
            lower -= Vector128<float>.One;
            upper *= Vector128.Create(2F);
            upper -= Vector128<float>.One;
            return HalfTypeHelper.Pack(lower, upper);
        }

        /// <summary>
        /// Converts two scaled vectors to binary16 storage while preserving scalar operation order.
        /// </summary>
        /// <param name="lower">The lower scaled values.</param>
        /// <param name="upper">The upper scaled values.</param>
        /// <returns>The packed binary16 values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> PackScaled(Vector256<float> lower, Vector256<float> upper)
        {
            lower *= Vector256.Create(2F);
            lower -= Vector256<float>.One;
            upper *= Vector256.Create(2F);
            upper -= Vector256<float>.One;
            return HalfTypeHelper.Pack(lower, upper);
        }

        /// <summary>
        /// Converts two scaled vectors to binary16 storage while preserving scalar operation order.
        /// </summary>
        /// <param name="lower">The lower scaled values.</param>
        /// <param name="upper">The upper scaled values.</param>
        /// <returns>The packed binary16 values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<ushort> PackScaled(Vector512<float> lower, Vector512<float> upper)
        {
            lower *= Vector512.Create(2F);
            lower -= Vector512<float>.One;
            upper *= Vector512.Create(2F);
            upper -= Vector512<float>.One;
            return HalfTypeHelper.Pack(lower, upper);
        }
    }
}
