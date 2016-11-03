using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public class RecognitionValue:IComparable<RecognitionValue>
    {
        public double AccumulatedConfidence { get; set; }
        public double Confidence { get; set; }
        public string Value { get; set; }

        public int CompareTo(RecognitionValue other)
        {
            if (other.AccumulatedConfidence > AccumulatedConfidence)
            {
                return -1;
            }
            if (other.AccumulatedConfidence == AccumulatedConfidence)
            {
                return 0;
            }
            return 1;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
