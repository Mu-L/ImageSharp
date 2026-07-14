// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.ImageSharp.PixelFormats.PixelBlenders;

/// <summary>
/// Provides pixel blenders for formats that store associated alpha.
/// </summary>
internal static partial class AssociatedAlphaPixelBlenders<TPixel>
    where TPixel : unmanaged, IPixel<TPixel>
{
    /// <summary>
    /// Gets the blender for the requested color blending and alpha composition modes.
    /// </summary>
    /// <param name="colorMode">The color blending mode.</param>
    /// <param name="alphaMode">The alpha composition mode.</param>
    /// <returns>The pixel blender.</returns>
    public static PixelBlender<TPixel> GetPixelBlender(PixelColorBlendingMode colorMode, PixelAlphaCompositionMode alphaMode)
    {
        return alphaMode switch
        {
            PixelAlphaCompositionMode.Src => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplySrc.Instance,
                PixelColorBlendingMode.Add => AddSrc.Instance,
                PixelColorBlendingMode.Subtract => SubtractSrc.Instance,
                PixelColorBlendingMode.Screen => ScreenSrc.Instance,
                PixelColorBlendingMode.Darken => DarkenSrc.Instance,
                PixelColorBlendingMode.Lighten => LightenSrc.Instance,
                PixelColorBlendingMode.Overlay => OverlaySrc.Instance,
                PixelColorBlendingMode.HardLight => HardLightSrc.Instance,
                _ => NormalSrc.Instance,
            },
            PixelAlphaCompositionMode.SrcAtop => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplySrcAtop.Instance,
                PixelColorBlendingMode.Add => AddSrcAtop.Instance,
                PixelColorBlendingMode.Subtract => SubtractSrcAtop.Instance,
                PixelColorBlendingMode.Screen => ScreenSrcAtop.Instance,
                PixelColorBlendingMode.Darken => DarkenSrcAtop.Instance,
                PixelColorBlendingMode.Lighten => LightenSrcAtop.Instance,
                PixelColorBlendingMode.Overlay => OverlaySrcAtop.Instance,
                PixelColorBlendingMode.HardLight => HardLightSrcAtop.Instance,
                _ => NormalSrcAtop.Instance,
            },
            PixelAlphaCompositionMode.SrcIn => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplySrcIn.Instance,
                PixelColorBlendingMode.Add => AddSrcIn.Instance,
                PixelColorBlendingMode.Subtract => SubtractSrcIn.Instance,
                PixelColorBlendingMode.Screen => ScreenSrcIn.Instance,
                PixelColorBlendingMode.Darken => DarkenSrcIn.Instance,
                PixelColorBlendingMode.Lighten => LightenSrcIn.Instance,
                PixelColorBlendingMode.Overlay => OverlaySrcIn.Instance,
                PixelColorBlendingMode.HardLight => HardLightSrcIn.Instance,
                _ => NormalSrcIn.Instance,
            },
            PixelAlphaCompositionMode.SrcOut => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplySrcOut.Instance,
                PixelColorBlendingMode.Add => AddSrcOut.Instance,
                PixelColorBlendingMode.Subtract => SubtractSrcOut.Instance,
                PixelColorBlendingMode.Screen => ScreenSrcOut.Instance,
                PixelColorBlendingMode.Darken => DarkenSrcOut.Instance,
                PixelColorBlendingMode.Lighten => LightenSrcOut.Instance,
                PixelColorBlendingMode.Overlay => OverlaySrcOut.Instance,
                PixelColorBlendingMode.HardLight => HardLightSrcOut.Instance,
                _ => NormalSrcOut.Instance,
            },
            PixelAlphaCompositionMode.Dest => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyDest.Instance,
                PixelColorBlendingMode.Add => AddDest.Instance,
                PixelColorBlendingMode.Subtract => SubtractDest.Instance,
                PixelColorBlendingMode.Screen => ScreenDest.Instance,
                PixelColorBlendingMode.Darken => DarkenDest.Instance,
                PixelColorBlendingMode.Lighten => LightenDest.Instance,
                PixelColorBlendingMode.Overlay => OverlayDest.Instance,
                PixelColorBlendingMode.HardLight => HardLightDest.Instance,
                _ => NormalDest.Instance,
            },
            PixelAlphaCompositionMode.DestAtop => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyDestAtop.Instance,
                PixelColorBlendingMode.Add => AddDestAtop.Instance,
                PixelColorBlendingMode.Subtract => SubtractDestAtop.Instance,
                PixelColorBlendingMode.Screen => ScreenDestAtop.Instance,
                PixelColorBlendingMode.Darken => DarkenDestAtop.Instance,
                PixelColorBlendingMode.Lighten => LightenDestAtop.Instance,
                PixelColorBlendingMode.Overlay => OverlayDestAtop.Instance,
                PixelColorBlendingMode.HardLight => HardLightDestAtop.Instance,
                _ => NormalDestAtop.Instance,
            },
            PixelAlphaCompositionMode.DestOver => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyDestOver.Instance,
                PixelColorBlendingMode.Add => AddDestOver.Instance,
                PixelColorBlendingMode.Subtract => SubtractDestOver.Instance,
                PixelColorBlendingMode.Screen => ScreenDestOver.Instance,
                PixelColorBlendingMode.Darken => DarkenDestOver.Instance,
                PixelColorBlendingMode.Lighten => LightenDestOver.Instance,
                PixelColorBlendingMode.Overlay => OverlayDestOver.Instance,
                PixelColorBlendingMode.HardLight => HardLightDestOver.Instance,
                _ => NormalDestOver.Instance,
            },
            PixelAlphaCompositionMode.DestIn => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyDestIn.Instance,
                PixelColorBlendingMode.Add => AddDestIn.Instance,
                PixelColorBlendingMode.Subtract => SubtractDestIn.Instance,
                PixelColorBlendingMode.Screen => ScreenDestIn.Instance,
                PixelColorBlendingMode.Darken => DarkenDestIn.Instance,
                PixelColorBlendingMode.Lighten => LightenDestIn.Instance,
                PixelColorBlendingMode.Overlay => OverlayDestIn.Instance,
                PixelColorBlendingMode.HardLight => HardLightDestIn.Instance,
                _ => NormalDestIn.Instance,
            },
            PixelAlphaCompositionMode.DestOut => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyDestOut.Instance,
                PixelColorBlendingMode.Add => AddDestOut.Instance,
                PixelColorBlendingMode.Subtract => SubtractDestOut.Instance,
                PixelColorBlendingMode.Screen => ScreenDestOut.Instance,
                PixelColorBlendingMode.Darken => DarkenDestOut.Instance,
                PixelColorBlendingMode.Lighten => LightenDestOut.Instance,
                PixelColorBlendingMode.Overlay => OverlayDestOut.Instance,
                PixelColorBlendingMode.HardLight => HardLightDestOut.Instance,
                _ => NormalDestOut.Instance,
            },
            PixelAlphaCompositionMode.Clear => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyClear.Instance,
                PixelColorBlendingMode.Add => AddClear.Instance,
                PixelColorBlendingMode.Subtract => SubtractClear.Instance,
                PixelColorBlendingMode.Screen => ScreenClear.Instance,
                PixelColorBlendingMode.Darken => DarkenClear.Instance,
                PixelColorBlendingMode.Lighten => LightenClear.Instance,
                PixelColorBlendingMode.Overlay => OverlayClear.Instance,
                PixelColorBlendingMode.HardLight => HardLightClear.Instance,
                _ => NormalClear.Instance,
            },
            PixelAlphaCompositionMode.Xor => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplyXor.Instance,
                PixelColorBlendingMode.Add => AddXor.Instance,
                PixelColorBlendingMode.Subtract => SubtractXor.Instance,
                PixelColorBlendingMode.Screen => ScreenXor.Instance,
                PixelColorBlendingMode.Darken => DarkenXor.Instance,
                PixelColorBlendingMode.Lighten => LightenXor.Instance,
                PixelColorBlendingMode.Overlay => OverlayXor.Instance,
                PixelColorBlendingMode.HardLight => HardLightXor.Instance,
                _ => NormalXor.Instance,
            },
            _ => colorMode switch
            {
                PixelColorBlendingMode.Multiply => MultiplySrcOver.Instance,
                PixelColorBlendingMode.Add => AddSrcOver.Instance,
                PixelColorBlendingMode.Subtract => SubtractSrcOver.Instance,
                PixelColorBlendingMode.Screen => ScreenSrcOver.Instance,
                PixelColorBlendingMode.Darken => DarkenSrcOver.Instance,
                PixelColorBlendingMode.Lighten => LightenSrcOver.Instance,
                PixelColorBlendingMode.Overlay => OverlaySrcOver.Instance,
                PixelColorBlendingMode.HardLight => HardLightSrcOver.Instance,
                _ => NormalSrcOver.Instance,
            },
        };
    }
}
