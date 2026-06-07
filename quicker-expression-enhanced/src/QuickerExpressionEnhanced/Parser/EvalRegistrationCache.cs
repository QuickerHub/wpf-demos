using System;
using System.Collections.Concurrent;
using Z.Expressions;

namespace QuickerExpressionEnhanced.Parser;

/// <summary>
/// Tracks assemblies/namespaces/types already registered on an EvalContext instance.
/// </summary>
internal sealed class EvalRegistrationCache
{
    private static readonly ConcurrentDictionary<int, EvalRegistrationCache> ByEval =
        new();

    private readonly ConcurrentDictionary<string, byte> _registered =
        new(StringComparer.Ordinal);

    public static EvalRegistrationCache For(EvalContext eval)
    {
        if (eval is null)
        {
            throw new ArgumentNullException(nameof(eval));
        }

        return ByEval.GetOrAdd(eval.GetHashCode(), _ => new EvalRegistrationCache());
    }

    public bool TryMark(string key)
    {
        return _registered.TryAdd(key, 0);
    }
}
