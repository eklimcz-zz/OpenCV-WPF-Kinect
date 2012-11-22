using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Emgu.CV;

namespace KinectWPFOpenCV
{
    public static class ImageHelpers
    {
       
        private const int MaxDepthDistance = 4000;
        private const int MinDepthDistance = 850;
        private const int MaxDepthDistanceOffset = 3150;

        public static BitmapSource SliceDepthImage(this DepthImageFrame image, int min = 20, int max = 1000)
        {
            int width = image.Width;
            int height = image.Height;

            //var depthFrame = image.Image.Bits;
            short[] rawDepthData = new short[image.PixelDataLength];
            image.CopyPixelDataTo(rawDepthData);
           
            var pixels = new byte[height * width * 4];
          
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;

            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {

                // Calculate the distance represented by the two depth bytes
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // Map the distance to an intesity that can be represented in RGB
                var intensity = CalculateIntensityFromDistance(depth);

                if (depth > min && depth < max)
                {
                    // Apply the intensity to the color channels
                    pixels[colorIndex + BlueIndex] = intensity; //blue
                    pixels[colorIndex + GreenIndex] = intensity; //green
                    pixels[colorIndex + RedIndex] = intensity; //red                    
                }
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
        }

        public static byte CalculateIntensityFromDistance(int distance)
        {
            // This will map a distance value to a 0 - 255 range
            // for the purposes of applying the resulting value
            // to RGB pixels.
            int newMax = distance - MinDepthDistance;
            if (newMax > 0)
                return (byte)(255 - (255 * newMax
                / (MaxDepthDistanceOffset)));
            else
                return (byte)255;
        }


        public static System.Drawing.Bitmap ToBitmap(this BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                // from System.Media.BitmapImage to System.Drawing.Bitmap
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
                return bitmap;
            }
        }


        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

    }
}
