using System;
using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>
/// What a cached table row was read for: the game's config tables (by their address), the language names are shown in,
/// and the web icons pictures are found among. Any of them changing makes every cached row stale.
/// </summary>
public readonly record struct TableKey(long Config, bool IsChinese, IconIndex Icons);

/// <summary>
/// Rows worked out from the game's tables (a name, an icon, an action id), kept by table id for as long as the key
/// they were read under holds. The panel reads every second and the power pass twice a second, and table rows never
/// change within one key, so each row is read from the game once. A row may be null, for an id that has nothing to
/// show, and that is kept too.
/// </summary>
public sealed class RowCache<TKey, TRow> where TKey : struct, IEquatable<TKey> where TRow : class
{
    private readonly Dictionary<int, TRow?> _rows = new();
    private TKey _key;
    private bool _hasKey;

    /// <summary>Drops every row when the key is not the one they were read under; true when it did.</summary>
    public bool Follow(TKey key)
    {
        if (_hasKey && _key.Equals(key)) return false;

        _rows.Clear();
        _key = key;
        _hasKey = true;

        return true;
    }

    public bool TryGet(int id, out TRow? row) => _rows.TryGetValue(id, out row);

    /// <summary>Keeps the row and hands it back.</summary>
    public TRow? Add(int id, TRow? row)
    {
        _rows[id] = row;

        return row;
    }
}
