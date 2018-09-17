// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;

    /// <summary>
    /// A comment element.
    /// </summary>
    public class Comment : GifElement
    {
        public Comment(string value)
        {
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value { get; }
    }
}
