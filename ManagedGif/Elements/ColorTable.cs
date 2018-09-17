// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// A color table element.
    /// </summary>
    public class ColorTable : GifElement
    {
        public static readonly IList<int> TableSizes = Enumerable.Range(0, 8).Select(i => 0x01 << (i + 1)).ToList().AsReadOnly();

        public ColorTable(Color[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (TableSizes.IndexOf(value.Length) == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.Value = value;
        }

        public Color[] Value { get; }
    }
}
