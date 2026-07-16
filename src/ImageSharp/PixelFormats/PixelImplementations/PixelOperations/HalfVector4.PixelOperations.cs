// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats.Utils;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides optimized overrides for bulk operations.
/// </content>
public partial struct HalfVector4
{
    /// <summary>
    /// Provides optimized overrides for bulk operations.
    /// </summary>
    internal class PixelOperations : PixelOperations<HalfVector4>
    {
        private static readonly Vector4 NativeScale = new(2F);

        /// <inheritdoc />
        protected override void ToUnassociatedVector4(Configuration configuration, ReadOnlySpan<HalfVector4> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            HalfVector4P.PixelOperations.Unpack(MemoryMarshal.Cast<HalfVector4, HalfVector4P>(source), destination[..source.Length], false);
        }

        /// <inheritdoc />
        protected override void ToAssociatedVector4(Configuration configuration, ReadOnlySpan<HalfVector4> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];

            // HalfVector4P has the same four-half layout, allowing both formats to share the SIMD half expansion. Association must
            // still occur in scaled space because the native half values use an affine [-1, 1] color coordinate system.
            HalfVector4P.PixelOperations.Unpack(MemoryMarshal.Cast<HalfVector4, HalfVector4P>(source), destination, true);
            Numerics.Premultiply(destination);
            Vector4Converters.MultiplyThenAdd(destination, NativeScale, -Vector4.One);
        }

        /// <inheritdoc />
        protected override void ToUnassociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfVector4> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            HalfVector4P.PixelOperations.Unpack(MemoryMarshal.Cast<HalfVector4, HalfVector4P>(source), destination[..source.Length], true);
        }

        /// <inheritdoc />
        protected override void ToAssociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfVector4> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            HalfVector4P.PixelOperations.Unpack(MemoryMarshal.Cast<HalfVector4, HalfVector4P>(source), destination, true);
            Numerics.Premultiply(destination);
        }

        /// <inheritdoc />
        protected override void FromUnassociatedVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            HalfVector4P.PixelOperations.Pack(source, MemoryMarshal.Cast<HalfVector4, HalfVector4P>(destination), false);
        }

        /// <inheritdoc />
        protected override void FromAssociatedVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];

            // Normalize native alpha with RGB before unassociation, then reuse the shared half packing kernel in scaled mode.
            Vector4Converters.AddThenDivide(source, Vector4.One, NativeScale);
            Numerics.UnPremultiply(source);
            HalfVector4P.PixelOperations.Pack(source, MemoryMarshal.Cast<HalfVector4, HalfVector4P>(destination), true);
        }

        /// <inheritdoc />
        protected override void FromUnassociatedScaledVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            HalfVector4P.PixelOperations.Pack(source, MemoryMarshal.Cast<HalfVector4, HalfVector4P>(destination), true);
        }

        /// <inheritdoc />
        protected override void FromAssociatedScaledVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfVector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            destination = destination[..source.Length];
            Numerics.UnPremultiply(source);
            HalfVector4P.PixelOperations.Pack(source, MemoryMarshal.Cast<HalfVector4, HalfVector4P>(destination), true);
        }
    }
}
