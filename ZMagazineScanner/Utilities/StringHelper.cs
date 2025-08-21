using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZMangaScanner.Utilities
{
    public static class StringHelper
    {
        public static string PadZeroes(string input, int totalDigitCount)
        {
            int numberOfZeroes = (totalDigitCount - input.Length);

            StringBuilder zeroesBuilder = new StringBuilder();

            for (int i = 0; i < numberOfZeroes; i++)
            {
                zeroesBuilder.Append('0');
            }

            return zeroesBuilder.ToString() + input;
        }

        public static string SetUrlToSpecificIDs(string url, int magazineId = 1, string secretId = "", int issueId = 0)
        {
            var updatedURL = url.Replace("{magazineId}", magazineId.ToString());
            updatedURL = updatedURL.Replace("{secretId}", secretId);
            updatedURL = updatedURL.Replace("{issueId}", issueId.ToString());

            return updatedURL;
        }

        public static string ReplaceLastOccurrence(string source, string find, string replace)
        {
            int place = source.LastIndexOf(find);

            if (place == -1)
                return source;

            return source.Remove(place, find.Length).Insert(place, replace);
        }

    }
}
