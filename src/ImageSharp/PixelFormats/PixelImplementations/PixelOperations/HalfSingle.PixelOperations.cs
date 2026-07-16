// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.ImageSharp.PixelFormats.Utils;

namespace SixLabors.ImageSharp.PixelFormats;

/// <content>
/// Provides optimized overrides for bulk operations.
/// </content>
public partial struct HalfSingle
{
    /// <summary>
    /// Provides optimized overrides for bulk operations.
    /// </summary>
    internal class PixelOperations : PixelOperations<HalfSingle>
    {
        private static readonly Vector4 NativeOffset = new(1F, 0F, 0F, 0F);
        private static readonly Vector4 NativeDivisor = new(2F, 1F, 1F, 1F);

        // Alpha is implicitly one, so both outward representations already contain associated color components.

        /// <inheritdoc />
        protected override void ToAssociatedVector4(Configuration configuration, ReadOnlySpan<HalfSingle> source, Span<Vector4> destination) => this.ToUnassociatedVector4(configuration, source, destination);

        /// <inheritdoc />
        protected override void ToAssociatedScaledVector4(Configuration configuration, ReadOnlySpan<HalfSingle> source, Span<Vector4> destination) => this.ToUnassociatedScaledVector4(configuration, source, destination);

        /// <inheritdoc />
        protected override void FromAssociatedVector4Destructive(Configuration configuration, Span<Vector4> source, Span<HalfSingle> destination)
        {
            // Only X uses the affine native range; W must remain normalized so the scaled path can unassociate the source color.
            Vector4Converters.AddThenDivide(source, NativeOffset, NativeDivisor);
            this.FromAssociatedScaledVector4Destructive(configuration, source, destination);
        }
    }
}
