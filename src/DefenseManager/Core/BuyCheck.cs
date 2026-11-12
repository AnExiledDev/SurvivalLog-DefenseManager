namespace DefenseManager.Core;

/// <summary>Whether the player may buy a yard square, and if not, the one reason the panel shows.</summary>
public abstract record BuyVerdict
{
    private BuyVerdict()
    {
    }

    public sealed record Allowed : BuyVerdict;

    /// <summary>The square is the player's already.</summary>
    public sealed record Owned : BuyVerdict;

    /// <summary>A zombie wave has the defenses locked.</summary>
    public sealed record WaveLocked : BuyVerdict;

    /// <summary>The Survival Points or the Defense level could not be read, so nothing is promised.</summary>
    public sealed record Unreadable : BuyVerdict;

    /// <summary>The game still hides the square: its ring needs a Defense level two or more above the player's.</summary>
    public sealed record Hidden(int Level) : BuyVerdict;

    /// <summary>The game shows the square but sells it only at this Defense level.</summary>
    public sealed record NeedsLevel(int Level) : BuyVerdict;

    /// <summary>The square costs more Survival Points than the player has.</summary>
    public sealed record Short(int Price, int Have) : BuyVerdict;
}

/// <summary>
/// The checks the game's purchase makes, in the order it makes them, read ahead so the panel can say why a square
/// can't be bought before the player presses anything. The game shows a square one Defense level early, with the
/// level it unlocks at, and hides those further out.
/// </summary>
public static class BuyCheck
{
    public static BuyVerdict Judge(Survey survey, YardSquare square)
    {
        if (square.IsOwned) return new BuyVerdict.Owned();
        if (survey.IsWaveLocked) return new BuyVerdict.WaveLocked();
        if (survey.SurvivalPoints is not { } points || survey.DefenseLevel is not { } level) return new BuyVerdict.Unreadable();
        if (square.RequiredLevel >= level + 2) return new BuyVerdict.Hidden(square.RequiredLevel);
        if (square.RequiredLevel > level) return new BuyVerdict.NeedsLevel(square.RequiredLevel);
        if (points < square.Price) return new BuyVerdict.Short(square.Price, points);

        return new BuyVerdict.Allowed();
    }
}
