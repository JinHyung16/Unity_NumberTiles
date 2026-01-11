using System.Runtime.CompilerServices;

namespace NTGame
{
    public static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCode(int h1, int h2)
        {
            unchecked
            {
                // (h1 * prime) + h2
                return (h1 * 397) ^ h2;
            }
        }
    }
}

