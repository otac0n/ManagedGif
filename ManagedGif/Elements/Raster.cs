// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;
    using System.Drawing;

    public class Raster : GifElement
    {
        public Raster(Bitmap value)
        {
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Bitmap Value { get; }
    }
}
