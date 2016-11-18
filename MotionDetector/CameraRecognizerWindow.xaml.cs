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
        
            var camera = WPFMediaKit.DirectShow.Controls.MultimediaUtil.VideoInputDevices.First();
            
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
         }

        private DateTime lastCapturedImageTime = DateTime.Now;

        private void VideoCaptureElement_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            var currentTime = DateTime.Now;
            if ((currentTime - lastCapturedImageTime).TotalMilliseconds >= 100
                && videoCaptureViewModel.EnableRecognizing)
            {
                var imageToRecognize = e.VideoFrame;
                imageToRecognize.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Bitmap croppedBitmap = imageToRecognize;
                
                
                videoCaptureViewModel.Source = croppedBitmap;
                videoCaptureViewModel.RecognizeMotionCommand.Execute(null);
                lastCapturedImageTime = DateTime.Now;
            }
        }

        

        private void CaptureButtonClick(object sender, RoutedEventArgs e)
        {
            videoCaptureViewModel.EnableRecognizing = true;
        }        
    }
}
