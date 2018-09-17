// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;

    public class ImageDescriptor : GifElement
    {
        public ImageDescriptor(int left, int top, int width, int height, bool localColorTableFlag, bool interlaceFlag, bool sortFlag, int sizeOfLocalColorTable)
        {
            if (left < ushort.MinValue || left > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(left));
            }

            if (top < ushort.MinValue || top > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(top));
            }

            if (width <= ushort.MinValue || width > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= ushort.MinValue || height > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (!localColorTableFlag)
            {
                if (sizeOfLocalColorTable != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(sizeOfLocalColorTable));
                }
            }
            else
            {
                if (ColorTable.TableSizes.IndexOf(sizeOfLocalColorTable) == -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(sizeOfLocalColorTable));
                }
            }

            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
            this.LocalColorTableFlag = localColorTableFlag;
            this.InterlaceFlag = interlaceFlag;
            this.SortFlag = sortFlag;
            this.SizeOfLocalColorTable = sizeOfLocalColorTable;
        }

        public int Height { get; }

        public bool InterlaceFlag { get; }

        public int Left { get; }

        public bool LocalColorTableFlag { get; }

        public int SizeOfLocalColorTable { get; }

        public bool SortFlag { get; }

        public int Top { get; }

        public int Width { get; }
    }
}
