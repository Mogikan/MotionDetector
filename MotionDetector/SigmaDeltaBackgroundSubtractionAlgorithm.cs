using ManagedCuda;
using ManagedCuda.VectorTypes;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MotionDetector
{
    public class SigmaDeltaBackgroundSubtractionAlgorithm
    {
        public SigmaDeltaBackgroundSubtractionAlgorithm(int width, int height,int bytesPerPixel)
        {
            _bytesPerPixel = bytesPerPixel;
            _width = width;
            _height = height;
            _median = new byte[height*width];
            _variance = new int[height*width];
            _delta = new byte[height*width];
            _detectionLabel = new byte[height*width];            
        }
        private int _height;
        private int _width;
        private byte[] _median;
        private int[] _variance;
        private byte[] _delta;
        private byte[] _detectionLabel;
        private int _bytesPerPixel;       

        private Bitmap GetBitmapFromGrayBytes(byte[] imageBytes)
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
                        Marshal.Copy(new byte[] { imageBytes[i * _width + j], imageBytes[i* _width + j], imageBytes[i* _width + j] , 255 }, 0, ptr, 4);
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
                Stopwatch watch = new Stopwatch();
                watch.Start();
                var result = GetBitmapFromGrayBytes(Erode(_detectionLabel));
                watch.Stop();
                System.Diagnostics.Debug.WriteLine($"erode time {watch.ElapsedMilliseconds}");
                return result; 
            }
        }

        private byte[] Erode(byte[] _detectionLabel)
        {
            ManagedCuda.NPP.NPPImage_8uC1 cudaImage = new ManagedCuda.NPP.NPPImage_8uC1(_width, _height);
            ManagedCuda.NPP.NPPImage_8uC1 erodedImage = new ManagedCuda.NPP.NPPImage_8uC1(_width, _height);

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
            cudaImage.Erode(erodedImage,mask,new ManagedCuda.NPP.NppiSize(7,7),new ManagedCuda.NPP.NppiPoint(4,4));
            mask.Dispose();
            cudaImage.Dispose();
            byte[] result = new byte[_detectionLabel.Length];
            erodedImage.CopyToHost(result);
            erodedImage.Dispose();            
            return result;
        }

        private const int N = 4;
        bool isFirstStep = true;
        public void ExecuteStep(byte[] imagePixels,int stride)
        {
            if (isFirstStep)
            {
                InitializeAlgorithm(imagePixels,stride);
                isFirstStep = false;
                return;
            }
            //extern "C" __global__ void motionKernel(const unsigned char *imagePixels, 
            //unsigned char *median, 
            //unsigned char* delta, 
            //int* variance, 
            //unsigned char* motionBytes, 
            //unsigned int bytesPerPixel)
            ManagedCuda.CudaContext cudaContext = new ManagedCuda.CudaContext();
            var motionKernel = cudaContext.LoadKernel("kernel_64.ptx", "motionKernel");
            var threadsPerBlock = new dim3(256);
            var blocksCount = new dim3((int)(_width * _height / threadsPerBlock.x));
            motionKernel.GridDimensions = blocksCount;
            motionKernel.BlockDimensions = threadsPerBlock;
            ManagedCuda.CudaDeviceVariable<byte> input = imagePixels;
            ManagedCuda.CudaDeviceVariable<byte> median = _median;
            ManagedCuda.CudaDeviceVariable<byte> delta = _delta;
            ManagedCuda.CudaDeviceVariable<int> variance = _variance;
            ManagedCuda.CudaDeviceVariable<byte> motionBytes = _detectionLabel;
            motionKernel.Run(
                input.DevicePointer
                , median.DevicePointer
                , delta.DevicePointer
                , variance.DevicePointer
                , motionBytes.DevicePointer
                , _bytesPerPixel);
            imagePixels = input;
            _median = median;
            _delta = delta;
            _variance = variance;
            _detectionLabel = motionBytes;
            input.Dispose();
            median.Dispose();
            delta.Dispose();
            variance.Dispose();
            motionBytes.Dispose();
            cudaContext.Dispose();
            //for (int i = 0; i < _height; i++)
            //{
            //    for (int j = 0; j < _width; j++)
            //    {
            //        var imagePixel = (byte)(0.3 * imagePixels[i][j* _bytesPerPixel] + 0.59 * imagePixels[i][j * _bytesPerPixel + 1] + 0.11 * imagePixels[i][j * _bytesPerPixel + 2]);
            //        _median[i][j] = (byte)(_median[i][j] + Math.Sign(imagePixel - _median[i][j]));//consult atricle
            //        _delta[i][j] = (byte)(Math.Abs(imagePixel - _median[i][j]));
            //        if (_delta[i][j] > 0.001)
            //        {
            //            _variance[i][j] = _variance[i][j] + Math.Sign(N * _delta[i][j] - _variance[i][j]);
            //        }
            //        if (_delta[i][j] > 0.001)
            //        {
            //            _detectionLabel[i][j] = (byte)(Convert.ToByte(_delta[i][j] >= _variance[i][j]) * 255);
            //        }
            //        else
            //        {
            //            _detectionLabel[i][j] = 0;
            //        }
            //    }
            //}
        }

        private void InitializeAlgorithm(byte[] imagePixels,int stride)
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    _median[i * _width + j] = (byte)(0.3*imagePixels[i * stride + j* _bytesPerPixel] + 0.59*imagePixels[i * stride + j* _bytesPerPixel + 1]+ 0.11*imagePixels[i * stride + j *_bytesPerPixel+2]);
                }
            }

        }
    }
}
