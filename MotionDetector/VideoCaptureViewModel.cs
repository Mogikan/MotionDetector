using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
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
            _width = width;
            _height = height;
            RecognizeMotionCommand = new Command<object>(() => { RecognizeImage(); }, () => { return true; });
            _sigmaDeltaBackgroundSubtractionAllgorithm = new SigmaDeltaBackgroundSubtractionAlgorithm(width, height,bytesPerPixel);
        }

        private int _width,_height;

        private SigmaDeltaBackgroundSubtractionAlgorithm _sigmaDeltaBackgroundSubtractionAllgorithm;
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

        

        

        public BitmapSource MotionPicture
        {
            get
            {
                lock (locker)
                {
                    return ImageUtils.ConvertBitmapToSource(_sigmaDeltaBackgroundSubtractionAllgorithm.MotionPicture);
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
                byte[] imageBytes = ImageUtils.GetImageBytes(Source,out stride);
            
                _sigmaDeltaBackgroundSubtractionAllgorithm.ExecuteStep(imageBytes, stride);                
                NotifyPropertyChanged(()=> MotionPicture);
                watch.Stop();
                System.Diagnostics.Debug.WriteLine($"MotionDetection {watch.ElapsedMilliseconds}");
            }
        }
        
    }
}
