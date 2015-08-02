using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircularBuffer
{
    public class NonThreadedCircularBuffer : ICircularBuffer
    {
        long size;       // Can take upto 8Billion elements. Size is immutable once created.
        int[] buffer;
        long head = 0;  // Position of next empty element. This can also be interpreted as number of elements inserted into buffer so far
        long tail = 0;  //  Position of last occupied element. This can also be interpreted as number of elements removed from buffer so far

        public NonThreadedCircularBuffer(long size)
        {
            this.size = size;     // we allocate one more than needed, because it is the sentinel position to indicate fullness of buffer
            this.head = 0;
            this.tail = 0;
            buffer = new int[this.size];
        }

        public static ICircularBuffer getBuffer(long size) {
            if (size > 0) { return new NonThreadedCircularBuffer(size); }
            else { return null; } 
        }

        public bool Put(int i)
        {
            if (head==(tail+size))  // Check if Full
            {
                return false;
            }
            else
            {
                long currentHead = head;
                head++;
                buffer[getPhysical(currentHead)] = i;
                return true;
            }
        }

        public int Get()
        {
            if (head == tail)    // Check if empty
            {
                return -1;      // Should be exception, or some other way of signalling error. Current protocol restricts values to 0 & positive only.
            }
            else
            {
                long currentTail = tail;
                tail++;
                int i = buffer[getPhysical(currentTail)];
                return i;
            }
        }

        public long Count
        {
            get { return (head - tail); }
        }

        public long Capacity
        {
            get { return size; }
        }

        private long getPhysical(long logical)
        {
            return logical % size;
        }

    }
}
