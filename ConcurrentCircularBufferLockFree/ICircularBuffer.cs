using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircularBuffer
{
    public interface ICircularBuffer
    {
        bool Put(int i);
        int Get();
        long Count { get; }
        long Capacity { get; }
    }
}
