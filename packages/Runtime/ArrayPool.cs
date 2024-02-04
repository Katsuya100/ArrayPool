using Katuusagi.GenericEnhance;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Katuusagi.Pool
{
    public static class ArrayPool<T, TSize>
        where TSize : struct, ITypeFormula<int>
    {
        public readonly ref struct GetHandler
        {
            private readonly T[] _array;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GetHandler(T[] array)
            {
                _array = array;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                TryReturn(_array);
            }
        }

        private static readonly int Size = TypeFormula.GetValue<TSize, int>();
        private static Stack<T[]> _stack;

        static ArrayPool()
        {
            _stack = new Stack<T[]>();
            MakeCache(32);
        }

        public static void MakeCache(int minCount)
        {
            for (int i = 0; i < minCount - _stack.Count; ++i)
            {
                _stack.Push(new T[Size]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GetHandler Get(out T[] result)
        {
            result = Get();
            return new GetHandler(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Get()
        {
            if (!_stack.TryPop(out var result))
            {
                result = new T[Size];
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReturn(T[] value)
        {
            if (value.Length != Size)
            {
                return false;
            }

            _stack.Push(value);
            return true;
        }
    }
}
