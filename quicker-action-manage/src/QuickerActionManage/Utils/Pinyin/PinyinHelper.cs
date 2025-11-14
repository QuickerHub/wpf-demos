using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QuickerActionManage.Utils
{
    public static class PinyinHelper
    {
        private const int MAX_WORDS = 30;

        public static string GetPinyinFirstCharString(string chnStr, bool onlyFirst = true)
        {
            StringBuilder stringBuilder = new StringBuilder(chnStr.Length * 2);
            for (int index = 0; index < chnStr.Length; ++index)
            {
                string pinyinFirstChar = PinyinConverter.GetPinyinFirstChar(chnStr[index]);
                if (onlyFirst && pinyinFirstChar.Length > 1)
                    stringBuilder.Append(pinyinFirstChar[0]);
                else
                    stringBuilder.Append(pinyinFirstChar);
            }
            return stringBuilder.ToString();
        }

        public static string GetPinYinMatchString(string chineseString)
        {
            StringBuilder stringBuilder = new StringBuilder(chineseString.Length * 4);
            bool flag1 = false;
            bool flag2 = false;
            for (int index = 0; index < chineseString.Length; ++index)
            {
                char ch = chineseString[index];
                if (PinyinConverter.IsValidCnChar(ch))
                {
                    if (stringBuilder.Length > 0 && !flag2)
                        stringBuilder.Append(' ');
                    string pinyin = PinyinConverter.GetPinyin(ch);
                    if (pinyin.Length > 0)
                    {
                        stringBuilder.Append(pinyin.Replace(' ', '|'));
                        stringBuilder.Append("|");
                    }
                    stringBuilder.Append(ch);
                    flag1 = true;
                    stringBuilder.Append(' ');
                }
                else
                {
                    if (flag1)
                    {
                        stringBuilder.Append(' ');
                        flag2 = true;
                    }
                    if (ch == ' ')
                    {
                        if (!flag2)
                        {
                            stringBuilder.Append(' ');
                            flag2 = true;
                        }
                    }
                    else
                    {
                        stringBuilder.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
                        flag2 = false;
                    }
                    flag1 = false;
                }
            }
            return stringBuilder.ToString().TrimEnd(' ');
        }

        public static bool IsPinyinMatch(
          string pinyin,
          string search,
          bool fromStart,
          bool onlyFirstChar)
        {
            if (search.Length < 1)
                return false;
            search = search.ToLowerInvariant();
            int[] wordStartPos = new int[30];
            int wordCount = 0;
            bool flag = true;
            for (int index = 0; index < pinyin.Length && wordCount < 30; ++index)
            {
                if (flag && pinyin[index] != ' ')
                    wordStartPos[wordCount++] = index;
                flag = pinyin[index] == ' ';
            }
            return onlyFirstChar ? fromStart ? IsFirstCharMatchFromBegin(pinyin, search, wordStartPos, wordCount) : IsFirstCharMatchAnyPart(pinyin, search, wordStartPos, wordCount) : fromStart ? IsMultiCharMatchFromBegin(pinyin, search, wordStartPos, wordCount) : IsMultiCharMatchAnyPart(pinyin, search, wordStartPos, wordCount);
        }

        public static bool Search(string search, params string[] textList) => SearchList(search, textList);

        public static bool SearchList(string search, IEnumerable<string> textList)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            if (textList == null)
                return false;

            var keys = search.Replace("'", "").Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            return textList.Where(x => !string.IsNullOrWhiteSpace(x))
                .Any(x => keys.All(k => x.IsPinyinMatchOrContains(k)));
        }

        private static bool IsPinyinMatchOrContains(this string text, string search)
        {
            return text.IndexOf(search, 0, StringComparison.OrdinalIgnoreCase) != -1
                || IsPinyinMatch(GetPinYinMatchString(text), search, false, false);
        }

        public static bool IsPinyinMatch(this string text, string search)
        {
            if (text == null || search == null) return false;
            return IsPinyinMatch(GetPinYinMatchString(text), search, false, false);
        }

        private static bool IsFirstCharMatchAnyPart(
          string pinyin,
          string search,
          int[] wordStartPos,
          int wordCount)
        {
            if (wordCount < search.Length)
                return false;
            int index1 = 0;
            int index2 = 0;
            int num = -1;
            while (index1 < wordCount && index2 < search.Length)
            {
                if (IsCharMatchWord(pinyin, wordStartPos[index1], search[index2]))
                {
                    if (num < 0)
                        num = index1;
                    ++index2;
                    ++index1;
                }
                else if (index2 == 0)
                {
                    ++index1;
                }
                else
                {
                    index2 = 0;
                    index1 = num + 1;
                    num = -1;
                }
            }
            return index2 == search.Length;
        }

        private static bool IsFirstCharMatchFromBegin(
          string pinyin,
          string search,
          int[] wordStartPos,
          int wordCount)
        {
            if (wordCount < search.Length)
                return false;
            for (int index = 0; index < search.Length; ++index)
            {
                if (!IsCharMatchWord(pinyin, wordStartPos[index], search[index]))
                    return false;
            }
            return true;
        }

        private static bool IsCharMatchWord(string pinyin, int startPos, char charToMatch)
        {
            bool flag = true;
            for (int index = startPos; index < pinyin.Length && pinyin[index] != ' '; ++index)
            {
                if (flag && pinyin[index] == charToMatch)
                    return true;
                flag = pinyin[index] == '|';
            }
            return false;
        }

        private static bool IsMultiCharMatchAnyPart(
          string pinyin,
          string search,
          int[] wordStartPos,
          int wordCount)
        {
            int searchStartPos = 0;
            int index1 = 0;
            int num1 = 0;
            int num2 = -1;
            while (index1 < wordCount && searchStartPos < search.Length)
            {
                int matchCharCount1 = GetMatchCharCount(pinyin, search, wordStartPos[index1], searchStartPos);
                if (matchCharCount1 > 0)
                {
                    num1 = matchCharCount1;
                    searchStartPos += matchCharCount1;
                    if (num2 < 0)
                        num2 = index1;
                    ++index1;
                }
                else if (searchStartPos == 0)
                {
                    ++index1;
                }
                else
                {
                    bool flag = false;
                    for (int index2 = 1; index2 < num1 - 1; ++index2)
                    {
                        int matchCharCount2 = GetMatchCharCount(pinyin, search, wordStartPos[index1], searchStartPos - index2);
                        if (matchCharCount2 > 0)
                        {
                            num1 = matchCharCount2;
                            searchStartPos = searchStartPos - index2 + matchCharCount2;
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        ++index1;
                    }
                    else
                    {
                        index1 = num2 + 1;
                        num2 = -1;
                        num1 = 0;
                        searchStartPos = 0;
                    }
                }
            }
            return searchStartPos == search.Length;
        }

        private static bool IsMultiCharMatchFromBegin(
          string pinyin,
          string search,
          int[] wordStartPos,
          int wordCount)
        {
            int searchStartPos = 0;
            int index1 = 0;
            int num1 = 0;
            int num2 = -1;
            while (index1 < wordCount && searchStartPos < search.Length)
            {
                int matchCharCount1 = GetMatchCharCount(pinyin, search, wordStartPos[index1], searchStartPos);
                if (matchCharCount1 > 0)
                {
                    num1 = matchCharCount1;
                    searchStartPos += matchCharCount1;
                    if (num2 < 0)
                        num2 = index1;
                    ++index1;
                }
                else
                {
                    if (searchStartPos == 0)
                    {
                        int num3 = index1 + 1;
                        return false;
                    }
                    bool flag = false;
                    for (int index2 = 1; index2 < num1 - 1; ++index2)
                    {
                        int matchCharCount2 = GetMatchCharCount(pinyin, search, wordStartPos[index1], searchStartPos - index2);
                        if (matchCharCount2 > 0)
                        {
                            num1 = matchCharCount2;
                            searchStartPos = searchStartPos - index2 + matchCharCount2;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        return false;
                    ++index1;
                }
            }
            return searchStartPos == search.Length;
        }

        private static int GetMatchCharCount(
          string pinyin,
          string search,
          int pinyinStartPos,
          int searchStartPos)
        {
            int num1 = 0;
            int index1 = pinyinStartPos;
            int index2 = searchStartPos;
            int num2 = 0;
            bool flag = true;
            for (; index1 < pinyin.Length && pinyin[index1] != ' ' && index2 < search.Length; ++index1)
            {
                if (pinyin[index1] == '|')
                {
                    if (num2 > num1)
                    {
                        num1 = num2;
                        num2 = 0;
                    }
                    flag = true;
                    index2 = searchStartPos;
                }
                else if (flag)
                {
                    if (pinyin[index1] == search[index2])
                    {
                        ++index2;
                        ++num2;
                    }
                    else
                        flag = false;
                }
            }
            if (num2 > num1)
                num1 = num2;
            return num1;
        }

        private static int GetNextWordStartPos(string pinyin, int pos)
        {
            int index = pos;
            bool flag = false;
            for (; index < pinyin.Length; ++index)
            {
                if (flag && pinyin[index] != ' ')
                    return index;
                if (pinyin[index] == ' ')
                    flag = true;
            }
            return -1;
        }
    }
}