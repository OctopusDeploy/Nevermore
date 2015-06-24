using System;
using System.Text;

namespace Nevermore
{
    /// <summary>
    /// This class makes it easy to build nested and/or conditions as SQL statements.
    /// </summary>
    public class ConditionBuilder
    {
        const string AndString = " AND ";
        const string OrString = " OR ";
        readonly StringBuilder buffer = new StringBuilder(128);
        uint groupOperators;
        uint groupUsage;
        int count;
        int lastGoodClose;

        public void PushAnd()
        {
            OpenGroup(true);
        }

        public void PushOr()
        {
            OpenGroup(false);
        }

        void OpenGroup(bool value)
        {
            count++;
            Join();

            BitStack.Push(ref groupOperators);
            BitStack.SetCurrent(ref groupOperators, value);
            BitStack.Push(ref groupUsage);

            buffer.Append('(');
        }

        void Join()
        {
            if (count > 0 && BitStack.Peek(ref groupUsage))
            {
                buffer.Append(BitStack.Peek(ref groupOperators) ? AndString : OrString);
            }
        }

        public void Condition(string condition, params string[] others)
        {
            Join();

            BitStack.SetCurrent(ref groupUsage, true);

            buffer.Append(condition);
            if (others != null)
            {
                foreach (var other in others)
                {
                    buffer.Append(other);
                }
            }

            lastGoodClose = buffer.Length;
        }

        public void Pop()
        {
            count--;
            if (BitStack.Pop(ref groupUsage))
            {
                buffer.Append(')');
                lastGoodClose = buffer.Length;

                if (count > 0)
                {
                    BitStack.SetCurrent(ref groupUsage, true);
                }
            }
            else
            {
                buffer.Length = lastGoodClose;
            }

            BitStack.Pop(ref groupOperators);
        }

        public override string ToString()
        {
            if (count > 0)
                throw new InvalidOperationException("The expression has not been closed correctly: " + buffer);

            return buffer.ToString();
        }

        static class BitStack
        {
            public static bool Pop(ref uint stack)
            {
                var v = Peek(ref stack);
                stack = (stack >> 1);
                return v;
            }

            public static void Push(ref uint stack)
            {
                stack = (stack << 1);
            }

            public static void SetCurrent(ref uint stack, bool value)
            {
                if (value)
                {
                    if (stack%2 != 0) return;
                    stack++;
                }
                else
                {
                    if (stack%2 != 1) return;
                    stack--;
                }
            }

            public static bool Peek(ref uint stack)
            {
                return stack%2 == 1;
            }
        }
    }
}