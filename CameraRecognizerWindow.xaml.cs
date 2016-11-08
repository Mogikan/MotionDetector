using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DirectShowLib;

namespace MotionDetector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CameraRecognizerWindow : Window
    {
        public CameraRecognizerWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;            
        }

        private int minFocusValue;
        private int maxFocusValue;
        private int focusValue;
        private int focusDelta;
        CameraControlFlags controlFlags;
        int defaultFocusValue;

        private int additionalMarginValue = 50;
        private int minimumBoundHeight = 400;
        private int bottomFocusThreshold = 13;
        private const int DesiredCameraHeight = 480;
        private const int DesiredCameraWidth = 640;
        private const int BytesPerPixel = 3;
        private IAMCameraControl cameraControl;
        private VideoCaptureViewModel videoCaptureViewModel;
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            videoCaptureElement.DesiredPixelHeight = DesiredCameraHeight;
            videoCaptureElement.DesiredPixelWidth = DesiredCameraWidth;
            DataContext = videoCaptureViewModel = new VideoCaptureViewModel(DesiredCameraWidth,DesiredCameraHeight,BytesPerPixel);
            //videoCaptureViewModel.Initialize();

            videoCaptureViewModel.PropertyChanged += VideoCaptureViewModel_PropertyChanged;
            var camera = WPFMediaKit.DirectShow.Controls.MultimediaUtil.VideoInputDevices.First();
            
            //videoCaptureElement.DesiredPixelHeight = 1080;
            //videoCaptureElement.DesiredPixelWidth = 1920;
            videoCaptureElement.VideoCaptureSource = camera.Name;
            videoCaptureElement.NewVideoSample += VideoCaptureElement_NewVideoSample;
            videoCaptureElement.EnableSampleGrabbing = true;

            IFilterGraph2 graphBuilder = new FilterGraph() as IFilterGraph2;
            IBaseFilter capFilter = null;
            graphBuilder?.AddSourceFilterForMoniker(camera.Mon, null, camera.Name,
                out capFilter);
            cameraControl = capFilter as IAMCameraControl;

            cameraControl.GetRange(CameraControlProperty.Focus, out minFocusValue, out maxFocusValue, out focusDelta, out defaultFocusValue, out controlFlags);
            cameraControl.Get(CameraControlProperty.Focus, out focusValue, out controlFlags);
            focusSlider.Maximum = maxFocusValue;
            focusSlider.Minimum = Math.Max(minFocusValue, focusDelta);
            focusSlider.TickFrequency = focusDelta;
            focusSlider.Value = focusValue == 0 ? (maxFocusValue + minFocusValue) / 2:focusValue;
            Debug.WriteLine("Min Focus Value  = {0}, Max Focus Value = {1}", minFocusValue, maxFocusValue);
        }

        private DateTime lastCapturedImageTime = DateTime.Now;

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);            
            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new System.Drawing.Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
        private void VideoCaptureElement_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            var currentTime = DateTime.Now;
            if ((currentTime - lastCapturedImageTime).TotalMilliseconds >= 100
                && videoCaptureViewModel.EnableRecognizing)
                //&& videoCaptureViewModel.TaskCount < 3)
            {
                var imageToRecognize = e.VideoFrame;
                //imageToRecognize = MakeGrayscale(imageToRecognize);
                imageToRecognize.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Bitmap croppedBitmap = imageToRecognize;
                
                
                videoCaptureViewModel.Source = croppedBitmap;
                videoCaptureViewModel.RecognizeMotionCommand.Execute(null);
                lastCapturedImageTime = DateTime.Now;
            }
        }

        private void VideoCaptureViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "MovementMap")
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        videoOverlay.Children.Clear();
            //        for (int i = 0; i < videoCaptureViewModel.MovementMap.Length; i++)
            //        {
            //            for (int j = 0; j < videoCaptureViewModel.MovementMap[i].Length; j++)
            //            {
            //                if (videoCaptureViewModel.MovementMap[i][j])
            //                {
            //                    Line movementLine = new Line();
            //                    movementLine.Stroke = new SolidColorBrush(System.Windows.Media.Colors.Lime);
            //                    movementLine.X1 = j;
            //                    movementLine.Y1 = i;
            //                    movementLine.X2 = j;
            //                    movementLine.Y2 = i;
            //                    videoOverlay.Children.Add(movementLine);
            //                }
            //            }
            //        }
                    
            //    });
            //}
        }

        private void CaptureButtonClick(object sender, RoutedEventArgs e)
        {
            videoCaptureViewModel.EnableRecognizing = true;
        }

        private void FocusSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cameraControl.Set(CameraControlProperty.Focus, (int)focusSlider.Value, CameraControlFlags.Manual);
            Dispatcher.Invoke(() =>
            {
                //var normalizedFocusValue = focusSlider.Value - bottomFocusThreshold;
                //documentBound.Height = Math.Min(documentCanvas.Height, Math.Max(minimumBoundHeight, normalizedFocusValue * 100));
                //documentBound.Width = Math.Min(documentCanvas.Width, documentBound.Height * 1.4);
                //Canvas.SetLeft(documentBound, (documentCanvas.Width - documentBound.Width) / 2);
                //Canvas.SetTop(documentBound, Math.Max(0, documentCanvas.Height - documentBound.Height - additionalMarginValue));
            });
        }
    }
}
