using System;

namespace Fody.Inspectors
{
    public class MethodStackDepth
    {
        private int _depth;

        public void Reset()
        {
            _depth = 0;
        }

        public void Increase(int count = 1)
        {
            _depth += count;
        }

        public void Decrease(int count = 1)
        {
            _depth -= count;
            if (_depth < 0) throw new InvalidOperationException("Method stack underflow.");
        }

        public static MethodStackDepth operator ++(MethodStackDepth stack)
        {
            stack.Increase();
            return stack;
        }

        public static MethodStackDepth operator +(MethodStackDepth stack, int count)
        {
            stack.Increase(count);
            return stack;
        }

        public static MethodStackDepth operator --(MethodStackDepth stack)
        {
            stack.Decrease();
            return stack;
        }

        public static MethodStackDepth operator -(MethodStackDepth stack, int count)
        {
            stack.Decrease(count);
            return stack;
        }

        public static implicit operator int(MethodStackDepth stack)
        {
            return stack._depth;
        }

        public static implicit operator MethodStackDepth(int depth)
        {
            return new MethodStackDepth { _depth = depth };
        }

        public IDisposable Stash() => new Holder(this);

        private class Holder(MethodStackDepth stack) : IDisposable
        {
            private readonly int _depth = stack._depth;

            public void Dispose()
            {
                stack._depth = _depth;
            }
        }
    }
}
