// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using SixLabors.ImageSharp.Common.Helpers;
using SixLabors.ImageSharp.Formats.Jpeg.Components;
using SixLabors.ImageSharp.Formats.Jpeg.Components.Decoder;
using SixLabors.ImageSharp.IO;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Formats.Jpeg
{
    /// <summary>
    /// Performs the jpeg decoding operation.
    /// Originally ported from <see href="https://github.com/mozilla/pdf.js/blob/master/src/core/jpg.js"/>
    /// with additional fixes for both performance and common encoding errors.
    /// </summary>
    internal sealed class JpegDecoderCore : IRawJpegData, IImageDecoderInternals
    {
        /// <summary>
        /// The only supported precision
        /// </summary>
        private readonly byte[] supportedPrecisions = { 8, 12 };

        /// <summary>
        /// The buffer used to temporarily store bytes read from the stream.
        /// </summary>
        private readonly byte[] temp = new byte[2 * 16 * 4];

        /// <summary>
        /// The buffer used to read markers from the stream.
        /// </summary>
        private readonly byte[] markerBuffer = new byte[2];

        /// <summary>
        /// Whether the image has an EXIF marker.
        /// </summary>
        private bool isExif;

        /// <summary>
        /// Contains exif data.
        /// </summary>
        private byte[] exifData;

        /// <summary>
        /// Whether the image has an ICC marker.
        /// </summary>
        private bool isIcc;

        /// <summary>
        /// Contains ICC data.
        /// </summary>
        private byte[] iccData;

        /// <summary>
        /// Whether the image has a IPTC data.
        /// </summary>
        private bool isIptc;

        /// <summary>
        /// Contains IPTC data.
        /// </summary>
        private byte[] iptcData;

        /// <summary>
        /// Contains information about the JFIF marker.
        /// </summary>
        private JFifMarker jFif;

        /// <summary>
        /// Contains information about the Adobe marker.
        /// </summary>
        private AdobeMarker adobe;

        /// <summary>
        /// Scan decoder.
        /// </summary>
        private HuffmanScanDecoder scanDecoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="JpegDecoderCore" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="options">The options.</param>
        public JpegDecoderCore(Configuration configuration, IJpegDecoderOptions options)
        {
            this.Configuration = configuration ?? Configuration.Default;
            this.IgnoreMetadata = options.IgnoreMetadata;
        }

        /// <inheritdoc />
        public Configuration Configuration { get; }

        /// <summary>
        /// Gets the frame
        /// </summary>
        public JpegFrame Frame { get; private set; }

        /// <inheritdoc/>
        public Size ImageSizeInPixels { get; private set; }

        /// <inheritdoc/>
        Size IImageDecoderInternals.Dimensions => this.Frame.PixelSize;

        /// <summary>
        /// Gets the number of MCU blocks in the image as <see cref="Size"/>.
        /// </summary>
        public Size ImageSizeInMCU { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the metadata should be ignored when the image is being decoded.
        /// </summary>
        public bool IgnoreMetadata { get; }

        /// <summary>
        /// Gets the <see cref="ImageMetadata"/> decoded by this decoder instance.
        /// </summary>
        public ImageMetadata Metadata { get; private set; }

        /// <inheritdoc/>
        public JpegColorSpace ColorSpace { get; private set; }

        /// <summary>
        /// Gets the components.
        /// </summary>
        public JpegComponent[] Components => this.Frame.Components;

        /// <inheritdoc/>
        IJpegComponent[] IRawJpegData.Components => this.Components;

        /// <inheritdoc/>
        public Block8x8F[] QuantizationTables { get; private set; }

        /// <summary>
        /// Finds the next file marker within the byte stream.
        /// </summary>
        /// <param name="marker">The buffer to read file markers to</param>
        /// <param name="stream">The input stream</param>
        /// <returns>The <see cref="JpegFileMarker"/></returns>
        public static JpegFileMarker FindNextFileMarker(byte[] marker, BufferedReadStream stream)
        {
            int value = stream.Read(marker, 0, 2);

            if (value == 0)
            {
                return new JpegFileMarker(JpegConstants.Markers.EOI, stream.Length - 2);
            }

            if (marker[0] == JpegConstants.Markers.XFF)
            {
                // According to Section B.1.1.2:
                // "Any marker may optionally be preceded by any number of fill bytes, which are bytes assigned code 0xFF."
                int m = marker[1];
                while (m == JpegConstants.Markers.XFF)
                {
                    int suffix = stream.ReadByte();
                    if (suffix == -1)
                    {
                        return new JpegFileMarker(JpegConstants.Markers.EOI, stream.Length - 2);
                    }

                    m = suffix;
                }

                return new JpegFileMarker((byte)m, stream.Position - 2);
            }

            return new JpegFileMarker(marker[1], stream.Position - 2, true);
        }

        /// <inheritdoc/>
        public Image<TPixel> Decode<TPixel>(BufferedReadStream stream, CancellationToken cancellationToken)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            using var spectralConverter = new SpectralConverter<TPixel>(this.Configuration, cancellationToken);

            var scanDecoder = new HuffmanScanDecoder(stream, spectralConverter, cancellationToken);

            this.ParseStream(stream, scanDecoder, cancellationToken);
            this.InitExifProfile();
            this.InitIccProfile();
            this.InitIptcProfile();
            this.InitDerivedMetadataProperties();

            return new Image<TPixel>(this.Configuration, spectralConverter.PixelBuffer, this.Metadata);
        }

        /// <inheritdoc/>
        public IImageInfo Identify(BufferedReadStream stream, CancellationToken cancellationToken)
        {
            this.ParseStream(stream, scanDecoder: null, cancellationToken);
            this.InitExifProfile();
            this.InitIccProfile();
            this.InitIptcProfile();
            this.InitDerivedMetadataProperties();

            Size pixelSize = this.Frame.PixelSize;
            return new ImageInfo(new PixelTypeInfo(this.Frame.BitsPerPixel), pixelSize.Width, pixelSize.Height, this.Metadata);
        }

        /// <summary>
        /// Parses the input stream for file markers.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="scanDecoder">Scan decoder used exclusively to decode SOS marker.</param>
        /// <param name="cancellationToken">The token to monitor cancellation.</param>
        internal void ParseStream(BufferedReadStream stream, HuffmanScanDecoder scanDecoder, CancellationToken cancellationToken)
        {
            bool metadataOnly = scanDecoder == null;

            this.scanDecoder = scanDecoder;

            this.Metadata = new ImageMetadata();

            // Check for the Start Of Image marker.
            stream.Read(this.markerBuffer, 0, 2);
            var fileMarker = new JpegFileMarker(this.markerBuffer[1], 0);
            if (fileMarker.Marker != JpegConstants.Markers.SOI)
            {
                JpegThrowHelper.ThrowInvalidImageContentException("Missing SOI marker.");
            }

            stream.Read(this.markerBuffer, 0, 2);
            byte marker = this.markerBuffer[1];
            fileMarker = new JpegFileMarker(marker, (int)stream.Position - 2);
            this.QuantizationTables = new Block8x8F[4];

            // Break only when we discover a valid EOI marker.
            // https://github.com/SixLabors/ImageSharp/issues/695
            while (fileMarker.Marker != JpegConstants.Markers.EOI
                || (fileMarker.Marker == JpegConstants.Markers.EOI && fileMarker.Invalid))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileMarker.Invalid)
                {
                    // Get the marker length
                    int remaining = this.ReadUint16(stream) - 2;

                    switch (fileMarker.Marker)
                    {
                        case JpegConstants.Markers.SOF0:
                        case JpegConstants.Markers.SOF1:
                        case JpegConstants.Markers.SOF2:
                            this.ProcessStartOfFrameMarker(stream, remaining, fileMarker, metadataOnly);
                            break;

                        case JpegConstants.Markers.SOS:
                            if (!metadataOnly)
                            {
                                this.ProcessStartOfScanMarker(stream, cancellationToken);
                                break;
                            }
                            else
                            {
                                // It's highly unlikely that APPn related data will be found after the SOS marker
                                // We should have gathered everything we need by now.
                                return;
                            }

                        case JpegConstants.Markers.DHT:

                            if (metadataOnly)
                            {
                                stream.Skip(remaining);
                            }
                            else
                            {
                                this.ProcessDefineHuffmanTablesMarker(stream, remaining);
                            }

                            break;

                        case JpegConstants.Markers.DQT:
                            this.ProcessDefineQuantizationTablesMarker(stream, remaining);
                            break;

                        case JpegConstants.Markers.DRI:
                            if (metadataOnly)
                            {
                                stream.Skip(remaining);
                            }
                            else
                            {
                                this.ProcessDefineRestartIntervalMarker(stream, remaining);
                            }

                            break;

                        case JpegConstants.Markers.APP0:
                            this.ProcessApplicationHeaderMarker(stream, remaining);
                            break;

                        case JpegConstants.Markers.APP1:
                            this.ProcessApp1Marker(stream, remaining);
                            break;

                        case JpegConstants.Markers.APP2:
                            this.ProcessApp2Marker(stream, remaining);
                            break;

                        case JpegConstants.Markers.APP3:
                        case JpegConstants.Markers.APP4:
                        case JpegConstants.Markers.APP5:
                        case JpegConstants.Markers.APP6:
                        case JpegConstants.Markers.APP7:
                        case JpegConstants.Markers.APP8:
                        case JpegConstants.Markers.APP9:
                        case JpegConstants.Markers.APP10:
                        case JpegConstants.Markers.APP11:
                        case JpegConstants.Markers.APP12:
                            stream.Skip(remaining);
                            break;

                        case JpegConstants.Markers.APP13:
                            this.ProcessApp13Marker(stream, remaining);
                            break;

                        case JpegConstants.Markers.APP14:
                            this.ProcessApp14Marker(stream, remaining);
                            break;

                        case JpegConstants.Markers.APP15:
                        case JpegConstants.Markers.COM:
                            stream.Skip(remaining);
                            break;
                    }
                }

                // Read on.
                fileMarker = FindNextFileMarker(this.markerBuffer, stream);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Frame?.Dispose();

            // Set large fields to null.
            this.Frame = null;
            this.scanDecoder = null;
        }

        /// <summary>
        /// Returns the correct colorspace based on the image component count
        /// </summary>
        /// <returns>The <see cref="JpegColorSpace"/></returns>
        private JpegColorSpace DeduceJpegColorSpace(byte componentCount)
        {
            if (componentCount == 1)
            {
                return JpegColorSpace.Grayscale;
            }

            if (componentCount == 3)
            {
                if (!this.adobe.Equals(default) && this.adobe.ColorTransform == JpegConstants.Adobe.ColorTransformUnknown)
                {
                    return JpegColorSpace.RGB;
                }

                // Some images are poorly encoded and contain incorrect colorspace transform metadata.
                // We ignore that and always fall back to the default colorspace.
                return JpegColorSpace.YCbCr;
            }

            if (componentCount == 4)
            {
                return this.adobe.ColorTransform == JpegConstants.Adobe.ColorTransformYcck
                    ? JpegColorSpace.Ycck
                    : JpegColorSpace.Cmyk;
            }

            JpegThrowHelper.ThrowInvalidImageContentException($"Unsupported color mode. Supported component counts 1, 3, and 4; found {componentCount}");
            return default;
        }

        /// <summary>
        /// Initializes the EXIF profile.
        /// </summary>
        private void InitExifProfile()
        {
            if (this.isExif)
            {
                this.Metadata.ExifProfile = new ExifProfile(this.exifData);
            }
        }

        /// <summary>
        /// Initializes the ICC profile.
        /// </summary>
        private void InitIccProfile()
        {
            if (this.isIcc)
            {
                var profile = new IccProfile(this.iccData);
                if (profile.CheckIsValid())
                {
                    this.Metadata.IccProfile = profile;
                }
            }
        }

        /// <summary>
        /// Initializes the IPTC profile.
        /// </summary>
        private void InitIptcProfile()
        {
            if (this.isIptc)
            {
                var profile = new IptcProfile(this.iptcData);
                this.Metadata.IptcProfile = profile;
            }
        }

        /// <summary>
        /// Assigns derived metadata properties to <see cref="Metadata"/>, eg. horizontal and vertical resolution if it has a JFIF header.
        /// </summary>
        private void InitDerivedMetadataProperties()
        {
            if (this.jFif.XDensity > 0 && this.jFif.YDensity > 0)
            {
                this.Metadata.HorizontalResolution = this.jFif.XDensity;
                this.Metadata.VerticalResolution = this.jFif.YDensity;
                this.Metadata.ResolutionUnits = this.jFif.DensityUnits;
            }
            else if (this.isExif)
            {
                double horizontalValue = this.GetExifResolutionValue(ExifTag.XResolution);
                double verticalValue = this.GetExifResolutionValue(ExifTag.YResolution);

                if (horizontalValue > 0 && verticalValue > 0)
                {
                    this.Metadata.HorizontalResolution = horizontalValue;
                    this.Metadata.VerticalResolution = verticalValue;
                    this.Metadata.ResolutionUnits = UnitConverter.ExifProfileToResolutionUnit(this.Metadata.ExifProfile);
                }
            }
        }

        private double GetExifResolutionValue(ExifTag<Rational> tag)
        {
            IExifValue<Rational> resolution = this.Metadata.ExifProfile.GetValue(tag);

            return resolution is null ? 0 : resolution.Value.ToDouble();
        }

        /// <summary>
        /// Extends the profile with additional data.
        /// </summary>
        /// <param name="profile">The profile data array.</param>
        /// <param name="extension">The array containing addition profile data.</param>
        private void ExtendProfile(ref byte[] profile, byte[] extension)
        {
            int currentLength = profile.Length;

            Array.Resize(ref profile, currentLength + extension.Length);
            Buffer.BlockCopy(extension, 0, profile, currentLength, extension.Length);
        }

        /// <summary>
        /// Processes the application header containing the JFIF identifier plus extra data.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessApplicationHeaderMarker(BufferedReadStream stream, int remaining)
        {
            // We can only decode JFif identifiers.
            if (remaining < JFifMarker.Length)
            {
                // Skip the application header length
                stream.Skip(remaining);
                return;
            }

            stream.Read(this.temp, 0, JFifMarker.Length);
            remaining -= JFifMarker.Length;

            JFifMarker.TryParse(this.temp, out this.jFif);

            // TODO: thumbnail
            if (remaining > 0)
            {
                if (stream.Position + remaining >= stream.Length)
                {
                    JpegThrowHelper.ThrowInvalidImageContentException("Bad App0 Marker length.");
                }

                stream.Skip(remaining);
            }
        }

        /// <summary>
        /// Processes the App1 marker retrieving any stored metadata
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessApp1Marker(BufferedReadStream stream, int remaining)
        {
            const int Exif00 = 6;
            if (remaining < Exif00 || this.IgnoreMetadata)
            {
                // Skip the application header length
                stream.Skip(remaining);
                return;
            }

            if (stream.Position + remaining >= stream.Length)
            {
                JpegThrowHelper.ThrowInvalidImageContentException("Bad App1 Marker length.");
            }

            var profile = new byte[remaining];
            stream.Read(profile, 0, remaining);

            if (ProfileResolver.IsProfile(profile, ProfileResolver.ExifMarker))
            {
                this.isExif = true;
                if (this.exifData is null)
                {
                    // The first 6 bytes (Exif00) will be skipped, because this is Jpeg specific
                    this.exifData = profile.AsSpan(Exif00).ToArray();
                }
                else
                {
                    // If the EXIF information exceeds 64K, it will be split over multiple APP1 markers
                    this.ExtendProfile(ref this.exifData, profile.AsSpan(Exif00).ToArray());
                }
            }
        }

        /// <summary>
        /// Processes the App2 marker retrieving any stored ICC profile information
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessApp2Marker(BufferedReadStream stream, int remaining)
        {
            // Length is 14 though we only need to check 12.
            const int Icclength = 14;
            if (remaining < Icclength || this.IgnoreMetadata)
            {
                stream.Skip(remaining);
                return;
            }

            var identifier = new byte[Icclength];
            stream.Read(identifier, 0, Icclength);
            remaining -= Icclength; // We have read it by this point

            if (ProfileResolver.IsProfile(identifier, ProfileResolver.IccMarker))
            {
                this.isIcc = true;
                var profile = new byte[remaining];
                stream.Read(profile, 0, remaining);

                if (this.iccData is null)
                {
                    this.iccData = profile;
                }
                else
                {
                    // If the ICC information exceeds 64K, it will be split over multiple APP2 markers
                    this.ExtendProfile(ref this.iccData, profile);
                }
            }
            else
            {
                // Not an ICC profile we can handle. Skip the remaining bytes so we can carry on and ignore this.
                stream.Skip(remaining);
            }
        }

        /// <summary>
        /// Processes a App13 marker, which contains IPTC data stored with Adobe Photoshop.
        /// The content of an APP13 segment is formed by an identifier string followed by a sequence of resource data blocks.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessApp13Marker(BufferedReadStream stream, int remaining)
        {
            if (remaining < ProfileResolver.AdobePhotoshopApp13Marker.Length || this.IgnoreMetadata)
            {
                stream.Skip(remaining);
                return;
            }

            stream.Read(this.temp, 0, ProfileResolver.AdobePhotoshopApp13Marker.Length);
            remaining -= ProfileResolver.AdobePhotoshopApp13Marker.Length;
            if (ProfileResolver.IsProfile(this.temp, ProfileResolver.AdobePhotoshopApp13Marker))
            {
                var resourceBlockData = new byte[remaining];
                stream.Read(resourceBlockData, 0, remaining);
                Span<byte> blockDataSpan = resourceBlockData.AsSpan();

                while (blockDataSpan.Length > 12)
                {
                    if (!ProfileResolver.IsProfile(blockDataSpan.Slice(0, 4), ProfileResolver.AdobeImageResourceBlockMarker))
                    {
                        return;
                    }

                    blockDataSpan = blockDataSpan.Slice(4);
                    Span<byte> imageResourceBlockId = blockDataSpan.Slice(0, 2);
                    if (ProfileResolver.IsProfile(imageResourceBlockId, ProfileResolver.AdobeIptcMarker))
                    {
                        var resourceBlockNameLength = ReadImageResourceNameLength(blockDataSpan);
                        var resourceDataSize = ReadResourceDataLength(blockDataSpan, resourceBlockNameLength);
                        int dataStartIdx = 2 + resourceBlockNameLength + 4;
                        if (resourceDataSize > 0 && blockDataSpan.Length >= dataStartIdx + resourceDataSize)
                        {
                            this.isIptc = true;
                            this.iptcData = blockDataSpan.Slice(dataStartIdx, resourceDataSize).ToArray();
                            break;
                        }
                    }
                    else
                    {
                        var resourceBlockNameLength = ReadImageResourceNameLength(blockDataSpan);
                        var resourceDataSize = ReadResourceDataLength(blockDataSpan, resourceBlockNameLength);
                        int dataStartIdx = 2 + resourceBlockNameLength + 4;
                        if (blockDataSpan.Length < dataStartIdx + resourceDataSize)
                        {
                            // Not enough data or the resource data size is wrong.
                            break;
                        }

                        blockDataSpan = blockDataSpan.Slice(dataStartIdx + resourceDataSize);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the adobe image resource block name: a Pascal string (padded to make size even).
        /// </summary>
        /// <param name="blockDataSpan">The span holding the block resource data.</param>
        /// <returns>The length of the name.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private static int ReadImageResourceNameLength(Span<byte> blockDataSpan)
        {
            byte nameLength = blockDataSpan[2];
            var nameDataSize = nameLength == 0 ? 2 : nameLength;
            if (nameDataSize % 2 != 0)
            {
                nameDataSize++;
            }

            return nameDataSize;
        }

        /// <summary>
        /// Reads the length of a adobe image resource data block.
        /// </summary>
        /// <param name="blockDataSpan">The span holding the block resource data.</param>
        /// <param name="resourceBlockNameLength">The length of the block name.</param>
        /// <returns>The block length.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private static int ReadResourceDataLength(Span<byte> blockDataSpan, int resourceBlockNameLength)
        {
            return BinaryPrimitives.ReadInt32BigEndian(blockDataSpan.Slice(2 + resourceBlockNameLength, 4));
        }

        /// <summary>
        /// Processes the application header containing the Adobe identifier
        /// which stores image encoding information for DCT filters.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessApp14Marker(BufferedReadStream stream, int remaining)
        {
            const int MarkerLength = AdobeMarker.Length;
            if (remaining < MarkerLength)
            {
                // Skip the application header length
                stream.Skip(remaining);
                return;
            }

            stream.Read(this.temp, 0, MarkerLength);
            remaining -= MarkerLength;

            AdobeMarker.TryParse(this.temp, out this.adobe);

            if (remaining > 0)
            {
                stream.Skip(remaining);
            }
        }

        /// <summary>
        /// Processes the Define Quantization Marker and tables. Specified in section B.2.4.1.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        /// <exception cref="ImageFormatException">
        /// Thrown if the tables do not match the header
        /// </exception>
        private void ProcessDefineQuantizationTablesMarker(BufferedReadStream stream, int remaining)
        {
            while (remaining > 0)
            {
                bool done = false;
                remaining--;
                int quantizationTableSpec = stream.ReadByte();
                int tableIndex = quantizationTableSpec & 15;

                // Max index. 4 Tables max.
                if (tableIndex > 3)
                {
                    JpegThrowHelper.ThrowBadQuantizationTable();
                }

                switch (quantizationTableSpec >> 4)
                {
                    case 0:
                    {
                        // 8 bit values
                        if (remaining < 64)
                        {
                            done = true;
                            break;
                        }

                        stream.Read(this.temp, 0, 64);
                        remaining -= 64;

                        ref Block8x8F table = ref this.QuantizationTables[tableIndex];
                        for (int j = 0; j < 64; j++)
                        {
                            table[j] = this.temp[j];
                        }
                    }

                    break;
                    case 1:
                    {
                        // 16 bit values
                        if (remaining < 128)
                        {
                            done = true;
                            break;
                        }

                        stream.Read(this.temp, 0, 128);
                        remaining -= 128;

                        ref Block8x8F table = ref this.QuantizationTables[tableIndex];
                        for (int j = 0; j < 64; j++)
                        {
                            table[j] = (this.temp[2 * j] << 8) | this.temp[(2 * j) + 1];
                        }
                    }

                    break;

                    default:
                    {
                        JpegThrowHelper.ThrowBadQuantizationTable();
                        break;
                    }
                }

                if (done)
                {
                    break;
                }
            }

            if (remaining != 0)
            {
                JpegThrowHelper.ThrowBadMarker(nameof(JpegConstants.Markers.DQT), remaining);
            }

            this.Metadata.GetFormatMetadata(JpegFormat.Instance).Quality = QualityEvaluator.EstimateQuality(this.QuantizationTables);
        }

        /// <summary>
        /// Processes the Start of Frame marker.  Specified in section B.2.2.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        /// <param name="frameMarker">The current frame marker.</param>
        /// <param name="metadataOnly">Whether to parse metadata only</param>
        private void ProcessStartOfFrameMarker(BufferedReadStream stream, int remaining, in JpegFileMarker frameMarker, bool metadataOnly)
        {
            if (this.Frame != null)
            {
                if (metadataOnly)
                {
                    return;
                }

                JpegThrowHelper.ThrowInvalidImageContentException("Multiple SOF markers. Only single frame jpegs supported.");
            }

            // Read initial marker definitions
            const int length = 6;
            stream.Read(this.temp, 0, length);

            // 1 byte: Bits/sample precision
            byte precision = this.temp[0];

            // Validity check: only 8-bit and 12-bit precisions are supported
            if (Array.IndexOf(this.supportedPrecisions, precision) == -1)
            {
                JpegThrowHelper.ThrowInvalidImageContentException("Only 8-Bit and 12-Bit precision supported.");
            }

            // 2 byte: Height
            int frameHeight = (this.temp[1] << 8) | this.temp[2];

            // 2 byte: Width
            int frameWidth = (this.temp[3] << 8) | this.temp[4];

            // Validity check: width/height > 0 (they are upper-bounded by 2 byte max value so no need to check that)
            if (frameHeight == 0 || frameWidth == 0)
            {
                JpegThrowHelper.ThrowInvalidImageDimensions(frameWidth, frameHeight);
            }

            // 1 byte: Number of components
            byte componentCount = this.temp[5];
            this.ColorSpace = this.DeduceJpegColorSpace(componentCount);

            this.Frame = new JpegFrame
            {
                Extended = frameMarker.Marker == JpegConstants.Markers.SOF1,
                Progressive = frameMarker.Marker == JpegConstants.Markers.SOF2,
                Precision = precision,
                PixelHeight = frameHeight,
                PixelWidth = frameWidth,
                ComponentCount = componentCount
            };

            this.Metadata.GetJpegMetadata().ColorType = this.ColorSpace == JpegColorSpace.Grayscale ? JpegColorType.Luminance : JpegColorType.YCbCr;

            if (!metadataOnly)
            {
                remaining -= length;

                const int componentBytes = 3;
                if (remaining > componentCount * componentBytes)
                {
                    JpegThrowHelper.ThrowBadMarker("SOFn", remaining);
                }

                stream.Read(this.temp, 0, remaining);

                // No need to pool this. They max out at 4
                this.Frame.ComponentIds = new byte[componentCount];
                this.Frame.ComponentOrder = new byte[componentCount];
                this.Frame.Components = new JpegComponent[componentCount];

                int maxH = 0;
                int maxV = 0;
                int index = 0;
                for (int i = 0; i < componentCount; i++)
                {
                    byte hv = this.temp[index + 1];
                    int h = (hv >> 4) & 15;
                    int v = hv & 15;

                    if (maxH < h)
                    {
                        maxH = h;
                    }

                    if (maxV < v)
                    {
                        maxV = v;
                    }

                    var component = new JpegComponent(this.Configuration.MemoryAllocator, this.Frame, this.temp[index], h, v, this.temp[index + 2], i);

                    this.Frame.Components[i] = component;
                    this.Frame.ComponentIds[i] = component.Id;

                    index += componentBytes;
                }

                this.Frame.MaxHorizontalFactor = maxH;
                this.Frame.MaxVerticalFactor = maxV;
                this.Frame.InitComponents();

                this.ImageSizeInMCU = new Size(this.Frame.McusPerLine, this.Frame.McusPerColumn);

                // This can be injected in SOF marker callback
                this.scanDecoder.InjectFrameData(this.Frame, this);
            }
        }

        /// <summary>
        /// Processes a Define Huffman Table marker, and initializes a huffman
        /// struct from its contents. Specified in section B.2.4.2.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessDefineHuffmanTablesMarker(BufferedReadStream stream, int remaining)
        {
            int length = remaining;

            using (IMemoryOwner<byte> huffmanData = this.Configuration.MemoryAllocator.Allocate<byte>(256, AllocationOptions.Clean))
            {
                Span<byte> huffmanDataSpan = huffmanData.GetSpan();
                ref byte huffmanDataRef = ref MemoryMarshal.GetReference(huffmanDataSpan);
                for (int i = 2; i < remaining;)
                {
                    byte huffmanTableSpec = (byte)stream.ReadByte();
                    int tableType = huffmanTableSpec >> 4;
                    int tableIndex = huffmanTableSpec & 15;

                    // Types 0..1 DC..AC
                    if (tableType > 1)
                    {
                        JpegThrowHelper.ThrowInvalidImageContentException("Bad Huffman Table type.");
                    }

                    // Max tables of each type
                    if (tableIndex > 3)
                    {
                        JpegThrowHelper.ThrowInvalidImageContentException("Bad Huffman Table index.");
                    }

                    stream.Read(huffmanDataSpan, 0, 16);

                    using (IMemoryOwner<byte> codeLengths = this.Configuration.MemoryAllocator.Allocate<byte>(17, AllocationOptions.Clean))
                    {
                        Span<byte> codeLengthsSpan = codeLengths.GetSpan();
                        ref byte codeLengthsRef = ref MemoryMarshal.GetReference(codeLengthsSpan);
                        int codeLengthSum = 0;

                        for (int j = 1; j < 17; j++)
                        {
                            codeLengthSum += Unsafe.Add(ref codeLengthsRef, j) = Unsafe.Add(ref huffmanDataRef, j - 1);
                        }

                        length -= 17;

                        if (codeLengthSum > 256 || codeLengthSum > length)
                        {
                            JpegThrowHelper.ThrowInvalidImageContentException("Huffman table has excessive length.");
                        }

                        using (IMemoryOwner<byte> huffmanValues = this.Configuration.MemoryAllocator.Allocate<byte>(256, AllocationOptions.Clean))
                        {
                            Span<byte> huffmanValuesSpan = huffmanValues.GetSpan();
                            stream.Read(huffmanValuesSpan, 0, codeLengthSum);

                            i += 17 + codeLengthSum;

                            this.scanDecoder.BuildHuffmanTable(
                                tableType,
                                tableIndex,
                                codeLengthsSpan,
                                huffmanValuesSpan);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes the DRI (Define Restart Interval Marker) Which specifies the interval between RSTn markers, in
        /// macroblocks
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="remaining">The remaining bytes in the segment block.</param>
        private void ProcessDefineRestartIntervalMarker(BufferedReadStream stream, int remaining)
        {
            if (remaining != 2)
            {
                JpegThrowHelper.ThrowBadMarker(nameof(JpegConstants.Markers.DRI), remaining);
            }

            this.scanDecoder.ResetInterval = this.ReadUint16(stream);
        }

        /// <summary>
        /// Processes the SOS (Start of scan marker).
        /// </summary>
        private void ProcessStartOfScanMarker(BufferedReadStream stream, CancellationToken cancellationToken)
        {
            if (this.Frame is null)
            {
                JpegThrowHelper.ThrowInvalidImageContentException("No readable SOFn (Start Of Frame) marker found.");
            }

            int selectorsCount = stream.ReadByte();
            this.Frame.MultiScan = this.Frame.ComponentCount != selectorsCount;
            for (int i = 0; i < selectorsCount; i++)
            {
                int componentIndex = -1;
                int selector = stream.ReadByte();

                for (int j = 0; j < this.Frame.ComponentIds.Length; j++)
                {
                    byte id = this.Frame.ComponentIds[j];
                    if (selector == id)
                    {
                        componentIndex = j;
                        break;
                    }
                }

                if (componentIndex < 0)
                {
                    JpegThrowHelper.ThrowInvalidImageContentException($"Unknown component selector {componentIndex}.");
                }

                ref JpegComponent component = ref this.Frame.Components[componentIndex];
                int tableSpec = stream.ReadByte();
                component.DCHuffmanTableId = tableSpec >> 4;
                component.ACHuffmanTableId = tableSpec & 15;
                this.Frame.ComponentOrder[i] = (byte)componentIndex;
            }

            stream.Read(this.temp, 0, 3);

            int spectralStart = this.temp[0];
            int spectralEnd = this.temp[1];
            int successiveApproximation = this.temp[2];

            // This is okay to inject here, might be good to wrap it in a separate struct but not really necessary
            this.scanDecoder.SpectralStart = spectralStart;
            this.scanDecoder.SpectralEnd = spectralEnd;
            this.scanDecoder.SuccessiveHigh = successiveApproximation >> 4;
            this.scanDecoder.SuccessiveLow = successiveApproximation & 15;

            this.scanDecoder.ParseEntropyCodedData(selectorsCount);
        }

        /// <summary>
        /// Reads a <see cref="ushort"/> from the stream advancing it by two bytes
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <returns>The <see cref="ushort"/></returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private ushort ReadUint16(BufferedReadStream stream)
        {
            stream.Read(this.markerBuffer, 0, 2);
            return BinaryPrimitives.ReadUInt16BigEndian(this.markerBuffer);
        }
    }
}
