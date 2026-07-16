// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SixLabors.ImageSharp.PixelFormats.Utils;

internal static partial class Vector4Converters
{
    /// <summary>
    /// Multiplies each vector component and then adds the corresponding offset component.
    /// </summary>
    /// <param name="vectors">The vectors to transform in place.</param>
    /// <param name="multiplier">The component-wise multiplier.</param>
    /// <param name="offset">The component-wise offset applied after multiplication.</param>
    internal static void MultiplyThenAdd(Span<Vector4> vectors, Vector4 multiplier, Vector4 offset)
    {
        ref Vector4 vectorBase = ref MemoryMarshal.GetReference(vectors);
        int index = 0;

        if (Vector512.IsHardwareAccelerated)
        {
            int vectorsPerVector = Vector512<float>.Count / Vector128<float>.Count;
            Vector256<float> multiplier256 = Vector256.Create(multiplier.AsVector128(), multiplier.AsVector128());
            Vector256<float> offset256 = Vector256.Create(offset.AsVector128(), offset.AsVector128());
            Vector512<float> multiplier512 = Vector512.Create(multiplier256, multiplier256);
            Vector512<float> offset512 = Vector512.Create(offset256, offset256);

            for (; index <= vectors.Length - vectorsPerVector; index += vectorsPerVector)
            {
                ref Vector512<float> vector = ref Unsafe.As<Vector4, Vector512<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector * multiplier512) + offset512;
            }
        }

        if (Vector256.IsHardwareAccelerated)
        {
            int vectorsPerVector = Vector256<float>.Count / Vector128<float>.Count;
            Vector256<float> multiplier256 = Vector256.Create(multiplier.AsVector128(), multiplier.AsVector128());
            Vector256<float> offset256 = Vector256.Create(offset.AsVector128(), offset.AsVector128());

            for (; index <= vectors.Length - vectorsPerVector; index += vectorsPerVector)
            {
                ref Vector256<float> vector = ref Unsafe.As<Vector4, Vector256<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector * multiplier256) + offset256;
            }
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> multiplier128 = multiplier.AsVector128();
            Vector128<float> offset128 = offset.AsVector128();

            for (; index < vectors.Length; index++)
            {
                ref Vector128<float> vector = ref Unsafe.As<Vector4, Vector128<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector * multiplier128) + offset128;
            }

            return;
        }

        // The scalar fallback retains the same multiply-then-add order as the SIMD paths and the per-pixel contracts.
        for (; index < vectors.Length; index++)
        {
            ref Vector4 vector = ref Unsafe.Add(ref vectorBase, (uint)index);
            vector = (vector * multiplier) + offset;
        }
    }

    /// <summary>
    /// Adds the corresponding offset component and then divides each vector component by its divisor.
    /// </summary>
    /// <param name="vectors">The vectors to transform in place.</param>
    /// <param name="offset">The component-wise offset applied before division.</param>
    /// <param name="divisor">The component-wise divisor.</param>
    internal static void AddThenDivide(Span<Vector4> vectors, Vector4 offset, Vector4 divisor)
    {
        ref Vector4 vectorBase = ref MemoryMarshal.GetReference(vectors);
        int index = 0;

        if (Vector512.IsHardwareAccelerated)
        {
            int vectorsPerVector = Vector512<float>.Count / Vector128<float>.Count;
            Vector256<float> offset256 = Vector256.Create(offset.AsVector128(), offset.AsVector128());
            Vector256<float> divisor256 = Vector256.Create(divisor.AsVector128(), divisor.AsVector128());
            Vector512<float> offset512 = Vector512.Create(offset256, offset256);
            Vector512<float> divisor512 = Vector512.Create(divisor256, divisor256);

            for (; index <= vectors.Length - vectorsPerVector; index += vectorsPerVector)
            {
                ref Vector512<float> vector = ref Unsafe.As<Vector4, Vector512<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector + offset512) / divisor512;
            }
        }

        if (Vector256.IsHardwareAccelerated)
        {
            int vectorsPerVector = Vector256<float>.Count / Vector128<float>.Count;
            Vector256<float> offset256 = Vector256.Create(offset.AsVector128(), offset.AsVector128());
            Vector256<float> divisor256 = Vector256.Create(divisor.AsVector128(), divisor.AsVector128());

            for (; index <= vectors.Length - vectorsPerVector; index += vectorsPerVector)
            {
                ref Vector256<float> vector = ref Unsafe.As<Vector4, Vector256<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector + offset256) / divisor256;
            }
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> offset128 = offset.AsVector128();
            Vector128<float> divisor128 = divisor.AsVector128();

            for (; index < vectors.Length; index++)
            {
                ref Vector128<float> vector = ref Unsafe.As<Vector4, Vector128<float>>(ref Unsafe.Add(ref vectorBase, (uint)index));
                vector = (vector + offset128) / divisor128;
            }

            return;
        }

        // Native-to-scaled conversion deliberately adds before dividing to match each format's scalar conversion order.
        for (; index < vectors.Length; index++)
        {
            ref Vector4 vector = ref Unsafe.Add(ref vectorBase, (uint)index);
            vector = (vector + offset) / divisor;
        }
    }
}
