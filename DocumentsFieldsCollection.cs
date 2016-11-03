using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public class DocumentsFieldsCollection:KeyedCollection<string, FieldConfidenceAggregator>
    {
        protected override string GetKeyForItem(FieldConfidenceAggregator item)
        {
            return item.FieldName;
        }
    }
}
