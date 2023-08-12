using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFCodeGenConverter.Utils
{
    public class RandomUtils
    {
        private static readonly Random RANDOM = new Random();

        public static void RandomString(Random random, char[] chars, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                chars[offset + i] = (char)random.Next('a', 'z');
        }

        public static string RandomStringWhere(string prefix, int count, Predicate<string> canAccept)
        {
            string str;
            int len = prefix.Length;
            char[] chars = new char[len + count];
            prefix.CopyTo(0, chars, 0, len);
            do
            {
                RandomString(RANDOM, chars, len, count);
                str = new string(chars);
            } while (!canAccept(str));

            return str;
        }
    }
}
