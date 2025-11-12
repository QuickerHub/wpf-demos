using System;
using System.Collections.Generic;

namespace Cea.Utils.Extension;

public static class EnumerableExt
{
    /// <summary>
    /// Merge into string, which is string.Join
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">Enumerable type</param>
    /// <param name="splitter">Default line break</param>
    /// <returns></returns>
    public static string JoinToString<T>(this IEnumerable<T> list, string splitter = "\r\n")
    {
        return string.Join(splitter, list);
    }
}

