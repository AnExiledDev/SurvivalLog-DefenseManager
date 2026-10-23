using System;
using System.Collections.Generic;
using System.Linq;
using DefenseManager.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace DefenseManager.Adapters;

/// <summary>
/// Reads the home yard's defense grid. The squares are the map slots named HomeBuildingTowerPos (slot type 10, row in
/// TowerRow), each standing at the point of the same name in the current map's BuildingNodeMap. A square is the
/// player's when the game's own TowerDefenseTagConstants.IsTowerSlotUnlocked says so; one with no unlock tag always
/// is. Its price is the TowerRingCost global setting for the ring its tag names. The door and window are the house
/// slots HomeBuildingPos07 and HomeBuildingPos08. Squares and points never change during a game, so they are read
/// once; only who owns which square is read each time.
/// </summary>
internal sealed class GameYard
{
    private const string SquarePrefix = "HomeBuildingTowerPos";
    private const int SquareType = 10;
    private const string DoorSlot = "HomeBuildingPos07";
    private const string WindowSlot = "HomeBuildingPos08";

    private readonly Action<string> _log;
    private List<(string Name, int Row, double X, double Z, string Tag, int Price)>? _squares;
    private List<WallFeature> _wall = new();
    private bool _reportedFailure;

    public GameYard(Action<string> log)
    {
        _log = log;
    }

    /// <summary>The grid with who owns each square, or null away from home or when it cannot be read.</summary>
    public Yard? Read(BattleLogicWorld world, ConfigManager config)
    {
        try
        {
            var squares = _squares ??= ReadSquares(config);

            if (squares == null || squares.Count == 0)
            {
                _squares = null;

                return null;
            }

            var tags = world._TagManager;
            var owned = squares.Select(square => new YardSquare(
                square.Name,
                square.Row,
                square.X,
                square.Z,
                square.Tag.Length == 0 || (tags != null && TowerDefenseTagConstants.IsTowerSlotUnlocked(tags, square.Name, square.Tag)),
                square.Price)).ToList();

            return new Yard(owned, _wall);
        }
        catch (Exception ex)
        {
            if (!_reportedFailure) _log("reading the yard grid failed (reported once): " + ex);

            _reportedFailure = true;

            return null;
        }
    }

    /// <summary>Null when the current map has no tower points, as on an expedition, so it is read again at home.</summary>
    private List<(string, int, double, double, string, int)>? ReadSquares(ConfigManager config)
    {
        var points = config.customCache?.MapConfig?.BuildingNodeMap;
        var slots = config._Config_MapSlot_Dict;

        if (points == null || slots == null) return null;

        var squares = new List<(string, int, double, double, string, int)>();

        foreach (var slot in slots.Values)
        {
            if (slot == null || slot.Type != SquareType || slot.Name == null || !slot.Name.StartsWith(SquarePrefix, StringComparison.Ordinal)) continue;
            if (!points.TryGetValue(slot.Name, out var point)) continue;

            var tag = slot.UnlockTag ?? "";

            squares.Add((slot.Name, slot.TowerRow, point.x, point.z, tag, tag.Length == 0 ? 0 : Price(config, tag)));
        }

        if (squares.Count == 0) return null;

        var wall = new List<WallFeature>();

        if (points.TryGetValue(DoorSlot, out var door)) wall.Add(new WallFeature(WallFeatureKind.Door, door.x));
        if (points.TryGetValue(WindowSlot, out var window)) wall.Add(new WallFeature(WallFeatureKind.Window, window.x));

        _wall = wall;
        _log($"yard grid: {squares.Count} squares in {squares.Select(square => square.Item2).Distinct().Count()} rows");

        return squares;
    }

    private static int Price(ConfigManager config, string tag)
    {
        var ring = TowerDefenseTagConstants.ParseRingFromTag(tag);

        return config.TryGet_Config_GlobalSetting("TowerRingCost" + ring)?.Params1 ?? 0;
    }
}
