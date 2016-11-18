using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
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

        
        public Bitmap MotionPicture
        {
            get
            {
                if (_imageBytes == null)
                {
                    return new Bitmap(1, 1);
                }
                var result = ImageUtils.GetImageFromBytes(
                    ImageUtils.DrawMotionContours(                    
                        ImageUtils.Erode(_detectionLabel,_width,_height)
                        ,_imageBytes
                        ,_width
                        ,_height)
                    ,_width
                    ,_height);                
                return result; 
            }
        }

        

        private const int N = 4;
        private bool isFirstStep = true;
        private byte[] _imageBytes;
        
        public void ExecuteStep(byte[] imagePixels,int stride)
        {
        //    _stride = stride;
            _imageBytes = imagePixels;
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
