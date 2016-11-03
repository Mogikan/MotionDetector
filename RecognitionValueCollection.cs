using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public class RecognitionValueCollection:KeyedCollection<string,RecognitionValue>
    {
        protected override string GetKeyForItem(RecognitionValue item)
        {
            return item.Value;
        }
    }
}
