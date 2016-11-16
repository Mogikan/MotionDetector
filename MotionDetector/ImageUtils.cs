//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MotionDetector
//{
//    public class ImageUtilsBase
//    {
//        private short[,] se = new short[5, 5]
//    {
//            { 1, 1, 1 , 1, 1},
//            { 1, 1, 1 , 1, 1},
//            { 1, 1, 1 , 1, 1},
//            { 1, 1, 1 , 1, 1},
//            { 1, 1, 1 , 1, 1},
//    };
//        public Bitmap Erode(byte[] input)
//        {
//            var destination = new byte[_height][];
//            for (int i = 0; i < _height; i++)
//            {
//                destination[i] = new byte[_width];
//            }
//            int size = 3;
//            // processing start and stop X,Y positions
//            int startX = 0;
//            int startY = 0;
//            int stopX = _width;
//            int stopY = _height;

//            // structuring element's radius
//            int r = size >> 1;

//            // flag to indicate if at least one pixel for the given structuring element was found
//            bool foundSomething;

//            // grayscale image

//            // compute each line
//            for (int y = startY; y < _height; y++)
//            {
//                byte[] sourceBytesLine = input[y];
//                int sourceLine = 0;
//                int destinationLineIndex = 0;
//                byte[] destinationBytesLine = destination[y];

//                byte min, v;

//                // loop and array indexes
//                int verticalIndex, horizontalIndex, ir, jr, i, j;

//                // for each pixel
//                for (int x = startX; x < stopX; x++, sourceLine++, destinationLineIndex++)
//                {
//                    min = 255;
//                    foundSomething = false;

//                    // for each structuring element's row
//                    for (i = 0; i < size; i++)
//                    {
//                        ir = i - r;
//                        verticalIndex = y + ir;

//                        // skip row
//                        if (verticalIndex < startY)
//                            continue;
//                        // break
//                        if (verticalIndex >= stopY)
//                            break;

//                        // for each structuring element's column
//                        for (j = 0; j < size; j++)
//                        {
//                            jr = j - r;
//                            horizontalIndex = x + jr;

//                            // skip column
//                            if (horizontalIndex < startX)
//                                continue;
//                            if (horizontalIndex >= stopX)
//                                continue;
//                            if (se[i, j] == 1)
//                            {
//                                foundSomething = true;
//                                // get new MIN value
//                                v = input[verticalIndex][horizontalIndex];
//                                if (v < min)
//                                    min = v;
//                            }

//                        }
//                    }
//                    // result pixel
//                    destination[y][x] = (foundSomething) ? min : input[y][x];
//                }
//            }
//            return destination;

//        }
//    }
//}
//}
