using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Cea.Utils
{
    public static partial class TextUtil
    {
        /// <summary>
        /// Get HTML document node for later extraction
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static HtmlNode GetHtmlDocNode(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode;
        }

        /// <summary>
        /// Extract first URL from text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string? ExtractFirstUrl(string text)
        {
            string pattern = @"https?://[^\s/$.?#].[^\s]*"; // Regex pattern

            Match match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate similarity between two strings
        /// </summary>
        /// <param name="s1">First string</param>
        /// <param name="s2">Second string</param>
        /// <returns>String similarity, value between 0 and 1</returns>
        public static double Sim(this string s1, string s2)
        {
            double maxlen = Math.Max(s1.Length, s2.Length);
            return maxlen != 0 ? 1 - (Levenshtein(s1, s2) / maxlen) : 1;
        }

        /// <summary>
        /// Calculate Levenshtein edit distance between strings s1 and s2
        /// </summary>
        /// <param name="s1">First string</param>
        /// <param name="s2">Second string</param>
        /// <returns>Levenshtein edit distance between s1 and s2</returns>
        public static int Levenshtein(string s1, string s2)
        {
            int l1 = s1.Length;
            int l2 = s2.Length;
            if (l1 == 0) return l1 == 0 ? 1 : 0;

            int[] c = new int[l2 + 1];
            int[] p = new int[l2 + 1];

            for (int a = 0; a <= l2; a++)
            {
                p[a] = a;
            }

            for (int i = 1; i <= l1; i++)
            {
                c[0] = i;
                for (int j = 1; j <= l2; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    c[j] = Math.Min(Math.Min(p[j - 1] + cost, c[j - 1] + 1), p[j] + 1);
                }
                Array.Copy(c, p, l2 + 1);
            }
            return c[l2];
        }
    }
}

