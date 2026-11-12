using System;
using System.Collections.Generic;
using System.Linq;
using DefenseManager.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace DefenseManager.Adapters;

/// <summary>
/// Reads the home yard's defense grid. The squares are the map slots of type 10 (row in TowerRow), each standing at the
/// point of the same name in the current map's BuildingNodeMap. Every character has a home of their own (the Wage
/// Slave's squares are HomeBuildingTowerPos, the College Student's NeighborGirlTowerPos, the Warehouse Manager's
/// WarehouseTowerPos), all in the one slot table, and the map holds points only for the home being played, so a
/// square with a point is one of this home's. A square is the
/// player's when the game's TowerDefenseTagConstants.IsTowerSlotUnlocked says so; one with no unlock tag always
/// is. Its price is the TowerRingCost global setting for the ring its tag names, and the Defense level it needs is
/// GameKey.TowerSlotRequiredDefenseLevelOf. Doors are the slots of type 8 and windows of type 9; only those in the wall
/// along the yard are kept. Squares and points never change during a game, so they are read
/// once. Who owns which square is read each time, but only for squares not yet owned: a square once bought stays
/// bought for the rest of that load, so it is asked about again only after a load. While no square changes hands the
/// same grid is handed back.
/// </summary>
internal sealed class GameYard
{
    private const int SquareType = 10;
    private const int DoorType = 8;
    private const int WindowType = 9;

    private readonly Action<string> _log;
    private List<(string Name, int Row, double X, double Z, string Tag, int Price, int Level)>? _squares;
    private IReadOnlyList<WallFeature> _wall = Array.Empty<WallFeature>();
    private bool _reportedFailure;

    private bool[] _owned = Array.Empty<bool>();
    private GameLoad? _ownedLoad;
    private Yard? _yard;

    public GameYard(Action<string> log)
    {
        _log = log;
    }

    /// <summary>The grid with who owns each square, or null away from home or when it cannot be read.</summary>
    public Yard? Read(BattleLogicWorld world, ConfigManager config, GameLoad load)
    {
        try
        {
            var squares = _squares ??= ReadSquares(config);

            if (squares == null || squares.Count == 0)
            {
                _squares = null;
                _owned = Array.Empty<bool>();

                return null;
            }

            if (_ownedLoad != load || _owned.Length != squares.Count)
            {
                _owned = new bool[squares.Count];
                _ownedLoad = load;
                _yard = null;
            }

            var tags = world._TagManager;
            var isChanged = false;

            for (var i = 0; i < squares.Count; i++)
            {
                if (_owned[i]) continue;

                var square = squares[i];

                if (square.Tag.Length != 0 && (tags == null || !TowerDefenseTagConstants.IsTowerSlotUnlocked(tags, square.Name, square.Tag))) continue;

                _owned[i] = true;
                isChanged = true;
            }

            if (_yard != null && !isChanged) return _yard;

            var owned = new YardSquare[squares.Count];

            for (var i = 0; i < squares.Count; i++)
            {
                var square = squares[i];
                owned[i] = new YardSquare(square.Name, square.Row, square.X, square.Z, _owned[i], square.Price, square.Level);
            }

            return _yard = new Yard(owned, _wall);
        }
        catch (Exception ex)
        {
            if (!_reportedFailure) _log("reading the yard grid failed (reported once): " + ex);

            _reportedFailure = true;

            return null;
        }
    }

    /// <summary>Null when the current map has no tower points, as on an expedition, so it is read again at home.</summary>
    private List<(string, int, double, double, string, int, int)>? ReadSquares(ConfigManager config)
    {
        var points = config.customCache?.MapConfig?.BuildingNodeMap;
        var slots = config._Config_MapSlot_Dict;

        if (points == null || slots == null) return null;

        var squares = new List<(string, int, double, double, string, int, int)>();
        var openings = new List<WallOpening>();

        foreach (var slot in slots.Values)
        {
            if (slot?.Name == null || !points.TryGetValue(slot.Name, out var point)) continue;

            if (slot.Type == DoorType) openings.Add(new WallOpening(WallFeatureKind.Door, point.x, point.z));
            if (slot.Type == WindowType) openings.Add(new WallOpening(WallFeatureKind.Window, point.x, point.z));
            if (slot.Type != SquareType) continue;

            var tag = slot.UnlockTag ?? "";

            squares.Add((slot.Name, slot.TowerRow, point.x, point.z, tag, tag.Length == 0 ? 0 : Price(config, tag), GameKey.TowerSlotRequiredDefenseLevelOf(slot.Name)));
        }

        if (squares.Count == 0) return null;

        var placed = squares.Select(square => new YardSquare(square.Item1, square.Item2, square.Item3, square.Item4, false, 0)).ToList();

        _wall = Yard.WallAlongTheYard(placed, openings);
        _log($"yard grid: {squares.Count} squares in {squares.Select(square => square.Item2).Distinct().Count()} rows, {_wall.Count} doors and windows along it");

        return squares;
    }

    private static int Price(ConfigManager config, string tag)
    {
        var ring = TowerDefenseTagConstants.ParseRingFromTag(tag);

        return config.TryGet_Config_GlobalSetting("TowerRingCost" + ring)?.Params1 ?? 0;
    }
}
