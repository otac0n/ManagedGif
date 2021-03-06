// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace ManagedGif.Tests
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using ManagedGif.Elements;
    using ManagedGif.Parser;
    using NUnit.Framework;

    [TestFixture]
    public class GifParserTests
    {
        private static IComparer<Color> ColorComparer = Comparer<Color>.Create((a, b) => a.ToArgb().CompareTo(b.ToArgb()));

        [Test]
        public void GifParser_WithWikipediaExampleGifFile_YieldsExpectedElements()
        {
            ////byte#  hexadecimal  text or
            ////(hex)               value       Meaning
            ////0:     47 49 46
            ////       38 39 61     GIF89a      Header
            ////                                Logical Screen Descriptor
            ////6:     03 00        3            - logical screen width in pixels
            ////8:     05 00        5            - logical screen height in pixels
            ////A:     F7                        - GCT follows for 256 colors with resolution 3 x 8 bits/primary; the lowest 3 bits represent the bit depth minus 1, the highest true bit means that the GCT is present
            ////B:     00           0            - background color #0
            ////C:     00                        - default pixel aspect ratio
            ////                   R    G    B  Global Color Table
            ////D:     00 00 00    0    0    0   - color #0 black
            ////10:    80 00 00  128    0    0   - color #1
            //// :                                       :
            ////85:    00 00 00    0    0    0   - color #40 black
            //// :                                       :
            ////30A:   FF FF FF  255  255  255   - color #255 white
            ////30D:   21 F9                    Graphic Control Extension (comment fields precede this in most files)
            ////30F:   04           4            - 4 bytes of GCE data follow
            ////310:   01                        - there is a transparent background color (bit field; the lowest bit signifies transparency)
            ////311:   00 00                     - delay for animation in hundredths of a second: not used
            ////313:   10          16            - color #16 is transparent
            ////314:   00                        - end of GCE block
            ////315:   2C                       Image Descriptor
            ////316:   00 00 00 00 (0,0)         - NW corner position of image in logical screen
            ////31A:   03 00 05 00 (3,5)         - image width and height in pixels
            ////31E:   00                        - no local color table
            ////31F:   08           8           Start of image - LZW minimum code size
            ////320:   0B          11            - 11 bytes of LZW encoded image data follow
            ////321:   00 51 FC 1B 28 70 A0 C1 83 01 01
            ////32C:   00                        - end of image data
            ////32D:   3B                       GIF file terminator

            var subject = new byte[]
            {
                0x47, 0x49, 0x46,
                0x38, 0x39, 0x61,
                0x03, 0x00,
                0x05, 0x00,
                0xF7,
                0x00,
                0x00,
                0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF,
                0x21, 0xF9,
                0x04,
                0x01,
                0x00, 0x00,
                0x10,
                0x00,
                0x2C,
                0x00, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x05, 0x00,
                0x00,
                0x08,
                0x0B,
                0x00, 0x51, 0xFC, 0x1B, 0x28, 0x70, 0xA0, 0xC1, 0x83, 0x01, 0x01,
                0x00,
                0x3B,
            };

            List<GifElement> result;
            using (var stream = new MemoryStream(subject))
            {
                result = GifParser.ReadGif(stream).ToList();
            }

            Assert.That(result.Count, Is.EqualTo(7));

            var header = (Header)result[0];
            Assert.That(header.Version, Is.EqualTo("89a"));

            var logicalScreenDescriptor = (LogicalScreenDescriptor)result[1];
            Assert.That(logicalScreenDescriptor.Width, Is.EqualTo(3));
            Assert.That(logicalScreenDescriptor.Height, Is.EqualTo(5));
            Assert.That(logicalScreenDescriptor.GlobalColorTableFlag, Is.True);
            Assert.That(logicalScreenDescriptor.SizeOfGlobalColorTable, Is.EqualTo(256));
            Assert.That(logicalScreenDescriptor.ColorResolution, Is.EqualTo(8));
            Assert.That(logicalScreenDescriptor.BackgroundColorIndex, Is.EqualTo(0));
            Assert.That(logicalScreenDescriptor.PixelAspectRatio, Is.Null);

            var globalColorTable = (GlobalColorTable)result[2];
            Assert.That(globalColorTable.Value.Length, Is.EqualTo(256));
            Assert.That(globalColorTable.Value, Is.EqualTo(
                new[] { Color.Black, Color.FromArgb(128, 0, 0) }.Concat(Enumerable.Repeat(Color.Black, 253)).Concat(new[] { Color.White }).ToArray()).Using(ColorComparer));

            var graphicsControlExtension = (GraphicsControlExtension)result[3];
            Assert.That(graphicsControlExtension.TransparencyFlag, Is.True);
            Assert.That(graphicsControlExtension.UserInputFlag, Is.False);
            Assert.That(graphicsControlExtension.DisposalMethod, Is.EqualTo(0));
            Assert.That(graphicsControlExtension.DelayTime, Is.EqualTo(0));
            Assert.That(graphicsControlExtension.TransparencyIndex, Is.EqualTo(16));

            var imageDescriptor = (ImageDescriptor)result[4];
            Assert.That(imageDescriptor.Left, Is.EqualTo(0));
            Assert.That(imageDescriptor.Top, Is.EqualTo(0));
            Assert.That(imageDescriptor.Width, Is.EqualTo(3));
            Assert.That(imageDescriptor.Height, Is.EqualTo(5));
            Assert.That(imageDescriptor.LocalColorTableFlag, Is.False);
            Assert.That(imageDescriptor.SortFlag, Is.False);
            Assert.That(imageDescriptor.InterlaceFlag, Is.False);
            Assert.That(imageDescriptor.SizeOfLocalColorTable, Is.EqualTo(0));

            var raster = (Raster)result[5];
            Assert.That(raster.Value.Width, Is.EqualTo(3));
            Assert.That(raster.Value.Height, Is.EqualTo(5));
            Assert.That(raster.Value.GetPixel(0, 0), Is.EqualTo(Color.Black).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(1, 0), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(2, 0), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(0, 1), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(1, 1), Is.EqualTo(Color.Black).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(2, 1), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(0, 2), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(1, 2), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(2, 2), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(0, 3), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(1, 3), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(2, 3), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(0, 4), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(1, 4), Is.EqualTo(Color.White).Using(ColorComparer));
            Assert.That(raster.Value.GetPixel(2, 4), Is.EqualTo(Color.White).Using(ColorComparer));

            var trailer = (Trailer)result[6];
        }
    }
}
