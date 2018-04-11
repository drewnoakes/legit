using System;
using JetBrains.Annotations;

namespace Legit
{
    internal static class ArrayUtil
    {
        public static T[] Slice<T>(T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        [CanBeNull]
        public static byte[] GetNullTerminatedBytes([NotNull] byte[] bytes, int index)
        {
            var zeroIndex = Array.IndexOf(bytes, 0, index);
            if (zeroIndex == -1)
                return null;
            return Slice(bytes, index, zeroIndex - index - 1);
        }
    }
}