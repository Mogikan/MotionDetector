using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                return ConvertBitmapToSource(_sigmaDeltaBackgroundSubtractionAllgorithm.MotionPicture);
            }
        }

        //public bool[][] MovementMap
        //{
        //    get
        //    {
        //        return _sigmaDeltaBackgroundSubtractionAllgorithm.MovementMap;
        //    }
        //}

        public int TaskCount { get; set; }

        private DocumentsFieldsAggregator fieldsAggregator = new DocumentsFieldsAggregator();

        private Dictionary<string, RecognitionValue> answerFields = new Dictionary<string, RecognitionValue>();
        public ObservableCollection<FieldDescriptor> RecognizedFields
        {
            get;
            private set;
        }

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

        private void RecognizeImageAsync()
        {         
            byte[][] imageBytes = GetImageBytes(Source);
            _sigmaDeltaBackgroundSubtractionAllgorithm.ExecuteStep(imageBytes);
            NotifyPropertyChanged(()=> MotionPicture);
        }
        private static object locker = new object();
        private byte[][] GetImageBytes(Bitmap bitmap)
        {
            int imageWidth = bitmap.Width;
            int imageHeight = bitmap.Height;
            BitmapData bmpdata = null;
            lock (locker) {
                try
                {
                    bmpdata = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    var totalImageBytes = bmpdata.Stride * bitmap.Height;
                    byte[][] buffer = new byte[imageHeight][];

                    var ptr = bmpdata.Scan0;
                    for (int j = 0; j < imageHeight; j++)
                    {
                        buffer[j] = new byte[bmpdata.Stride];
                        Marshal.Copy(ptr, buffer[j], 0, bmpdata.Stride);
                        ptr = ptr + bmpdata.Stride;
                    }
                    return buffer;
                }
                finally
                {
                    if (bmpdata != null)
                        bitmap.UnlockBits(bmpdata);
                }
            }
        }

        private Task StartRecognitionAsync()
        {
            return  Task.Run(() => RecognizeImageAsync());
        }
    }
}
