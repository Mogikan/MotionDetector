using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetector
{
    public interface IUpdatableCommand
    {
        void UpdateCanExecute();
        void SetIsSupported(bool value);
    }
}
