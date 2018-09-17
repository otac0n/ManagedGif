// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using ManagedGif.Elements;

    public class GifEncoder : IDisposable
    {
        private readonly int height;
        private readonly Stream output;
        private readonly int width;
        private bool disposed;
        private int[] previousContents;

        public GifEncoder(int width, int height, Stream output)
        {
            this.width = width;
            this.height = height;
            this.output = output;
            this.previousContents = new int[width * height];
            for (var i = 0; i < this.previousContents.Length; i++)
            {
                this.previousContents[i] = Color.Transparent.ToArgb();
            }

            this.WriteHeader(new Header("89a"));
            this.WriteLogicalScreenDescriptor(new LogicalScreenDescriptor(
                width: this.width,
                height: this.height,
                globalColorTableFlag: false,
                colorResolution: 8,
                sortFlag: false,
                sizeOfGlobalColorTable: 0,
                backgroundColorIndex: 0,
                pixelAspectRatio: null));
        }

        public void AddFrame(Image frame, TimeSpan? delay = null)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var delayT = Math.Min((int)Math.Round((delay ?? TimeSpan.FromMilliseconds(100)).TotalMilliseconds / 10), ushort.MaxValue);

            var length = this.width * this.height;

            var bmpCopy = new Bitmap(this.width, this.height);
            using (var g = Graphics.FromImage(bmpCopy))
            {
                g.Clear(Color.Transparent);
                g.DrawImageUnscaled(frame, Point.Empty);
            }

            var bmpData = bmpCopy.LockBits(new Rectangle(0, 0, this.width, this.height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var colorData = new int[length];
            var scan = bmpData.Scan0;
            for (var y = 0; y < height; y++, scan += bmpData.Stride)
            {
                Marshal.Copy(scan, colorData, y * width, width);
            }

            var minX = this.width;
            var minY = this.height;
            var maxX = -1;
            var maxY = -1;
            for (var i = 0; i < length; i++)
            {
                var y = i / this.width;
                var x = i % this.width;
                var c = colorData[i];

                var transparent = false;
                if ((c & 0xFF000000) != 0xFF000000)
                {
                    if ((c & 0xFF000000) >= 0x80000000)
                    {
                        colorData[i] = c = (int)(c | 0xFF000000);
                    }
                    else
                    {
                        colorData[i] = c = Color.Transparent.ToArgb();
                        transparent = true;
                    }
                }

                if (c == this.previousContents[i])
                {
                    colorData[i] = c = Color.Transparent.ToArgb();
                    transparent = true;
                }
                else if (c != Color.Transparent.ToArgb())
                {
                    this.previousContents[i] = c;
                }

                if (!transparent)
                {
                    minX = Math.Min(x, minX);
                    maxX = Math.Max(x, maxX);
                    minY = Math.Min(y, minY);
                    maxY = Math.Max(y, maxY);
                }
            }

            if (maxX == -1)
            {
                var colorTable = new ColorTable(new Color[] { Color.Magenta, Color.Black });
                this.WriteGraphicsControlExtension(new GraphicsControlExtension(
                    disposalMethod: 0x01,
                    userInputFlag: false,
                    delayTime: delayT,
                    transparencyFlag: true,
                    transparencyIndex: 0));
                this.WriteImageDescriptor(new ImageDescriptor(
                    left: 0,
                    top: 0,
                    width: 1,
                    height: 1,
                    localColorTableFlag: true,
                    interlaceFlag: false,
                    sortFlag: false,
                    sizeOfLocalColorTable: colorTable.Value.Length));
                this.WriteColorTable(colorTable);
                this.WriteImageData(colorTable.Value.Length, new[] { 0 });
            }
            else
            {
                var w = (maxX + 1) - minX;
                var h = (maxY + 1) - minY;
                var frameColors = new int[w * h];
                for (var y = 0; y < h; y++)
                {
                    Array.Copy(colorData, (y + minY) * this.width + minX, frameColors, y * w, w);
                }

                var palette = new HashSet<int>(frameColors);

                while (palette.Count > 0)
                {
                    var currentPalette = new List<int>();
                    currentPalette.Add(Color.Transparent.ToArgb());
                    currentPalette.AddRange(palette.Take(255));
                    var portion = new int[frameColors.Length];
                    palette.ExceptWith(currentPalette);
                    var colorTable = MakeColorTable(currentPalette.Select((c, i) => i == 0 ? Color.Magenta : Color.FromArgb(c)).ToList());

                    for (var i = 0; i < frameColors.Length; i++)
                    {
                        portion[i] = Math.Max(0, currentPalette.IndexOf(frameColors[i]));
                    }

                    this.WriteGraphicsControlExtension(new GraphicsControlExtension(
                        disposalMethod: 0x01,
                        userInputFlag: false,
                        delayTime: palette.Count > 0 ? 0 : delayT,
                        transparencyFlag: true,
                        transparencyIndex: 0));
                    this.WriteImageDescriptor(new ImageDescriptor(
                        left: minX,
                        top: minY,
                        width: w,
                        height: h,
                        localColorTableFlag: true,
                        interlaceFlag: false,
                        sortFlag: false,
                        sizeOfLocalColorTable: colorTable.Value.Length));
                    this.WriteColorTable(colorTable);
                    this.WriteImageData(colorTable.Value.Length, portion);
                }
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.WriteTrailer();
                this.output.Flush();
            }
        }

        public void StartRepetition(int repetitions = 0)
        {
            this.WriteNetscapeApplicationExtension(repetitions);
        }

        public void WriteTrailer()
        {
            this.output.WriteByte(0x3B);
        }

        private static ColorTable MakeColorTable(IList<Color> colors)
        {
            var size = ColorTable.TableSizes.FirstOrDefault(s => s >= colors.Count);
            if (size == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colors));
            }

            var table = new Color[size];
            for (var i = 0; i < colors.Count; i++)
            {
                table[i] = colors[i];
            }

            return new ColorTable(table);
        }

        private void WriteColorTable(ColorTable colorTable)
        {
            var colors = colorTable.Value;
            var data = new byte[colors.Length * 3];
            for (var i = 0; i < data.Length;)
            {
                var c = colors[i];
                data[i++] = c.R;
                data[i++] = c.G;
                data[i++] = c.B;
            }

            this.output.Write(data, 0, data.Length);
        }

        private void WriteExtension(byte label, params byte[][] data)
        {
            if (data.Length > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            var extData = new byte[2];
            extData[0] = 0x21;
            extData[1] = label;
            this.output.Write(extData, 0, extData.Length);

            for (var i = 0; i < data.Length; i++)
            {
                this.output.WriteByte((byte)data[i].Length);
                this.output.Write(data[i], 0, data[i].Length);
            }

            this.output.WriteByte(0);
        }

        private void WriteGraphicsControlExtension(GraphicsControlExtension extension)
        {
            var data = new byte[4];
            data[0] = (byte)(extension.DisposalMethod << 2 |
                             (extension.UserInputFlag ? 1 : 0) << 1 |
                             (extension.TransparencyFlag ? 1 : 0));
            data[1] = (byte)(extension.DelayTime & 0xFF);
            data[2] = (byte)((extension.DelayTime & 0xFF00) >> 8);
            data[3] = (byte)extension.TransparencyIndex;

            this.WriteExtension(0xF9, data);
        }

        private void WriteHeader(Header header)
        {
            var data = new byte[6];
            data[0] = (byte)'G';
            data[1] = (byte)'I';
            data[2] = (byte)'F';
            Encoding.ASCII.GetBytes(header.Version, 0, 3, data, 3);
            this.output.Write(data, 0, data.Length);
        }

        private void WriteImageData(int tableSize, int[] data)
        {
            var minimumCodeSize = Math.Max(2, ColorTable.TableSizes.IndexOf(tableSize) + 1);

            const int MaximumCodeValue = 0xFFF;
            const int ClearCode = -1;
            const int EndOfInformationCode = -2;

            var codeLookup = Enumerable
                .Range(0, 1 << minimumCodeSize)
                .Concat(new[] { ClearCode, EndOfInformationCode })
                .Select((c, i) => new { Code = (IList<int>)new[] { c }, Index = i })
                .ToDictionary(x => x.Code, x => x.Index, new CodeEqualityComparer());

            var outputData = new byte[byte.MaxValue + 1];
            var writeIndex = 1;
            var remainingBits = 8;

            void Flush()
            {
                var length = writeIndex + (remainingBits != 8 ? 1 : 0);
                var payloadSize = length - 1;
                if (payloadSize > 0)
                {
                    outputData[0] = (byte)payloadSize;
                    this.output.Write(outputData, 0, length);
                    Array.Clear(outputData, 0, outputData.Length);
                    writeIndex = 1;
                    remainingBits = 8;
                }
            }

            void Output(int code)
            {
                var largestCode = codeLookup.Count - 1;
                var readBits = (int)Math.Log(largestCode, 2) + 1;
                while (readBits > 0)
                {
                    var bits = readBits < remainingBits ? readBits : remainingBits;
                    var mask = (1 << bits) - 1;
                    outputData[writeIndex] |= (byte)((code & mask) << (8 - remainingBits));
                    code >>= bits;
                    readBits -= bits;
                    remainingBits -= bits;

                    if (remainingBits == 0)
                    {
                        writeIndex++;
                        remainingBits = 8;
                        if (writeIndex >= outputData.Length)
                        {
                            Flush();
                        }
                    }
                }
            }

            this.output.WriteByte((byte)minimumCodeSize);
            Output(codeLookup[new[] { ClearCode }]);

            var readIndex = 0;
            var indexBuffer = new List<int> { data[readIndex++] };
            while (readIndex < data.Length)
            {
                indexBuffer.Add(data[readIndex++]);

                if (!codeLookup.TryGetValue(indexBuffer, out var ignore))
                {
                    var toAdd = indexBuffer.ToArray();
                    Output(codeLookup[indexBuffer.GetRange(0, indexBuffer.Count - 1)]);
                    indexBuffer.RemoveRange(0, indexBuffer.Count - 1);
                    if (codeLookup.Count <= MaximumCodeValue)
                    {
                        codeLookup.Add(toAdd, codeLookup.Count);
                    }
                }
            }

            Output(codeLookup[indexBuffer]);
            Output(codeLookup[new[] { EndOfInformationCode }]);
            Flush();

            this.output.WriteByte(0x00);
        }

        private void WriteImageDescriptor(ImageDescriptor image)
        {
            var colorTableSizeIndex = ColorTable.TableSizes.IndexOf(image.SizeOfLocalColorTable);

            var data = new byte[10];
            data[0] = 0x2C;
            data[1] = (byte)(image.Left & 0xFF);
            data[2] = (byte)((image.Left & 0xFF00) >> 8);
            data[3] = (byte)(image.Top & 0xFF);
            data[4] = (byte)((image.Top & 0xFF00) >> 8);
            data[5] = (byte)(image.Width & 0xFF);
            data[6] = (byte)((image.Width & 0xFF00) >> 8);
            data[7] = (byte)(image.Height & 0xFF);
            data[8] = (byte)((image.Height & 0xFF00) >> 8);
            data[9] = (byte)((image.LocalColorTableFlag ? 1 : 0) << 7 |
                             (image.InterlaceFlag ? 1 : 0) << 6 |
                             (image.SortFlag ? 1 : 0) << 5 |
                             colorTableSizeIndex);

            this.output.Write(data, 0, data.Length);
        }

        private void WriteLogicalScreenDescriptor(LogicalScreenDescriptor screen)
        {
            var colorTableSizeIndex = ColorTable.TableSizes.IndexOf(screen.SizeOfGlobalColorTable);

            var data = new byte[7];
            data[0] = (byte)(screen.Width & 0xFF);
            data[1] = (byte)((screen.Width & 0xFF00) >> 8);
            data[2] = (byte)(screen.Height & 0xFF);
            data[3] = (byte)((screen.Height & 0xFF00) >> 8);
            data[4] = (byte)((screen.GlobalColorTableFlag ? 1 : 0) << 7 |
                             (screen.ColorResolution - 1) << 4 |
                             (screen.SortFlag ? 1 : 0) << 3 |
                             colorTableSizeIndex);
            data[5] = (byte)screen.BackgroundColorIndex;
            data[6] = screen.PixelAspectRatio == null
                ? byte.MinValue
                : (byte)Math.Min(Math.Max((int)Math.Round(screen.PixelAspectRatio.Value * 64 - 15), byte.MinValue + 1), byte.MaxValue);

            this.output.Write(data, 0, data.Length);
        }

        private void WriteNetscapeApplicationExtension(int repetitions)
        {
            if (repetitions < ushort.MinValue || repetitions > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(repetitions));
            }

            var header = new byte[11];
            Encoding.ASCII.GetBytes("NETSCAPE", 0, 8, header, 0);
            Encoding.ASCII.GetBytes("2.0", 0, 3, header, 8);
            var data = new byte[3];
            data[0] = 0x01;
            data[1] = (byte)(repetitions & 0xFF);
            data[2] = (byte)((repetitions & 0xFF00) >> 8);

            this.WriteExtension(0xFF, header, data);
        }

        private class CodeEqualityComparer : IEqualityComparer<IList<int>>
        {
            public bool Equals(IList<int> x, IList<int> y)
            {
                var count = x.Count;
                if (y.Count != count)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(IList<int> obj)
            {
                var c = obj.Count;
                var hash = c--;

                if (c > 0)
                {
                    hash = (hash << 8) | obj[c--];
                }

                if (c > 0)
                {
                    hash = (hash << 8) | obj[c--];
                }

                if (c > 0)
                {
                    hash = (hash << 8) | obj[c];
                }

                return hash;
            }
        }
    }
}
