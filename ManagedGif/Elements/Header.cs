// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;

    internal class Header : GifElement
    {
        public Header(string version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (version.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            this.Version = version;
        }

        public string Version { get; }
    }
}
