using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircularBuffer
{

    /*
     *  Simple locking implementation. 
     *  For low-contention, this is good enough, and simpler to maintain.
     *  Heavily Prefer this, unless there is a reason to use a more complex implementation
     *  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
     * 
     * Just wrap all objects in Lock(), to make thread safe.
     * 
     * We could also do this by composing the Non-Threaded class as a member variable, 
     * and writing wrapper functions that just call the composed non-threaded class inside a lock.
     * That would let us share code between the two, rather than have copied code. 
     * Which you use depends on whether you need both. If you need both, then this composing approach is better. 
     * If you need only one, then the approach implemented below is better, and delete the Non-Threaded one from code tree.
     * 
     */


    public class ConcurrentCircularBuffer : ICircularBuffer
    {
        long size;       // Can take upto 8Billion elements. Size is immutable once created.
        int[] buffer;
        long head = 0;  // Position of next empty element. This can also be interpreted as number of elements inserted into buffer so far
        long tail = 0;  //  Position of last occupied element. This can also be interpreted as number of elements removed from buffer so far
        private object lockObj = new Object();


        public ConcurrentCircularBuffer(long size)
        {
            this.size = size;     // we allocate one more than needed, because it is the sentinel position to indicate fullness of buffer
            this.head = 0;
            this.tail = 0;
            buffer = new int[this.size];
        }

        public static ICircularBuffer getBuffer(long size) {
            // This is thread-safe, since there are no static variables used - so there is no shared state between method invocations
            if (size > 0) { return new NonThreadedCircularBuffer(size); }
            else { return null; } 
        }

        public bool Put(int i)
        {
            lock (lockObj)
            {
                if (head == (tail + size))  // Check if Full
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
        }

        public int Get()
        {
            lock (lockObj)
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
        }

        public long Count
        {
            get
            {
                lock (lockObj)
                {
                    return (head - tail);
                }
            }
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
