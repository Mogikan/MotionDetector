using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public class FieldConfidenceAggregator
    {
        public FieldConfidenceAggregator(string fieldName)
        {
            FieldName = fieldName;
        }
        public string FieldName { get;private  set; }
        RecognitionValueCollection fieldConfidences = new RecognitionValueCollection();
        public void AddFieldRecognitionResult(RecognitionValue recognitionValue)
        {
            if (fieldConfidences.Contains(recognitionValue.Value))
            {
                fieldConfidences[recognitionValue.Value].AccumulatedConfidence += recognitionValue.AccumulatedConfidence;
                fieldConfidences[recognitionValue.Value].Confidence = Math.Max(fieldConfidences[recognitionValue.Value].Confidence, recognitionValue.Confidence);
            }
            else
            {
                fieldConfidences.Add(recognitionValue);
            }
        }

        public RecognitionValue SelectBestFieldResult()
        {
            return fieldConfidences.Max();
        }
    }
}
