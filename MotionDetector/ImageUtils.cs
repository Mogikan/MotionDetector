using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ManagedCuda;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MotionDetector
{
    public class ImageUtils
    {

        public static BitmapSource ConvertBitmapToSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }
        private static object locker = new object();
        public static byte[] GetImageBytes(Bitmap bitmap, out int stride)
        {
            int imageWidth = bitmap.Width;
            int imageHeight = bitmap.Height;
            BitmapData bmpdata = null;
            lock (locker)
            {
                try
                {
                    bmpdata = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    stride = bmpdata.Stride;
                    var totalImageBytes = bmpdata.Stride * bitmap.Height;
                    byte[] buffer = new byte[totalImageBytes];
                    var ptr = bmpdata.Scan0;
                    Marshal.Copy(ptr, buffer, 0, totalImageBytes);
                    return buffer;
                }
                finally
                {
                    if (bmpdata != null)
                        bitmap.UnlockBits(bmpdata);
                }
            }
        }

        public static Bitmap GetImageFromBytes(byte[] bitmapBytes,int width, int height)
        {
            BitmapData bmpdata = null;
            Bitmap image = null;
            try
            {
                image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                bmpdata = image.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb
                );
                var ptr = bmpdata.Scan0;
                Marshal.Copy(bitmapBytes, 0, ptr, bitmapBytes.Length);
                return image;
            }
            finally
            {
                if (bmpdata != null)
                    image.UnlockBits(bmpdata);
            }
        }


        public static byte[] Erode(byte[] _detectionLabel,int width, int height)
        {
            ManagedCuda.NPP.NPPImage_8uC1 cudaImage = new ManagedCuda.NPP.NPPImage_8uC1(width, height);
            ManagedCuda.NPP.NPPImage_8uC1 erodedImage = new ManagedCuda.NPP.NPPImage_8uC1(width, height);

            cudaImage.CopyToDevice(_detectionLabel);
            var erodeMask = new byte[]
            {
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1,
            };
            CudaDeviceVariable<byte> mask = erodeMask;
            cudaImage.Erode(erodedImage, mask, new ManagedCuda.NPP.NppiSize(7, 7), new ManagedCuda.NPP.NppiPoint(4, 4));
            //cudaImage.Erode3x3(erodedImage);
            mask.Dispose();
            cudaImage.Dispose();
            byte[] result = new byte[_detectionLabel.Length];
            erodedImage.CopyToHost(result);
            erodedImage.Dispose();
            return result;
        }

        public static byte[] DrawMotionContours(byte[] binaryImageBytes, byte[] drawingImageBytes,int width, int height)
        {
            var binaryImage = new Image<Gray, byte>(width, height);
            binaryImage.Bytes = binaryImageBytes;
            var outputImage = new Image<Rgb, byte>(width, height);
            outputImage.Bytes = drawingImageBytes;
            using (var hierarchy = new Mat())
            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(binaryImage, contours, hierarchy, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                for (int i = 0; i < contours.Size; i++)
                {
                    var contourPoints = contours[i];
                    var rectangle = CvInvoke.BoundingRectangle(contourPoints);
                    if (rectangle.Size.Width > 30 || rectangle.Size.Height > 30)
                    {
                        CvInvoke.Rectangle(outputImage, rectangle, new MCvScalar(0, 0, 255));
                    }
                }
            }
            return outputImage.Bytes;
        }
        //private Bitmap GetBitmapFromGrayBytes(byte[] imageBytes)
        //{
        //    BitmapData bmpdata = null;
        //    Bitmap image = null;
        //    try
        //    {
        //        image = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
        //        bmpdata = image.LockBits(
        //            new Rectangle(0, 0, _width, _height),
        //            ImageLockMode.ReadWrite,
        //            PixelFormat.Format32bppArgb
        //        );
        //        var ptr = bmpdata.Scan0;
        //        for (int i = 0; i < _height; i++)
        //        {
        //            for (int j = 0; j < _width; j++)
        //            {
        //                Marshal.Copy(new byte[] { imageBytes[i * _width + j], imageBytes[i * _width + j], imageBytes[i * _width + j], 255 }, 0, ptr, 4);
        //                ptr = ptr + 4;
        //            }
        //        }
        //        //image.Save(@"c:\temp\motion.jpeg", ImageFormat.Jpeg);
        //        return image;
        //    }
        //    finally
        //    {
        //        if (bmpdata != null)
        //            image.UnlockBits(bmpdata);
        //    }
        //}
    }
}
    //{
    //    private short[,] se = new short[5, 5]
    //{
    //        { 1, 1, 1 , 1, 1},
    //        { 1, 1, 1 , 1, 1},
    //        { 1, 1, 1 , 1, 1},
    //        { 1, 1, 1 , 1, 1},
    //        { 1, 1, 1 , 1, 1},
    //};
    //    public Bitmap Erode(byte[] input)
    //    {
    //        var destination = new byte[_height][];
    //        for (int i = 0; i < _height; i++)
    //        {
    //            destination[i] = new byte[_width];
    //        }
    //        int size = 3;
    //        // processing start and stop X,Y positions
    //        int startX = 0;
    //        int startY = 0;
    //        int stopX = _width;
    //        int stopY = _height;

    //        // structuring element's radius
    //        int r = size >> 1;

    //        // flag to indicate if at least one pixel for the given structuring element was found
    //        bool foundSomething;

    //        // grayscale image

    //        // compute each line
    //        for (int y = startY; y < _height; y++)
    //        {
    //            byte[] sourceBytesLine = input[y];
    //            int sourceLine = 0;
    //            int destinationLineIndex = 0;
    //            byte[] destinationBytesLine = destination[y];

    //            byte min, v;

    //            // loop and array indexes
    //            int verticalIndex, horizontalIndex, ir, jr, i, j;

    //            // for each pixel
    //            for (int x = startX; x < stopX; x++, sourceLine++, destinationLineIndex++)
    //            {
    //                min = 255;
    //                foundSomething = false;

    //                // for each structuring element's row
    //                for (i = 0; i < size; i++)
    //                {
    //                    ir = i - r;
    //                    verticalIndex = y + ir;

    //                    // skip row
    //                    if (verticalIndex < startY)
    //                        continue;
    //                    // break
    //                    if (verticalIndex >= stopY)
    //                        break;

    //                    // for each structuring element's column
    //                    for (j = 0; j < size; j++)
    //                    {
    //                        jr = j - r;
    //                        horizontalIndex = x + jr;

    //                        // skip column
    //                        if (horizontalIndex < startX)
    //                            continue;
    //                        if (horizontalIndex >= stopX)
    //                            continue;
    //                        if (se[i, j] == 1)
    //                        {
    //                            foundSomething = true;
    //                            // get new MIN value
    //                            v = input[verticalIndex][horizontalIndex];
    //                            if (v < min)
    //                                min = v;
    //                        }

    //                    }
    //                }
    //                // result pixel
    //                destination[y][x] = (foundSomething) ? min : input[y][x];
    //            }
    //        }
    //        return destination;

    //    }
    //}
//}

