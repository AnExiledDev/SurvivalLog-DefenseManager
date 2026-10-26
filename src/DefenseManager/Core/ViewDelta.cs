using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DefenseManager.Core;

/// <summary>
/// Trims a panel view to what the page does not have yet. A defense row the page was last sent unchanged goes as its id
/// alone (a string in place of the row's object), and page.js takes the row it already holds for that id; a new or
/// changed row goes whole. Everything else in the view goes as it is. The rows the page holds are only counted as
/// held once the send is known to have run (<see cref="Sent"/>), and are forgotten when the page reloads or asks for
/// everything again, so the next view goes whole.
/// </summary>
public sealed class ViewDelta
{
    private const string RowsStart = "\"defenses\":[";
    private const string IdStart = "{\"id\":\"";

    private Dictionary<long, string> _held = new();
    private Dictionary<long, string> _next = new();
    private readonly StringBuilder _text = new(4096);

    /// <summary>The view to send: <paramref name="view"/> with every row the page already holds cut down to its id.</summary>
    public string Next(string view)
    {
        _next.Clear();

        var start = view.IndexOf(RowsStart, StringComparison.Ordinal);

        if (start < 0) return view;

        var at = start + RowsStart.Length;
        var isTrimmed = false;

        _text.Clear();
        _text.Append(view, 0, at);

        while (at < view.Length && view[at] == '{')
        {
            var end = ObjectEnd(view, at);

            if (end < 0 || !TryId(view, at, out var id, out var idText) || _next.ContainsKey(id))
            {
                // Not a row this reads as expected, or a second row with the same id (the page could not tell which it
                // holds): the view goes whole and nothing is counted as held.
                _next.Clear();

                return view;
            }

            var length = end - at;

            if (_held.TryGetValue(id, out var held) && held.Length == length && string.CompareOrdinal(view, at, held, 0, length) == 0)
            {
                _next[id] = held;
                _text.Append('"').Append(view, idText.Start, idText.Length).Append('"');
                isTrimmed = true;
            }
            else
            {
                _next[id] = view.Substring(at, length);
                _text.Append(view, at, length);
            }

            at = end;

            if (at < view.Length && view[at] == ',')
            {
                _text.Append(',');
                at++;
            }
        }

        if (!isTrimmed) return view;

        _text.Append(view, at, view.Length - at);

        return _text.ToString();
    }

    /// <summary>The view from the last <see cref="Next"/> ran in the page, which now holds its rows.</summary>
    public void Sent()
    {
        (_held, _next) = (_next, _held);
        _next.Clear();
    }

    /// <summary>The page holds no rows: it reloaded, or asked for everything again.</summary>
    public void Forget()
    {
        _held.Clear();
        _next.Clear();
    }

    /// <summary>Where the JSON object starting at <paramref name="start"/> ends (one past its closing brace), or -1.</summary>
    private static int ObjectEnd(string json, int start)
    {
        var depth = 0;
        var isInString = false;

        for (var i = start; i < json.Length; i++)
        {
            var character = json[i];

            if (isInString)
            {
                if (character == '\\') i++;
                else if (character == '"') isInString = false;

                continue;
            }

            switch (character)
            {
                case '"':
                    isInString = true;
                    break;
                case '{' or '[':
                    depth++;
                    break;
                case '}' or ']':
                    depth--;

                    if (depth == 0) return i + 1;

                    break;
            }
        }

        return -1;
    }

    /// <summary>A row opens with its id as a string of digits (DefenseView): {"id":"123",...</summary>
    private static bool TryId(string json, int start, out long id, out (int Start, int Length) text)
    {
        id = 0;
        text = default;

        if (string.CompareOrdinal(json, start, IdStart, 0, IdStart.Length) != 0) return false;

        var first = start + IdStart.Length;
        var end = json.IndexOf('"', first);

        if (end <= first) return false;

        text = (first, end - first);

        return long.TryParse(json.AsSpan(first, end - first), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out id);
    }
}
