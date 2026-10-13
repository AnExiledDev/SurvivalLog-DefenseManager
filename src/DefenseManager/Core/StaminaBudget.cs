using System;
using System.Linq;

namespace DefenseManager.Core;

/// <summary>
/// The stamina one press may still promise to repairs. The game checks a repair's stamina when it starts and spends
/// it then, and the character's queue runs repairs one after another, so a press checking every defense against the
/// same stamina would line up repairs the game refuses later. Repairs already queued have their share set aside first.
/// </summary>
public sealed class StaminaBudget
{
    private readonly int _perRepair;
    private float _left;

    private StaminaBudget(float left, int perRepair)
    {
        _left = left;
        _perRepair = perRepair;
    }

    /// <summary>Stamina the game did not report, or a repair that needs none, caps nothing: the game's own check still runs.</summary>
    public static StaminaBudget Of(Survey survey)
    {
        if (survey.Stamina is not { PerRepair: > 0 } stamina) return new StaminaBudget(float.PositiveInfinity, 0);

        var budget = new StaminaBudget(stamina.Have, stamina.PerRepair);
        var queued = survey.Defenses.Count(defense => defense.IsRepairQueued);

        for (var i = 0; i < queued; i++) budget.Reserve();

        return budget;
    }

    public int PerRepair => _perRepair;

    public bool CoversOne => _left >= _perRepair;

    public void Reserve() => _left = Math.Max(0, _left - _perRepair);
}
