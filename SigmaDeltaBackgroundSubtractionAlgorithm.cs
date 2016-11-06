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
                        Marshal.Copy(new byte[] { imageBytes[i][j], imageBytes[i][j], imageBytes[i][j], imageBytes[i][j] }, 0, ptr, 4);
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
                return GetBitmap(_detectionLabel);        
            }
        }

        private const int N = 8;
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
                    var imagePixel = (byte)(0.3 * imagePixels[i][j*4 + 1] + 0.59 * imagePixels[i][j * 4 + 2] + 0.11 * imagePixels[i][j * 4 + 3]);
                    _median[i][j] = (byte)(_median[i][j] + Math.Sign(_median[i][j] - imagePixel));//consult atricle
                    _delta[i][j] = (byte)(Math.Abs(imagePixel - _median[i][j]));
                    _variance[i][j] = _variance[i][j] + Math.Sign(N*_delta[i][j] - _variance[i][j]);
                    _detectionLabel[i][j] = (byte)(Convert.ToByte(_delta[i][j] >= _variance[i][j])*255);
                }
            }
        }

        private void InitializeAlgorithm(byte[][] imagePixels)
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    _median[i][j] = (byte)(0.3*imagePixels[i][j*4+1] + 0.59*imagePixels[i][j*4+2]+ 0.11*imagePixels[i][j*4+3]);
                }
            }

        }
    }
}
