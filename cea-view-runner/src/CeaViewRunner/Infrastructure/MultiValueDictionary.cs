using System.Collections.Generic;
using System.Linq;

namespace CeaViewRunner.Infrastructure;

/// <summary>
/// Minimal multi-map used to track HWNDs per window tag (same behavior as Cea.Data.MultiValueDictionary for ViewRunner).
/// </summary>
internal sealed class MultiValueDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _map = new();

    public void Add(TKey key, TValue value)
    {
        if (!_map.TryGetValue(key, out var list))
        {
            list = new List<TValue>();
            _map[key] = list;
        }

        list.Add(value);
    }

    public IEnumerable<TValue> GetValues(TKey key) =>
        _map.TryGetValue(key, out var list) ? list : System.Linq.Enumerable.Empty<TValue>();

    public void RemoveKey(TKey key) => _map.Remove(key);
}
