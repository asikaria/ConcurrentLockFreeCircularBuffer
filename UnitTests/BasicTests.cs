using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CircularBuffer;

namespace UnitTests
{

    public class BasicTests
    {


        public static void Main(string[] args)
        {
            BasicTests bt = new BasicTests();
            //bt.BaseSequence();
            RunTest(bt.BaseSequence);
            Console.WriteLine("Done...");
            Console.ReadLine();
        }

        public delegate void TestMethod();

        public static void RunTest(TestMethod r)
        {
            try
            {
                r();
            }
            catch (TestFailedException) { }
        }

        public void BaseSequence()
        {

            Assert.That(NonThreadedCircularBuffer.getBuffer(0) == null, "Trying to create abuffer of size 0 should fail");
            Assert.That(NonThreadedCircularBuffer.getBuffer(-1) == null, "Trying to create abuffer of negative size should fail");

            // create Buffer
            long size = 4;
            ICircularBuffer cb = NonThreadedCircularBuffer.getBuffer(size);
            Assert.That(cb.Capacity == 4, "Capacity should be 4"); // otherwise other tests below will fail

            // cases around empty and one-element buffer
            int i;
            Assert.That(cb.Count == 0, "Initial size should be 0");
            i = cb.Get();
            Assert.That(i == -1, "It should not be possible to pull anything from a just-initialized buffer");
            Assert.That(cb.Put(16987), "First insert should succeed");  // side-effect: insert into buffer
            Assert.That(cb.Count == 1, "Count after first insert should be 1");
            i = cb.Get();
            Assert.That(i == 16987, "First element returned should be same as first element inserted"); // side-effect: remove from buffer
            Assert.That(cb.Count == 0, "Size should be 0 after inserting and removing 1 element");
            i = cb.Get();
            Assert.That(i == -1, "Removing from empty should fail");

            // Full size and test
            for (int j = 0; (long)j < size; j++)
            {
                Assert.That(cb.Put(j), String.Format("Insert {0} should succeed", j));
                Assert.That(cb.Count == ((long)j + 1), String.Format("Size should match number inserted: {0}", j));
            }
            Assert.False(cb.Put(100), "Should overflow after 4 inserts");
            Assert.That(cb.Count == 4, "Count should still be 4 after failed insert");

            // Empty the Full buffer and test
            for (int j = 0; (long)j < size; j++)
            {
                Assert.That(cb.Count == ((long)(4 - j)), String.Format("Size should match number inserted minus removed: {0}. Got {1}", j, cb.Count));
                i = cb.Get();
                Assert.That(i == j, String.Format("Remove {0} should succeed and return value {0}. Instead got {1}", j, i));

            }
            Assert.That(cb.Count == 0, "Size should be 0 after inserting and removing 4 elements");
            i = cb.Get();
            Assert.That(i == -1, "Removing from empty should fail after 4 elements");

            //Wrap around test
            cb.Put(1);
            cb.Put(2);
            cb.Get();  // 1
            cb.Put(3);
            cb.Put(4);
            cb.Get();  // 2 
            cb.Get();  // 3
            cb.Put(5);
            cb.Put(6);
            cb.Put(7);
            Assert.False(cb.Put(100), "Should overflow after wrapped around fill");
            Assert.That(cb.Count == 4, "Count should still be 4 after wrapped insert failure");
            i = cb.Get();  // 4
            Assert.That(i == 4, "This should have got value 4");
            cb.Put(8);
            Assert.False(cb.Put(100), "Should overflow after wrapped around fill. ...2");
            Assert.That(cb.Count == 4, "Count should still be 4 after wrapped insert failure. ...2");
            i = cb.Get(); // 5
            i = cb.Get(); // 6
            i = cb.Get(); // 7
            i = cb.Get(); // 8
            i = cb.Get(); // invalid
            Assert.That(i == -1, "Removing from empty should fail after wrapping around");
            Assert.That(cb.Count == 0, "Count should be 0 after wrapping and emptying");
        }
    }

    // test harness for unit tests
    public static class Assert
    {
        public static void That(bool b, String msg)
        {
            if (!b)
            {
                Console.WriteLine("Failed: {0}", msg);
                throw new TestFailedException();
            }
        }

        public static void True(bool b, String msg)   // Just an Alias for "That"
        {
            That(b, msg);
        }

        public static void False(bool b, String msg)
        {
            if (b)
            {
                Console.WriteLine("Failed: {0}", msg);
                throw new TestFailedException();
            }
        }
    }

    class TestFailedException : Exception { }
}
