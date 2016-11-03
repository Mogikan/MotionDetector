using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    internal class FieldDescriptor:Observable
    {
        public string Id { get; set; }
        public string Caption { get; set; }
        public string Value { get; set; }
        public bool IsReliable { get; set; }

    }
}
