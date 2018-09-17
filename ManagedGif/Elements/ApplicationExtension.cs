// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    public class ApplicationExtension : GifElement
    {
        public ApplicationExtension(string applicationIdentifier, string applicationAuthenticationCode, byte[] data)
        {
        }

        public string ApplicationAuthenticationCode { get; }

        public string ApplicationIdentifier { get; }

        public byte[] Data { get; }
    }
}
