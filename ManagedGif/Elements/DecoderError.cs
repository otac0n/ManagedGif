// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;
    using System.Collections.Generic;

    public class DecoderError : GifElement
    {
        public DecoderError(string value, Dictionary<string, object> context = null)
        {
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
            this.Context = context;
        }

        public object Context { get; }

        public string Value { get; }
    }
}
