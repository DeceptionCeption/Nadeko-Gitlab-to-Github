using System;
using System.Collections.Generic;
using System.Linq;

namespace Nadeko.Bot.Common
{
    // todo 3.1 .parse and .tryparse
    // could also do implicit conversion to int/long
    // could also make it a readonly struct
    public class Kwok
    {
        private static readonly char[] kwokLettersAndDigits = Enumerable.Range(50, 8) // 2-9 = 8
            .Concat(Enumerable.Range(97, 11)) // a - k = 11 (+8 = 19)
            .Concat(Enumerable.Range(109, 2)) // l is skipped, m, n = 2 (+19 = 21)
            .Concat(Enumerable.Range(112, 11)) // o is skipped, p - z = 11 (+21 = 32)
            .Select(x => (char)x)
            .ToArray();

        private static readonly Dictionary<char, int> charToVal;
        private static readonly Dictionary<int, char> valToChar;

        static Kwok()
        {
            charToVal = new Dictionary<char, int>();
            valToChar = new Dictionary<int, char>();
            for (int i = 0; i < kwokLettersAndDigits.Length; i++)
            {
                charToVal.Add(kwokLettersAndDigits[i], i);
                valToChar.Add(i, kwokLettersAndDigits[i]);
            }
        }

        public static bool IsKwok(string input) => input.All(x => kwokLettersAndDigits.Contains(x));

        public static bool KwokToInt(string input, out int id)
        {
            input = input?.ToLowerInvariant();
            id = 0;
            var sign = 1;
            if (!string.IsNullOrWhiteSpace(input) && input.StartsWith('-'))
            {
                input = input.Substring(1);
                sign = -1;
            }

            if (string.IsNullOrWhiteSpace(input))
                return true;

            if (!IsKwok(input))
            {
                return false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                id <<= 5;
                id |= charToVal[input[i]];
            }

            id *= sign;
            return true;
        }

        public static string IntToKwok(int id)
        {
            string output = "";

            int sign = id < 0 ? -1 : 1;

            id *= sign;

            if (id == 0)
                return "2";

            while (id > 0)
            {
                var val = id % 32;
                output = valToChar[val] + output;
                id >>= 5;
            }

            return sign is -1 ? "-" + output : output;
        }
    }
}
