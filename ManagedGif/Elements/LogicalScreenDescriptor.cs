// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

using System;

namespace ManagedGif.Elements
{
    public class LogicalScreenDescriptor : GifElement
    {
        public LogicalScreenDescriptor(int width, int height, bool globalColorTableFlag, int colorResolution, bool sortFlag, int sizeOfGlobalColorTable, int backgroundColorIndex, double? pixelAspectRatio)
        {
            if (width <= ushort.MinValue || width > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= ushort.MinValue || height > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (colorResolution <= 0 || colorResolution > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(colorResolution));
            }

            if (!globalColorTableFlag)
            {
                if (sizeOfGlobalColorTable != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(sizeOfGlobalColorTable));
                }
            }
            else
            {
                if (ColorTable.TableSizes.IndexOf(sizeOfGlobalColorTable) == -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(sizeOfGlobalColorTable));
                }
            }

            if (backgroundColorIndex < 0 || backgroundColorIndex > byte.MaxValue || (!globalColorTableFlag && backgroundColorIndex != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(backgroundColorIndex));
            }

            if (pixelAspectRatio <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pixelAspectRatio));
            }

            this.Width = width;
            this.Height = height;
            this.GlobalColorTableFlag = globalColorTableFlag;
            this.ColorResolution = colorResolution;
            this.SortFlag = sortFlag;
            this.SizeOfGlobalColorTable = sizeOfGlobalColorTable;
            this.BackgroundColorIndex = backgroundColorIndex;
            this.PixelAspectRatio = pixelAspectRatio;
        }

        public int BackgroundColorIndex { get; }

        public int ColorResolution { get; }

        public bool GlobalColorTableFlag { get; }

        public int Height { get; }

        public double? PixelAspectRatio { get; }

        public int SizeOfGlobalColorTable { get; }

        public bool SortFlag { get; }

        public int Width { get; }
    }
}
