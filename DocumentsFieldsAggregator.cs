using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public class DocumentsFieldsAggregator
    {
        DocumentsFieldsCollection fields = new DocumentsFieldsCollection();
        public DocumentsFieldsCollection Fields { get { return fields; } }
        public void AggregateFieldConfidence(string fieldName, string fieldValue, double confidence)
        {
            FieldConfidenceAggregator field = null;
            if (fields.Contains(fieldName))
            {
                field = fields[fieldName];
            }
            else
            {
                field = new FieldConfidenceAggregator(fieldName);
                fields.Add(field);
            }
            field.AddFieldRecognitionResult(new RecognitionValue()
            {
                AccumulatedConfidence = confidence,
                Confidence = confidence,
                Value = fieldValue,
            });
        }
    }
}
