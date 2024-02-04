using NUnit.Framework;
using System.Buffers;
using System.Collections.Generic;
using Unity.PerformanceTesting;

namespace Katuusagi.Pool.Tests
{
    public class ArrayPoolPerformanceTest
    {
        private static int WarmupCount = 1;
        private static int IterationCount = 8000;
        private static int MeasurementCount = 20;
        private static int CacheCount = ((IterationCount + WarmupCount) * MeasurementCount) + 1;

        [SetUp]
        public void Init()
        {
            ArrayPool<int>.Create(32, CacheCount);
            ArrayPool<int, _32>.MakeCache(CacheCount);
            ThreadStaticArrayPool<int, _32>.MakeCache(CacheCount);
            ConcurrentArrayPool<int, _32>.MakeCache(CacheCount);
        }

        [Test]
        [Performance]
        public void Get_Legacy()
        {
            Stack<int[]> arrs = new Stack<int[]>(CacheCount);

            Measure.Method(() =>
            {
                arrs.Push(ArrayPool<int>.Shared.Rent(32));
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();

            foreach (var arr in arrs)
            {
                ArrayPool<int>.Shared.Return(arr);
            }
        }

        [Test]
        [Performance]
        public void Get_Fast()
        {
            Stack<int[]> arrs = new Stack<int[]>(CacheCount);

            Measure.Method(() =>
            {
                arrs.Push(ArrayPool<int, _32>.Get());
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();

            foreach (var arr in arrs)
            {
                ArrayPool<int, _32>.TryReturn(arr);
            }
        }

        [Test]
        [Performance]
        public void Get_ThreadStatic()
        {
            Stack<int[]> arrs = new Stack<int[]>(CacheCount);

            Measure.Method(() =>
            {
                arrs.Push(ThreadStaticArrayPool<int, _32>.Get());
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();

            foreach (var arr in arrs)
            {
                ThreadStaticArrayPool<int, _32>.TryReturn(arr);
            }
        }

        [Test]
        [Performance]
        public void Get_Concurrent()
        {
            Stack<int[]> arrs = new Stack<int[]>(CacheCount);

            Measure.Method(() =>
            {
                arrs.Push(ConcurrentArrayPool<int, _32>.Get());
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();

            foreach (var arr in arrs)
            {
                ConcurrentArrayPool<int, _32>.TryReturn(arr);
            }
        }

        [Test]
        [Performance]
        public void Return_Legacy()
        {
            Stack<int[]> arrs = new Stack<int[]>(10000 * 20 * 2);
            for (int i = 0; i < 10000 * 20 * 2; ++i)
            {
                arrs.Push(ArrayPool<int>.Shared.Rent(32));
            }

            Measure.Method(() =>
            {
                var arr = arrs.Pop();
                ArrayPool<int>.Shared.Return(arr);
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();
        }

        [Test]
        [Performance]
        public void Return_Fast()
        {
            Stack<int[]> arrs = new Stack<int[]>(10000 * 20 * 2);
            for (int i = 0; i < 10000 * 20 * 2; ++i)
            {
                arrs.Push(ArrayPool<int, _32>.Get());
            }

            Measure.Method(() =>
            {
                var arr = arrs.Pop();
                ArrayPool<int, _32>.TryReturn(arr);
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();
        }

        [Test]
        [Performance]
        public void Return_ThreadStatic()
        {
            Stack<int[]> arrs = new Stack<int[]>(10000 * 20 * 2);
            for (int i = 0; i < 10000 * 20 * 2; ++i)
            {
                arrs.Push(ThreadStaticArrayPool<int, _32>.Get());
            }

            Measure.Method(() =>
            {
                var arr = arrs.Pop();
                ThreadStaticArrayPool<int, _32>.TryReturn(arr);
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();
        }

        [Test]
        [Performance]
        public void Return_Concurrent()
        {
            Stack<int[]> arrs = new Stack<int[]>(10000 * 20 * 2);
            for (int i = 0; i < 10000 * 20 * 2; ++i)
            {
                arrs.Push(ConcurrentArrayPool<int, _32>.Get());
            }

            Measure.Method(() =>
            {
                var arr = arrs.Pop();
                ConcurrentArrayPool<int, _32>.TryReturn(arr);
            })
            .WarmupCount(WarmupCount)
            .IterationsPerMeasurement(IterationCount)
            .MeasurementCount(MeasurementCount)
            .Run();
        }
    }
}
