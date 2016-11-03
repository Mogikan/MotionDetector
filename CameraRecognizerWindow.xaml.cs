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
            DataContext = videoCaptureViewModel = new VideoCaptureViewModel();
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

        private IAMCameraControl cameraControl;
        private VideoCaptureViewModel videoCaptureViewModel;
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //videoCaptureViewModel.Initialize();
            var camera = WPFMediaKit.DirectShow.Controls.MultimediaUtil.VideoInputDevices.First();
            videoCaptureElement.DesiredPixelHeight = 1080;
            videoCaptureElement.DesiredPixelWidth = 1920;
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
        private void VideoCaptureElement_NewVideoSample(object sender, WPFMediaKit.DirectShow.MediaPlayers.VideoSampleArgs e)
        {
            var currentTime = DateTime.Now;
            if ((currentTime - lastCapturedImageTime).TotalSeconds >= 2)
                //&& videoCaptureViewModel.EnableRecognizing
                //&& videoCaptureViewModel.TaskCount < 3)
            {
                var imageToRecognize = e.VideoFrame;
                imageToRecognize.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Bitmap croppedBitmap = imageToRecognize;
                Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine("Focus Value " + (int)focusSlider.Value);
                    var documentBoundLeft = (int) Canvas.GetLeft(documentBound);
                    var documentBoundTop = (int) Canvas.GetTop(documentBound);
                    var newDocumentBoundLeft = Math.Max(0, documentBoundLeft - additionalMarginValue);
                    var newDocumentBoundTop = Math.Max(0, documentBoundTop - additionalMarginValue);
                    croppedBitmap = imageToRecognize.Clone(new System.Drawing.Rectangle(
                                                            newDocumentBoundLeft,
                                                            newDocumentBoundTop,
                                                            Math.Min((int)documentCanvas.Width - newDocumentBoundLeft, (int)documentBound.Width +  2 * additionalMarginValue),
                                                            Math.Min((int)documentCanvas.Height - newDocumentBoundTop, (int)documentBound.Height + 2 * additionalMarginValue)),
                                                        imageToRecognize.PixelFormat);
                });
                
                videoCaptureViewModel.Source = croppedBitmap;
                videoCaptureViewModel.RecognizeMotionCommand.Execute(null);
                lastCapturedImageTime = DateTime.Now;
            }
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
                var normalizedFocusValue = focusSlider.Value - bottomFocusThreshold;
                documentBound.Height = Math.Min(documentCanvas.Height, Math.Max(minimumBoundHeight, normalizedFocusValue * 100));
                documentBound.Width = Math.Min(documentCanvas.Width, documentBound.Height * 1.4);
                Canvas.SetLeft(documentBound, (documentCanvas.Width - documentBound.Width) / 2);
                Canvas.SetTop(documentBound, Math.Max(0, documentCanvas.Height - documentBound.Height - additionalMarginValue));
            });
        }
    }
}
