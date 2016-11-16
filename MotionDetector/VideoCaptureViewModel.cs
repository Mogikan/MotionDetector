using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MotionDetector
{
    internal class VideoCaptureViewModel: Observable
    {
        public VideoCaptureViewModel(int width, int height,int bytesPerPixel)
        {
            RecognizeMotionCommand = new Command<object>(() => { RecognizeImage(); }, () => { return true; });
            _sigmaDeltaBackgroundSubtractionAllgorithm = new SigmaDeltaBackgroundSubtractionAlgorithm(width, height,bytesPerPixel);
        }

        SigmaDeltaBackgroundSubtractionAlgorithm _sigmaDeltaBackgroundSubtractionAllgorithm;
        public ICommand RecognizeMotionCommand { get; private set; }

        public Bitmap Source
        {
            get
            {
                return source;
            }
            set
            {
                this.source = value;
                NotifyPropertyChanged(()=>Source);
            }
        }

        public BitmapSource ConvertBitmapToSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgra32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public BitmapSource MotionPicture
        {
            get
            {
                lock (locker)
                {
                    return ConvertBitmapToSource(_sigmaDeltaBackgroundSubtractionAllgorithm.MotionPicture);
                }
            }
        }
        
        public int TaskCount { get; set; }

        private bool enableRecognizing;
        public bool EnableRecognizing
        {
            get
            {
                return enableRecognizing;
            }
            set
            {
                this.enableRecognizing = value;
            }
        }


        private Bitmap source;
        
        private const double AverageConfidenceThreshold = 0.85;
        private const double FieldConfidenceTreshold = 0.8;
        private const int  LaplassianThreshold = 100;        
        
        private async void RecognizeImage()
        {
            TaskCount++;
            await Task.Run(() => RecognizeImageAsync());
            TaskCount--;
        }
        static object locker = new object();
        private void RecognizeImageAsync()
        {
            lock (locker)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                int stride;
                byte[] imageBytes = GetImageBytes(Source,out stride);
            
                _sigmaDeltaBackgroundSubtractionAllgorithm.ExecuteStep(imageBytes, stride);
                NotifyPropertyChanged(()=> MotionPicture);
                watch.Stop();
                System.Diagnostics.Debug.WriteLine($"MotionDetection {watch.ElapsedMilliseconds}");
            }

        }
        private byte[] GetImageBytes(Bitmap bitmap,out int stride)
        {
            int imageWidth = bitmap.Width;
            int imageHeight = bitmap.Height;
            BitmapData bmpdata = null;
            lock (locker) {
                try
                {                    
                    bmpdata = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    stride = bmpdata.Stride;
                    var totalImageBytes = bmpdata.Stride * bitmap.Height;
                    byte[] buffer = new byte[totalImageBytes];
                    var ptr = bmpdata.Scan0;
                    Marshal.Copy(ptr, buffer, 0,totalImageBytes);                                        
                    return buffer;
                }
                finally
                {
                    if (bmpdata != null)
                        bitmap.UnlockBits(bmpdata);
                }
            }
        }
    }
}
