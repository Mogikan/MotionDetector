using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;       

namespace MotionDetector
{
    internal class VideoCaptureViewModel: Observable
    {
        public VideoCaptureViewModel()
        {
        }


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
            //IPicture picture;
            //Bitmap sharpnessCheckChunkBitmap;

            //lock (Source)
            //{
            //    picture = pvService.PictureService.LoadFromBitmap(Source,
            //        Library.Core.Pictures.Implementation.PictureSourceInfo.CreateFromMemory());
            //    //ToDo try to find better rectangle than the whole image. (200, Source.Height / 2, 200, 200) - is a nice choice
            //    sharpnessCheckChunkBitmap = Source.Clone(new Rectangle(0, 0, Source.Width, Source.Height),
            //        Source.PixelFormat);
            //}

            //var inputFocusTestImage = AutoImage.FromBitmap(new DefaultImageManager(), sharpnessCheckChunkBitmap,
            //    "SharpnessCheck");
            //var outputFocusTestImage = new AutoImage<Bgr, byte>(new DefaultImageManager(), inputFocusTestImage.Size);
            //CvInvoke.Laplacian(inputFocusTestImage.Image, outputFocusTestImage.Image, DepthType.Cv32F);
            //double[] minV, maxV;
            //Point[] minL, maxL;
            //outputFocusTestImage.Image.MinMax(out minV, out maxV, out minL, out maxL);
            //Debug.WriteLine("Laplasian value " + maxV[0]);

            //if (maxV[0] < LaplassianThreshold || !enableRecognizing)
            //{
            //    return;
            //}

            //var settings =
            //    pvService.SettingService.CreateSettingSet(
            //        PassportVision.Documents.RussianPassport.Info.DocumentTypeInfoProvider.Instance);

            //var settingSetBuilder = pvService.SettingService.CreateSettingSetBuilder();
            ////TODO uncomment to dump images
            ////pvService.DebugService.Settings.DebugMode.SetValue(settingSetBuilder, pvService.DebugService.DebugModes.Full);
            //pvService.EnvironmentService.GuiSettings.ShowProgressBar.SetValue(settingSetBuilder, false);
            //var debugSettings = settingSetBuilder.Build();
            //var finalSettingSet = (settings == null) ? debugSettings : SettingHelper.Combine(settings, debugSettings);

            //var logger = pvService.DebugService.CreateLogger("VideoRecognizer", finalSettingSet);

            //logger.Info("Laplace value {0}", maxV[0]);

            //var result = pvService.RecognitionService.Recognize(picture, finalSettingSet);

            //lock (fieldsAggregator)
            //{
            //    if (result.FieldImages.Count > 2 && enableRecognizing)
            //    {
            //        System.Diagnostics.Debug.WriteLine("good");

            //        foreach (var fieldImage in result.Fields)
            //        {
            //            if (fieldImage.Info.IsRequired)
            //                fieldsAggregator.AggregateFieldConfidence(fieldImage.Info.Id,
            //                    fieldImage.RecognizedText.PlainText,
            //                    fieldImage.RecognizedText.AverageConfidence);
            //        }

            //        fieldsAggregator.Fields[0].SelectBestFieldResult();

            //        Dispatcher.Invoke(() =>
            //        {
            //            RecognizedFields.Clear();
            //            foreach (var field in fieldsAggregator.Fields)
            //            {
            //                var recognitionValue = field.SelectBestFieldResult();
            //                var isFieldReliable = recognitionValue.Confidence >= FieldConfidenceTreshold;
            //                RecognizedFields.Add(new FieldDescriptor()
            //                {
            //                    Id = field.FieldName,
            //                    Value = recognitionValue.Value,
            //                    IsReliable = isFieldReliable
            //                });
            //            }
            //        });

            //        foreach (var field in RecognizedFields)
            //        {
            //            System.Diagnostics.Debug.WriteLine(field.Value);
            //        }

            //        var averageConfidence = result.DocumentInfo.PageInfo.FieldsCount != 0
            //            ? fieldsAggregator.Fields.Sum(f => f.SelectBestFieldResult().Confidence)/
            //              result.DocumentInfo.PageInfo.FieldsCount
            //            : 0;
            //        Debug.WriteLine("average confidence = {0}", averageConfidence);
            //        if (averageConfidence > AverageConfidenceThreshold)
            //        {
            //            EnableRecognizing = false;
            //            foreach (var field in fieldsAggregator.Fields)
            //            {
            //                var recognitionValue = field.SelectBestFieldResult();
            //                var fieldName = field.FieldName;
            //                answerFields[fieldName] = recognitionValue;
            //            }
            //            fieldsAggregator = new DocumentsFieldsAggregator();
            //        }
            //    }
            //}
        }

        private Task StartRecognitionAsync()
        {
            return  Task.Run(() => RecognizeImageAsync());
        }
    }
}
