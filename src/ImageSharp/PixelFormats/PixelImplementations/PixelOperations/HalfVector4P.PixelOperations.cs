// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides bulk operations.
/// </content>
public partial struct HalfVector4P
{
    /// <summary>
    /// Provides bulk operations for <see cref="HalfVector4P"/>.
    /// </summary>
    internal class PixelOperations : AssociatedAlphaPixelOperations<HalfVector4P>
    {
        /// <inheritdoc />
        internal override Vector4 ToUnassociatedScaledVector4(HalfVector4P source)
        {
            Vector4 vector = source.ToScaledVector4();
            Numerics.UnPremultiply(ref vector);
            return vector;
        }

        /// <inheritdoc />
        internal override HalfVector4P FromUnassociatedScaledVector4(Vector4 source)
            => FromScaledVector4(Associate(source));

        /// <inheritdoc />
        public override HalfVector4P FromAssociatedScaledVector4(Vector4 source)
            => FromScaledVector4(Reassociate(source));

        /// <inheritdoc />
        internal override void ToUnassociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfVector4P> source, Span<Vector4> destination)
        {
            this.ToVector4(configuration, source, destination, PixelConversionModifiers.Scale);
            Numerics.UnPremultiply(destination[..source.Length]);
        }

        /// <inheritdoc />
        internal override void FromUnassociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            source = source[..destination.Length];
            Associate(source);
            this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
        }

        /// <inheritdoc />
        public override void FromAssociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<HalfVector4P> destination)
        {
            source = source[..destination.Length];
            Reassociate(source);
            this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
        }
    }
}
