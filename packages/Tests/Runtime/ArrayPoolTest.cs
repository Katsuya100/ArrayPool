using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;

namespace Katuusagi.Pool.Tests
{
    public class ArrayPoolTest
    {
        [Test]
        public void Pool()
        {
            using (ArrayPool<int, _32>.Get(out var arr))
            {
                Assert.AreEqual(arr.Length, 32);
            }

            {
                var arr = ArrayPool<int, _32>.Get();
                Assert.AreEqual(arr.Length, 32);
                ArrayPool<int, _32>.TryReturn(arr);
            }
        }

        [Test]
        public void ThreadStaticPool()
        {
            using (ThreadStaticArrayPool<int, _32>.Get(out var arr))
            {
                Assert.AreEqual(arr.Length, 32);
            }

            {
                var arr = ThreadStaticArrayPool<int, _32>.Get();
                Assert.AreEqual(arr.Length, 32);
                ArrayPool<int, _32>.TryReturn(arr);
            }
        }

        [Test]
        public void ConcurrentPool()
        {
            using (ConcurrentArrayPool<int, _32>.Get(out var arr))
            {
                Assert.AreEqual(arr.Length, 32);
            }

            {
                var arr = ConcurrentArrayPool<int, _32>.Get();
                Assert.AreEqual(arr.Length, 32);
                ArrayPool<int, _32>.TryReturn(arr);
            }
        }


        [Test]
        public void Parallel_()
        {
            var wait = new SpinWait();
            var result = Parallel.For(0, 10000, (i) =>
            {
                using (ConcurrentArrayPool<int, _32>.Get(out var arr))
                {
                    Assert.AreEqual(arr.Length, 32);
                }
            });

            while (!result.IsCompleted)
            {
                wait.SpinOnce();
            }

            result = Parallel.For(0, 10000, (i) =>
            {
                using (ThreadStaticArrayPool<int, _32>.Get(out var arr))
                {
                    Assert.AreEqual(arr.Length, 32);
                }
            });

            while (!result.IsCompleted)
            {
                wait.SpinOnce();
            }
        }
    }
}
