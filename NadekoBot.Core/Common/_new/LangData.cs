using System.Collections.Generic;

namespace Nadeko.Common.Localization
{
    public class LangData<T>
    {
        public IReadOnlyDictionary<string, T> Strings { get; }
        public string Locale { get; }

        public LangData(string locale, IReadOnlyDictionary<string, T> strings)
        {
            Strings = strings;
            Locale = locale;
        }
    }
}
