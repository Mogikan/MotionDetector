
#define _SIZE_T_DEFINED
#ifndef __CUDACC__
#define __CUDACC__
#endif
#ifndef __cplusplus
#define __cplusplus
#endif

#include <cuda.h>
#include <device_launch_parameters.h>
#include <texture_fetch_functions.h>
#include "float.h"
#include <builtin_types.h>
#include <vector_functions.h>



//        var imagePixel = (byte)(0.3 * imagePixels[i * stride + j * _bytesPerPixel] + 0.59 * imagePixels[i][j * _bytesPerPixel + 1] + 0.11 * imagePixels[i][j * _bytesPerPixel + 2]);
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


const int N = 4;
extern "C" __global__ void motionKernel(const unsigned char *imagePixels, unsigned char *median, unsigned char* delta, int* variance, unsigned char* motionBytes, unsigned int bytesPerPixel)
{
	int index = blockIdx.x * blockDim.x + threadIdx.x;
	int imagePixel = (int)(0.3f * imagePixels[index * bytesPerPixel] + 0.59f * imagePixels[index * bytesPerPixel + 1] + 0.11f * imagePixels[index * bytesPerPixel + 2]);
	int medianDiff = (imagePixel - median[index]);
	median[index] +=  (medianDiff>0)?1:((medianDiff<0)?-1:0);
	int absdelta = abs(imagePixel - median[index]);
	delta[index] = (unsigned char)absdelta;
	if (absdelta > 0)
	{		
		int varianceChange = (N * absdelta - variance[index]);
		variance[index] = variance[index] + (varianceChange>0)?1:((varianceChange<0)?-1:0);
		motionBytes[index] = 255 * (absdelta >= variance[index]);
	}
	else
	{
		motionBytes[index] = 0;
	}
}


	//_median[i][j] = (byte)(_median[i][j] + Math.Sign(imagePixel - _median[i][j]));//consult atricle
	//_delta[i][j] = (byte)(Math.Abs(imagePixel - _median[i][j]));
	//
	//if (_delta[i][j] > 0.001)
	//{
	//	_variance[i][j] = _variance[i][j] + Math.Sign(N * _delta[i][j] - _variance[i][j]);
	//	_detectionLabel[i][j] = (byte)(Convert.ToByte(_delta[i][j] >= _variance[i][j]) * 255);
	//}
	//else
	//{
	//	_detectionLabel[i][j] = 0;
	//}


//int main()
//{
//	const int arraySize = 5;
//	const int a[arraySize] = { 1, 2, 3, 4, 5 };
//	const int b[arraySize] = { 10, 20, 30, 40, 50 };
//	int c[arraySize] = { 0 };
//
//	// Add vectors in parallel.
//	cudaError_t cudaStatus = addWithCuda(c, a, b, arraySize);
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "addWithCuda failed!");
//		return 1;
//	}
//
//	printf("{1,2,3,4,5} + {10,20,30,40,50} = {%d,%d,%d,%d,%d}\n",
//		c[0], c[1], c[2], c[3], c[4]);
//
//	// cudaDeviceReset must be called before exiting in order for profiling and
//	// tracing tools such as Nsight and Visual Profiler to show complete traces.
//	cudaStatus = cudaDeviceReset();
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaDeviceReset failed!");
//		return 1;
//	}
//	fgetc(stdin);
//	return 0;
//}
//
//// Helper function for using CUDA to add vectors in parallel.
//cudaError_t addWithCuda(int *c, const int *a, const int *b, unsigned int size)
//{
//	//var imagePixel = (byte)(0.3 * imagePixels[i][j* _bitsPerPixel] + 0.59 * imagePixels[i][j * _bitsPerPixel + 1] + 0.11 * imagePixels[i][j * _bitsPerPixel + 2]);
//	int *dev_a = 0;
//	int *dev_b = 0;
//	int *dev_c = 0;
//	cudaError_t cudaStatus;
//
//	// Choose which GPU to run on, change this on a multi-GPU system.
//	cudaStatus = cudaSetDevice(0);
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaSetDevice failed!  Do you have a CUDA-capable GPU installed?");
//		goto Error;
//	}
//
//	// Allocate GPU buffers for three vectors (two input, one output)    .
//	cudaStatus = cudaMalloc((void**)&dev_c, size * sizeof(int));
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMalloc failed!");
//		goto Error;
//	}
//
//	cudaStatus = cudaMalloc((void**)&dev_a, size * sizeof(int));
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMalloc failed!");
//		goto Error;
//	}
//
//	cudaStatus = cudaMalloc((void**)&dev_b, size * sizeof(int));
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMalloc failed!");
//		goto Error;
//	}
//
//	// Copy input vectors from host memory to GPU buffers.
//	cudaStatus = cudaMemcpy(dev_a, a, size * sizeof(int), cudaMemcpyHostToDevice);
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMemcpy failed!");
//		goto Error;
//	}
//
//	cudaStatus = cudaMemcpy(dev_b, b, size * sizeof(int), cudaMemcpyHostToDevice);
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMemcpy failed!");
//		goto Error;
//	}
//	
//
//	// Check for any errors launching the kernel
//	cudaStatus = cudaGetLastError();
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "addKernel launch failed: %s\n", cudaGetErrorString(cudaStatus));
//		goto Error;
//	}
//
//	// cudaDeviceSynchronize waits for the kernel to finish, and returns
//	// any errors encountered during the launch.
//	cudaStatus = cudaDeviceSynchronize();
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaDeviceSynchronize returned error code %d after launching addKernel!\n", cudaStatus);
//		goto Error;
//	}
//
//	// Copy output vector from GPU buffer to host memory.
//	cudaStatus = cudaMemcpy(c, dev_c, size * sizeof(int), cudaMemcpyDeviceToHost);
//	if (cudaStatus != cudaSuccess) {
//		fprintf(stderr, "cudaMemcpy failed!");
//		goto Error;
//	}
//
//Error:
//	cudaFree(dev_c);
//	cudaFree(dev_a);
//	cudaFree(dev_b);	
//	return cudaStatus;
//}
