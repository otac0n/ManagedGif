// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using ManagedGif.Elements;

    public static class GifParser
    {
        public static IEnumerable<GifElement> ReadGif(Stream stream)
        {
            var header = new byte[6];
            if (stream.Read(header, 0, header.Length) != header.Length ||
                header[0] != (byte)'G' ||
                header[1] != (byte)'I' ||
                header[2] != (byte)'F')
            {
                yield return new DecoderError(Resources.HeaderNotFound, new Dictionary<string, object>
                {
                    ["Header"] = header,
                });
                yield break;
            }

            yield return new Header(
                version: Encoding.ASCII.GetString(header, 3, 3));

            var logicalScreenDescriptor = new byte[7];
            var readBytes = stream.Read(logicalScreenDescriptor, 0, logicalScreenDescriptor.Length);
            if (readBytes != logicalScreenDescriptor.Length)
            {
                yield return new DecoderError(Resources.EndOfStreamLogicalScreenDescriptor, new Dictionary<string, object>
                {
                    ["Read"] = readBytes,
                    ["Expected"] = logicalScreenDescriptor.Length,
                });
                yield break;
            }

            var logicalScreen = new LogicalScreenDescriptor(
                width: BitConverter.ToUInt16(logicalScreenDescriptor, 0),
                height: BitConverter.ToUInt16(logicalScreenDescriptor, 2),
                globalColorTableFlag: (logicalScreenDescriptor[4] & 0b10000000) != 0,
                colorResolution: ((logicalScreenDescriptor[4] & 0b01110000) >> 4) + 1,
                sortFlag: (logicalScreenDescriptor[4] & 0b00001000) != 0,
                sizeOfGlobalColorTable: 1 << ((logicalScreenDescriptor[4] & 0b00000111) + 1),
                backgroundColorIndex: logicalScreenDescriptor[5],
                pixelAspectRatio: logicalScreenDescriptor[6] == 0 ? default(double?) : (logicalScreenDescriptor[6] + 15) / 64.0);
            yield return logicalScreen;

            Color[] globalColorTable = null;
            if (logicalScreen.GlobalColorTableFlag)
            {
                var globalColorTableValues = new byte[3 * logicalScreen.SizeOfGlobalColorTable];
                readBytes = stream.Read(globalColorTableValues, 0, globalColorTableValues.Length);
                if (readBytes != globalColorTableValues.Length)
                {
                    yield return new DecoderError(Resources.EndOfStreamGlobalColorTable, new Dictionary<string, object>
                    {
                        ["Read"] = readBytes,
                        ["Expected"] = globalColorTableValues.Length,
                    });
                    yield break;
                }

                globalColorTable = new Color[logicalScreen.SizeOfGlobalColorTable];
                for (var i = 0; i < globalColorTable.Length; i++)
                {
                    globalColorTable[i] = Color.FromArgb(globalColorTableValues[i * 3], globalColorTableValues[i * 3 + 1], globalColorTableValues[i * 3 + 2]);
                }

                yield return new GlobalColorTable(globalColorTable);
            }

            byte[] ReadData()
            {
                var read = new List<byte>();
                while (true)
                {
                    var dataSize = stream.ReadByte();
                    if (dataSize == -1)
                    {
                        return null;
                    }

                    if (dataSize == 0)
                    {
                        break;
                    }

                    var data = new byte[dataSize];
                    if (stream.Read(data, 0, data.Length) != data.Length)
                    {
                        return null;
                    }

                    read.AddRange(data);
                }

                return read.ToArray();
            }

            while (true)
            {
                var separator = stream.ReadByte();
                switch (separator)
                {
                    case 0x2C:
                        var imageDescriptor = new byte[9];
                        readBytes = stream.Read(imageDescriptor, 0, imageDescriptor.Length);
                        if (readBytes != imageDescriptor.Length)
                        {
                            yield return new DecoderError(Resources.EndOfStreamImageDescriptor, new Dictionary<string, object>
                            {
                                ["Read"] = readBytes,
                                ["Expected"] = imageDescriptor.Length,
                            });
                            yield break;
                        }

                        var localColorTableFlag = (imageDescriptor[8] & 0b00100000) != 0;
                        var sizeOfLocalColorTableValue = imageDescriptor[8] & 0b00000111;
                        if (localColorTableFlag && sizeOfLocalColorTableValue != 0)
                        {
                            yield return new DecoderWarning(Resources.NonzeroLocalColorTableSizeValue, new Dictionary<string, object>
                            {
                                ["SizeOfLocalColorTableValue"] = sizeOfLocalColorTableValue,
                            });
                        }

                        var image = new ImageDescriptor(
                            left: BitConverter.ToUInt16(imageDescriptor, 0),
                            top: BitConverter.ToUInt16(imageDescriptor, 2),
                            width: BitConverter.ToUInt16(imageDescriptor, 4),
                            height: BitConverter.ToUInt16(imageDescriptor, 6),
                            localColorTableFlag: localColorTableFlag,
                            interlaceFlag: (imageDescriptor[8] & 0b01000000) != 0,
                            sortFlag: (imageDescriptor[8] & 0b00100000) != 0,
                            sizeOfLocalColorTable: localColorTableFlag ? 1 << (sizeOfLocalColorTableValue + 1) : 0);
                        yield return image;

                        Color[] localColorTable = null;
                        if (image.LocalColorTableFlag)
                        {
                            var localColorTableValues = new byte[3 * image.SizeOfLocalColorTable];
                            readBytes = stream.Read(localColorTableValues, 0, localColorTableValues.Length);
                            if (readBytes != localColorTableValues.Length)
                            {
                                yield return new DecoderError(Resources.EndOfStreamLocalColorTable, new Dictionary<string, object>
                                {
                                    ["Read"] = readBytes,
                                    ["Expected"] = localColorTable.Length,
                                });
                                yield break;
                            }

                            localColorTable = new Color[image.SizeOfLocalColorTable];
                            for (var i = 0; i < localColorTable.Length; i++)
                            {
                                localColorTable[i] = Color.FromArgb(localColorTableValues[i * 3], localColorTableValues[i * 3 + 1], localColorTableValues[i * 3 + 2]);
                            }

                            yield return new LocalColorTable(localColorTable);
                        }

                        var colorTable = localColorTable ?? globalColorTable;
                        if (colorTable == null)
                        {
                            yield return new DecoderWarning(Resources.NoColorTable);
                            break;
                        }

                        var minimumCodeSize = stream.ReadByte();
                        if (minimumCodeSize == -1)
                        {
                            yield return new DecoderError(Resources.EndOfStreamMinimumCodeSize);
                            yield break;
                        }

                        var tableSize = image.LocalColorTableFlag ? image.SizeOfLocalColorTable : logicalScreen.SizeOfGlobalColorTable;

                        var encoded = ReadData();
                        if (encoded == null)
                        {
                            yield return new DecoderError(Resources.EndOfStreamImageData);
                            yield break;
                        }

                        const int MaximumCodeValue = 0xFFF;
                        const int ClearCode = -1;
                        const int EndOfInformationCode = -2;
                        var decoded = new List<int>();
                        var codeTable = Enumerable.Range(0, 1 << minimumCodeSize).Concat(new[] { ClearCode, EndOfInformationCode }).Select(c => new[] { c }).ToList();
                        var endReached = false;

                        var readIndex = 0;
                        var readBit = 0;
                        Func<bool, int> readNext = (peek) =>
                        {
                            var index = readIndex;
                            var bit = readBit;

                            int value;
                            if (readIndex < encoded.Length)
                            {
                                value = 0;
                                var largestCode = codeTable.Count - 1 + (peek ? 1 : 0);
                                var bits = (int)Math.Log(largestCode, 2) + 1;
                                var readBits = Math.Max(minimumCodeSize, bits);
                                for (var writeBit = 0; writeBit < readBits && readIndex < encoded.Length; writeBit++)
                                {
                                    value |= ((encoded[readIndex] & (1 << readBit)) >> readBit) << writeBit;
                                    readBit++;
                                    if (readBit >= 8)
                                    {
                                        readBit = 0;
                                        readIndex++;
                                    }
                                }

                                // TODO: Warn if readBits > 0?
                            }
                            else
                            {
                                value = -1;
                            }

                            if (peek)
                            {
                                readBit = bit;
                                readIndex = index;
                            }

                            return value;
                        };

                        while (readIndex < encoded.Length && !endReached)
                        {
                            var value = readNext(false);
                            if (value >= codeTable.Count)
                            {
                                yield return new DecoderError(Resources.DecodedValueOutsideRange, new Dictionary<string, object>
                                {
                                    ["Value"] = value,
                                    ["CodeCount"] = codeTable.Count,
                                });
                                yield break;
                            }

                            var codes = codeTable[value];
                            if (codes[0] == ClearCode)
                            {
                                var startingSize = (1 << minimumCodeSize) + 2;
                                codeTable.RemoveRange(startingSize, codeTable.Count - startingSize);
                            }
                            else if (codes[0] == EndOfInformationCode)
                            {
                                // TODO: Warn if remaining data.
                                endReached = true;
                            }
                            else
                            {
                                foreach (var code in codes)
                                {
                                    decoded.Add(code);
                                }

                                if (codeTable.Count <= MaximumCodeValue)
                                {
                                    var peek = readNext(true);
                                    if (peek != -1)
                                    {
                                        int tail;
                                        if (peek < codeTable.Count)
                                        {
                                            tail = codeTable[peek][0];
                                        }
                                        else if (peek == codeTable.Count)
                                        {
                                            tail = codes[0];
                                        }
                                        else
                                        {
                                            yield return new DecoderError(Resources.DecodedValueOutsideRange, new Dictionary<string, object>
                                            {
                                                ["Value"] = peek,
                                                ["CodeCount"] = codeTable.Count,
                                            });
                                            yield break;
                                        }

                                        codeTable.Add(codes.Concat(new[] { tail }).ToArray());
                                    }
                                }
                            }
                        }

                        if (decoded.Count != image.Width * image.Height)
                        {
                            yield return new DecoderWarning(Resources.MismatchedPixelCount, new Dictionary<string, object>
                            {
                                ["Read"] = decoded.Count,
                                ["Expected"] = image.Width * image.Height,
                            });
                        }

                        var rasterImage = new Bitmap(image.Width, image.Height);
                        var bmpData = rasterImage.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                        var colorData = new int[image.Width];
                        var scan = bmpData.Scan0;
                        for (var y = 0; y < image.Height; y++, scan += bmpData.Stride)
                        {
                            for (var x = 0; x < image.Width; x++)
                            {
                                var i = x + y * image.Width;
                                colorData[x] = i < decoded.Count && decoded[i] < colorTable.Length
                                    ? colorTable[decoded[i]].ToArgb()
                                    : colorTable[0].ToArgb(); // TODO: Warning?
                            }

                            Marshal.Copy(colorData, 0, scan, image.Width);
                        }

                        rasterImage.UnlockBits(bmpData);

                        yield return new Raster(rasterImage);

                        break;

                    case 0x21:
                        var extension = stream.ReadByte();
                        if (extension == -1)
                        {
                            yield return new DecoderError(Resources.EndOfStreamExtensionIdentifier);
                            yield break;
                        }

                        var extensionData = ReadData();
                        if (extensionData == null)
                        {
                            yield return new DecoderError(Resources.EndOfStreamExtensionData);
                            yield break;
                        }

                        switch (extension)
                        {
                            case 0xF9:
                                if (extensionData.Length != 4)
                                {
                                    yield return new DecoderWarning(string.Format(CultureInfo.CurrentCulture, Resources.IgnoringGraphicsControlExtension, extensionData.Length), new Dictionary<string, object>
                                    {
                                        ["ExtensionData"] = extensionData,
                                    });
                                    break;
                                }

                                yield return new GraphicsControlExtension(
                                    disposalMethod: (extensionData[0] & 0b00011100) >> 2,
                                    userInputFlag: (extensionData[0] & 0b00000010) != 0,
                                    transparencyFlag: (extensionData[0] & 0b00000001) != 0,
                                    delayTime: BitConverter.ToUInt16(extensionData, 1),
                                    transparencyIndex: extensionData[3]);
                                break;

                            case 0xFE:
                                yield return new Comment(Encoding.ASCII.GetString(extensionData));
                                break;

                            case 0xFF:
                                if (extensionData.Length < 11)
                                {
                                    yield return new DecoderWarning(string.Format(CultureInfo.CurrentCulture, Resources.IgnoringApplicationExtension, extensionData.Length), new Dictionary<string, object>
                                    {
                                        ["ExtensionData"] = extensionData,
                                    });
                                    break;
                                }

                                var applicationIdentifier = new byte[8];
                                Array.Copy(extensionData, applicationIdentifier, applicationIdentifier.Length);
                                var applicationAuthenticationCode = new byte[3];
                                Array.Copy(extensionData, applicationIdentifier.Length, applicationAuthenticationCode, 0, applicationIdentifier.Length);
                                yield return new ApplicationExtension(
                                    applicationIdentifier: Encoding.ASCII.GetString(applicationIdentifier),
                                    applicationAuthenticationCode: Encoding.ASCII.GetString(applicationAuthenticationCode),
                                    data: extensionData.Length > 11 ? extensionData.Skip(11).ToArray() : null);
                                break;

                            default:
                                yield return new DecoderWarning(string.Format(CultureInfo.CurrentCulture, Resources.ExtensionNotSupported, extension), new Dictionary<string, object>
                                {
                                    ["Extension"] = extension,
                                });
                                break;
                        }

                        break;

                    default:
                        yield return new DecoderError(string.Format(CultureInfo.CurrentCulture, Resources.BlockTypeNotSupported, separator), new Dictionary<string, object>
                        {
                            ["Separator"] = separator,
                        });
                        yield break;

                    case -1:
                        yield return new DecoderError(Resources.EndOfStreamSeparator);
                        yield break;

                    case 0x3B:
                        yield return new Trailer();
                        yield break;
                }
            }
        }
    }
}
