using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CircularBuffer
{

    /*
     * References:
     *     https://en.wikipedia.org/wiki/Circular_buffer
     *     http://www.codeproject.com/Articles/153898/Yet-another-implementation-of-a-lock-free-circular
     *     http://lmax-exchange.github.io/disruptor/files/Disruptor-1.0.pdf
     * 
     * It is critical to get the non-threaded version functionally correct first, then try to reason about order of operations for concurrent version
     * 
    */

    public class ConcurrentLockFreeCircularBuffer : ICircularBuffer
    {
        long size;       // Can take upto 8Billion elements. Size is immutable once created.
        int[] buffer;
        long head = 0;  // Position of next empty element. This can also be interpreted as number of elements inserted into buffer so far
        long tail = 0;     //  Position of last occupied element. This can also be interpreted as number of elements removed from buffer so far
        long published = 0;  // this will only be useful in lock-free concurrent version
        long consumed = 0;   // this will only be useful in lock-free concurrent version
        // in the concurrent version, we have to separate "reserved" slots from "used" slots
        // head represents what slot has been reserved, and published represents what has been actually inserted. Get should not return anything that has not been published.
        // tail represents read reservation, and consumed represents actually read. Put should not write beyond what has been confirmed consumed


        public ConcurrentLockFreeCircularBuffer(long size)
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
            long currentHead, currentTail;  // read these first. Then do computations, and reject the computation if value has changed in the meantime
            long loopcount = 0;

            do
            {
                currentHead = head;  // Read head first
                currentTail = consumed;  // Read tail next. Head may have gone up by now, so you will say "full" in error, but does not compromise safety of stored data
                // rest of the loop uses these snapshotted values of head and tail. The order of reads is important for safety.
                if (currentHead == (currentTail + size))  // Check if Full
                {
                    return false;
                }
                loopcount++;
            } while (!(Interlocked.CompareExchange(ref head, head + 1, currentHead) == currentHead)); // this repeats the loop if head has moved in the meantime by some other thread, and does not match currentHead
            // after the loop terminates, currentHead represents the slot we got reserved. 
            // The value in the slot is still not valid. Reads should not read this value until it has been safely filled

            // Note that even though the algorithm is lock-free threads can compete if they keep overwriting each other's stale values
            // Progress is still guaranteed though, since Interlocked has to succeed for one thread, and only interlocked succeeding represents invalidation for other threads
            // TODO: To measure perf, we can report loopcounter into logs/perf-counters. High counts mean high contention.

            buffer[getPhysical(currentHead)] = i;  // fill the slot
            
            // Now we need to move the published counter forward. This can only move when previously reserved slots have caught up. 
            // Published means it is safe for readers to read this value now
            while (!(Interlocked.CompareExchange(ref published, currentHead, currentHead - 1) == (currentHead - 1)))   // succeed only if published is caught up
            {
                Thread.Yield();   // might as well let some other thread run, otherwise this would be a tight loop on a single-proc system until thread quantum expires
            }

            return true;

        }

        public int Get()
        {
            long currentHead, currentTail;  // read these first. Then do computations, and reject the computation if value has changed in the meantime
            long loopcounter = 0;
            do
            {
                currentTail = tail;  // Read tail first
                currentHead = published;  // Read (published) head next. Tail may have gone up by now, so you will say "empty" in error, but does not compromise safety of stored data
                // rest of the loop uses these snapshotted values of head and tail. The order of reads is important for safety.
                if (currentTail == currentHead)    // Check if empty
                {
                    return -1;      // Should be exception, or some other way of signalling error. Current protocol restricts values to 0 & positive only.
                }
                loopcounter++;
            } while (!(Interlocked.CompareExchange(ref tail, tail + 1, currentTail) == currentTail)); // this repeats the loop if tail has moved in the meantime by some other thread, and does not match currentTail
            // after the loop terminates, currentTail represents the slot we got reserved for reading
            // The value in the slot is still not consumed/copied. Writers should not overwrite this value until it has been safely consumed

            Thread.MemoryBarrier();                    // Yes, this is needed. Because even though the interlocked variables themselves get invalidated in the cache, 
                                                       // the array locations are not. Reading here may read a stale value from last wrap around the buffer, unless there is a barrier
                                                       // Interlocked is not a full memory barrier, it just guarantees the variable touched is flushed from caches

            int i = buffer[getPhysical(currentTail)];  // copy the value into i (represents "consuming" the value)

            while (!(Interlocked.CompareExchange(ref consumed, currentTail, currentTail - 1) == (currentTail - 1)))   // succeed only if consumed is caught up
            {
                Thread.Yield();   
            }

            return i;
            
        }

        public long Count
        {
            // approximate value, since values are read at different times. It can even become negative! 
            // Accurate value needs consistent-point-in-time values for both head and tail, which is not possible to get simultaneously
            // So for the lock-free implementation, count may not be of much use
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
