// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Elements
{
    using System;

    public class GraphicsControlExtension : GifElement
    {
        public GraphicsControlExtension(int disposalMethod, bool userInputFlag, int delayTime, bool transparencyFlag, int transparencyIndex)
        {
            if (disposalMethod < 0 || disposalMethod >= 8)
            {
                throw new ArgumentOutOfRangeException(nameof(disposalMethod));
            }

            if (delayTime < ushort.MinValue || delayTime >= ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(delayTime));
            }

            if (transparencyIndex < byte.MinValue || transparencyIndex >= byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(transparencyIndex));
            }

            this.DisposalMethod = disposalMethod;
            this.UserInputFlag = userInputFlag;
            this.DelayTime = delayTime;
            this.TransparencyFlag = transparencyFlag;
            this.TransparencyIndex = transparencyIndex;
        }

        public int DelayTime { get; }

        public int DisposalMethod { get; }

        public bool TransparencyFlag { get; }

        public int TransparencyIndex { get; }

        public bool UserInputFlag { get; }
    }
}
