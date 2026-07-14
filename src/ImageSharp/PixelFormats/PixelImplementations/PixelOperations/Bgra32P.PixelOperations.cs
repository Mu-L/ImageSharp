// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.ImageSharp.PixelFormats.Utils;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides optimized overrides for bulk operations.
/// </content>
public partial struct Bgra32P
{
    /// <summary>
    /// Provides optimized bulk operations for <see cref="Bgra32P"/>.
    /// </summary>
    internal class PixelOperations : AssociatedAlphaPixelOperations<Bgra32P>
    {
        /// <inheritdoc />
        internal override Vector4 ToUnassociatedScaledVector4(Bgra32P source)
            => Vector4Converters.AssociatedRgbaCompatible.ToUnassociatedVector4(source.R, source.G, source.B, source.A);

        /// <inheritdoc />
        internal override Bgra32P FromUnassociatedScaledVector4(Vector4 source)
            => FromScaledVector4(Vector4Converters.AssociatedRgbaCompatible.Associate(source));

        /// <inheritdoc />
        public override Bgra32P FromAssociatedScaledVector4(Vector4 source)
            => Vector4Converters.AssociatedRgbaCompatible.FromAssociatedVector4ToBgra32P(source);

        /// <inheritdoc />
        internal override void ToUnassociatedScaledVector4(Configuration configuration, ReadOnlySpan<Bgra32P> source, Span<Vector4> destination)
            => Vector4Converters.AssociatedRgbaCompatible.ToUnassociatedVector4(source, destination);

        /// <inheritdoc />
        internal override void FromUnassociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<Bgra32P> destination)
        {
            source = source[..destination.Length];
            Vector4Converters.AssociatedRgbaCompatible.Associate(source);
            this.FromVector4Destructive(configuration, source, destination, PixelConversionModifiers.Scale);
        }

        /// <inheritdoc />
        public override void FromAssociatedScaledVector4(Configuration configuration, Span<Vector4> source, Span<Bgra32P> destination)
        {
            source = source[..destination.Length];
            Vector4Converters.AssociatedRgbaCompatible.FromAssociatedVector4(source, destination);
        }

        /// <inheritdoc />
        public override void ToVector4(
            Configuration configuration,
            ReadOnlySpan<Bgra32P> source,
            Span<Vector4> destinationVectors,
            PixelConversionModifiers modifiers)
            => Vector4Converters.AssociatedRgbaCompatible.ToVector4(source, destinationVectors, modifiers);

        /// <inheritdoc />
        public override void FromVector4Destructive(
            Configuration configuration,
            Span<Vector4> sourceVectors,
            Span<Bgra32P> destination,
            PixelConversionModifiers modifiers)
            => Vector4Converters.AssociatedRgbaCompatible.FromVector4(sourceVectors, destination, modifiers);
    }
}
