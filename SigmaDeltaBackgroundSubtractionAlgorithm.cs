using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MotionDetector
{
    public class SigmaDeltaBackgroundSubtractionAlgorithm
    {
        public SigmaDeltaBackgroundSubtractionAlgorithm(int width, int height,int bytesPerPixel)
        {
            _bitsPerPixel = bytesPerPixel;
            _width = width;
            _height = height;
            _median = new byte[height][];
            _variance = new int[height][];
            _delta = new byte[height][];
            _detectionLabel = new byte[height][];
            for (int i = 0; i < height; i++)
            {
                _median[i] = new byte[width];
                _variance[i] = new int[width];
                _delta[i] = new byte[width];
                _detectionLabel[i] = new byte[width];
            }
        }
        private int _height;
        private int _width;
        private byte[][] _median;
        private int[][] _variance;
        private byte[][] _delta;
        private byte[][] _detectionLabel;
        private int _bitsPerPixel;
        public byte[][] MovementMap
        {
            get
            {
                return _detectionLabel;
            }
        }

        private Bitmap GetBitmap(byte[][] imageBytes)
        {
            BitmapData bmpdata = null;
            Bitmap image = null;
            try
            {
                image = new Bitmap(_width, _height,PixelFormat.Format32bppArgb);
                bmpdata = image.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb
                );                
                var ptr = bmpdata.Scan0;
                for (int i = 0; i < _height; i++)
                {
                    for (int j = 0; j < _width; j++)
                    {
                        Marshal.Copy(new byte[] { imageBytes[i][j], imageBytes[i][j], imageBytes[i][j] , 255 }, 0, ptr, 4);
                        ptr = ptr + 4;                        
                    }
                }
                //image.Save(@"c:\temp\motion.jpeg", ImageFormat.Jpeg);
                return image;
            }
            finally
            {
                if (bmpdata != null)
                    image.UnlockBits(bmpdata);
            }
        }

        
        public Bitmap MotionPicture
        {
            get
            {
                return GetBitmap(Erode(_detectionLabel));        
            }
        }

        private short[,] se = new short[5, 5]
        {
            { 1, 1, 1 , 1, 1},
            { 1, 1, 1 , 1, 1},
            { 1, 1, 1 , 1, 1},
            { 1, 1, 1 , 1, 1},
            { 1, 1, 1 , 1, 1},
        };
        private byte[][] Erode(byte[][] input)
        {
            var destination = new byte[_height][];
            for (int i = 0; i < _height; i++)
            {
                destination[i] = new byte[_width];
            }
            int size = 3;
            // processing start and stop X,Y positions
            int startX = 0;
            int startY = 0;
            int stopX = _width;
            int stopY = _height;

            // structuring element's radius
            int r = size >> 1;

            // flag to indicate if at least one pixel for the given structuring element was found
            bool foundSomething;

            // grayscale image

            // compute each line
            for (int y = startY; y < _height; y++)
            {
                byte[] sourceBytesLine = input[y];
                int sourceLine = 0;
                int destinationLineIndex = 0;
                byte[] destinationBytesLine = destination[y];

                byte min, v;

                // loop and array indexes
                int verticalIndex, horizontalIndex, ir, jr, i, j;

                // for each pixel
                for (int x = startX; x < stopX; x++, sourceLine++, destinationLineIndex++)
                {
                    min = 255;
                    foundSomething = false;

                    // for each structuring element's row
                    for (i = 0; i < size; i++)
                    {
                        ir = i - r;
                        verticalIndex = y + ir;

                        // skip row
                        if (verticalIndex < startY)
                            continue;
                        // break
                        if (verticalIndex >= stopY)
                            break;

                        // for each structuring element's column
                        for (j = 0; j < size; j++)
                        {
                            jr = j - r;
                            horizontalIndex = x + jr;

                            // skip column
                            if (horizontalIndex < startX)
                                continue;
                            if (horizontalIndex >= stopX)
                                continue;
                            if (se[i, j] == 1)
                            {
                                foundSomething = true;
                                // get new MIN value
                                v = input[verticalIndex][horizontalIndex];
                                if (v < min)
                                    min = v;
                            }

                        }
                    }
                    // result pixel
                    destination[y][x] = (foundSomething) ? min : input[y][x];
                }
            }
            return destination;

        }

        private const int N = 4;
        bool isFirstStep = true;
        public void ExecuteStep(byte[][] imagePixels)
        {
            if (isFirstStep)
            {
                InitializeAlgorithm(imagePixels);
                isFirstStep = false;
                return;
            }
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    var imagePixel = (byte)(0.3 * imagePixels[i][j* _bitsPerPixel] + 0.59 * imagePixels[i][j * _bitsPerPixel + 1] + 0.11 * imagePixels[i][j * _bitsPerPixel + 2]);
                    _median[i][j] = (byte)(_median[i][j] + Math.Sign(imagePixel - _median[i][j]));//consult atricle
                    _delta[i][j] = (byte)(Math.Abs(imagePixel - _median[i][j]));
                    if (_delta[i][j] > 0.001)
                    {
                        _variance[i][j] = _variance[i][j] + Math.Sign(N * _delta[i][j] - _variance[i][j]);
                    }
                    if (_delta[i][j] > 0.001)
                    {
                        _detectionLabel[i][j] = (byte)(Convert.ToByte(_delta[i][j] >= _variance[i][j]) * 255);
                    }
                    else
                    {
                        _detectionLabel[i][j] = 0;
                    }
                }
            }
        }

        private void InitializeAlgorithm(byte[][] imagePixels)
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    _median[i][j] = (byte)(0.3*imagePixels[i][j* _bitsPerPixel] + 0.59*imagePixels[i][j* _bitsPerPixel + 1]+ 0.11*imagePixels[i][j*_bitsPerPixel+2]);
                }
            }

        }
    }
}
