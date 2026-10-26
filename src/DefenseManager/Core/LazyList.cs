using System;
using System.Collections;
using System.Collections.Generic;

namespace DefenseManager.Core;

/// <summary>
/// A list read from the game only when something first looks at it, and then kept. The power pass hands the living
/// zombies over this way, so a pass whose modes never ask about zombies never reads them. A read that throws throws
/// from whatever first looks, and is tried again on the next look.
/// </summary>
public sealed class LazyList<T> : IReadOnlyList<T>
{
    private readonly Func<IReadOnlyList<T>> _read;
    private IReadOnlyList<T>? _items;

    public LazyList(Func<IReadOnlyList<T>> read)
    {
        _read = read;
    }

    public int Count => Items.Count;

    public T this[int index] => Items[index];

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IReadOnlyList<T> Items => _items ??= _read();
}
