// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides bulk operations.
/// </content>
public partial struct NormalizedByte4P
{
    /// <summary>
    /// Provides bulk operations for <see cref="NormalizedByte4P"/>.
    /// </summary>
    internal class PixelOperations : AssociatedAlphaPixelOperations<NormalizedByte4P>
    {
        /// <inheritdoc />
        internal override Vector4 ToUnassociatedScaledVector4(NormalizedByte4P source)
            => NormalizedByte4P.ToUnassociatedScaledVector4(source);

        /// <inheritdoc />
        internal override NormalizedByte4P FromUnassociatedScaledVector4(Vector4 source)
            => FromScaledVector4(Associate(source));

        /// <inheritdoc />
        public override NormalizedByte4P FromAssociatedScaledVector4(Vector4 source)
            => FromScaledVector4(Reassociate(source));

        /// <inheritdoc />
        internal override void ToUnassociatedScaledVector4(Configuration configuration, ReadOnlySpan<NormalizedByte4P> source, Span<Vector4> destination)
        {
            Guard.DestinationShouldNotBeTooShort(source, destination, nameof(destination));

            ref NormalizedByte4P sourceBase = ref MemoryMarshal.GetReference(source);
            ref Vector4 destinationBase = ref MemoryMarshal.GetReference(destination);

            for (nuint i = 0; i < (uint)source.Length; i++)
            {
                Unsafe.Add(ref destinationBase, i) = NormalizedByte4P.ToUnassociatedScaledVector4(Unsafe.Add(ref sourceBase, i));
            }
        }

        /// <inheritdoc />
        internal override void FromUnassociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<NormalizedByte4P> destination)
        {
            source = source[..destination.Length];
            Associate(source);
            this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
        }

        /// <inheritdoc />
        public override void FromAssociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<NormalizedByte4P> destination)
        {
            source = source[..destination.Length];
            Reassociate(source);
            this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
        }
    }
}
