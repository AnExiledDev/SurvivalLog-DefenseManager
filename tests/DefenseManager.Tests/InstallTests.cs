using DefenseManager.Core;
using Xunit;
using static DefenseManager.Tests.FakeGame;

namespace DefenseManager.Tests;

/// <summary>Installing a packed defense from the panel on an empty square of the yard.</summary>
public class InstallTests
{
    private const int Tesla = 36001;

    /// <summary>One row of three squares; the last is not the player's yet.</summary>
    private static Yard ThreeSquares() => new(
        new[] { new YardSquare("s1", 1, -1.4, 0, true, 0), new YardSquare("s2", 1, -0.7, 0, true, 0), new YardSquare("s3", 1, 0, 0, false, 50) },
        Array.Empty<WallFeature>());

    private static DefensePackage Pack(long[]? yard = null, int banked = 0) => new(Tesla, "Tesla coil", null, yard ?? Array.Empty<long>(), banked);

    private static FakeGame Game(DefensePackage? package = null)
    {
        var game = new FakeGame { Yard = ThreeSquares() };

        if (package is not null) game.Packages.Add(package);

        return game;
    }

    [Fact]
    public void A_package_lying_in_the_yard_is_installed_before_one_is_taken_out_of_the_bank()
    {
        var game = Game(Pack(yard: new long[] { 71, 72 }, banked: 3));

        Assert.Equal(new DefenseOutcome.Installing("Tesla coil", FromBank: false), DefenseRun.Install(game, "s2", Tesla));
        Assert.Equal(new[] { (71L, "s2") }, game.Installed);
        Assert.Empty(game.TakenOut);
    }

    [Fact]
    public void With_none_in_the_yard_one_is_taken_out_of_the_bank_and_that_package_is_installed()
    {
        var game = Game(Pack(banked: 1));
        game.BankAnswer = 555;

        Assert.Equal(new DefenseOutcome.Installing("Tesla coil", FromBank: true), DefenseRun.Install(game, "s1", Tesla));
        Assert.Equal(new[] { Tesla }, game.TakenOut);
        Assert.Equal(new[] { (555L, "s1") }, game.Installed);
    }

    [Fact]
    public void A_bank_with_no_free_yard_spot_to_put_it_on_installs_nothing()
    {
        var game = Game(Pack(banked: 1));
        game.BankAnswer = BankTakeOut.NoFreeSpot;

        Assert.Equal(new DefenseOutcome.NoFreeSpot("Tesla coil"), DefenseRun.Install(game, "s1", Tesla));
        Assert.Empty(game.Installed);
    }

    [Fact]
    public void A_take_out_the_bank_fails_installs_nothing()
    {
        var game = Game(Pack(banked: 1));
        game.BankAnswer = BankTakeOut.Failed;

        Assert.Equal(new DefenseOutcome.BankFailed("Tesla coil"), DefenseRun.Install(game, "s1", Tesla));
        Assert.Empty(game.Installed);
    }

    [Fact]
    public void An_install_the_game_turns_down_says_so_and_whether_the_package_came_out_of_the_bank()
    {
        var fromYard = Game(Pack(yard: new long[] { 71 }));
        var fromBank = Game(Pack(banked: 1));
        fromYard.RefusesInstall = fromBank.RefusesInstall = true;

        Assert.Equal(new DefenseOutcome.NotInstalled("Tesla coil", FromBank: false), DefenseRun.Install(fromYard, "s1", Tesla));
        Assert.Equal(new DefenseOutcome.NotInstalled("Tesla coil", FromBank: true), DefenseRun.Install(fromBank, "s1", Tesla));
    }

    [Fact]
    public void A_square_with_a_defense_on_it_is_not_installed_on()
    {
        var game = Game(Pack(yard: new long[] { 71 }));
        game.Defenses.Add(Def(1, x: -0.68, z: 0.02));

        Assert.Equal(new DefenseOutcome.SquareTaken(), DefenseRun.Install(game, "s2", Tesla));
        Assert.Empty(game.Installed);
    }

    [Fact]
    public void A_defense_on_the_next_square_leaves_this_one_free()
    {
        var game = Game(Pack(yard: new long[] { 71 }));
        game.Defenses.Add(Def(1, x: -1.4, z: 0));

        Assert.IsType<DefenseOutcome.Installing>(DefenseRun.Install(game, "s2", Tesla));
    }

    [Theory]
    [InlineData("s3")]
    [InlineData("nowhere")]
    public void A_square_not_yet_owned_or_not_in_the_yard_is_not_installed_on(string square)
    {
        var game = Game(Pack(yard: new long[] { 71 }));

        Assert.Equal(new DefenseOutcome.SquareTaken(), DefenseRun.Install(game, square, Tesla));
        Assert.Empty(game.Installed);
    }

    [Fact]
    public void A_defense_with_no_package_anywhere_is_not_installed()
    {
        var other = Game(new DefensePackage(Tesla + 1, "Spikes", null, new long[] { 71 }, 0));
        var used = Game(Pack());

        Assert.Equal(new DefenseOutcome.NoPackage(), DefenseRun.Install(other, "s1", Tesla));
        Assert.Equal(new DefenseOutcome.NoPackage(), DefenseRun.Install(used, "s1", Tesla));
        Assert.Empty(other.Installed);
        Assert.Empty(used.TakenOut);
    }

    [Fact]
    public void An_install_already_going_holds_back_the_next_one_before_the_bank_is_touched()
    {
        var game = Game(Pack(banked: 1));
        game.IsInstalling = true;

        Assert.Equal(new DefenseOutcome.InstallBusy(), DefenseRun.Install(game, "s1", Tesla));
        Assert.Empty(game.TakenOut);
        Assert.Empty(game.Installed);
    }

    [Fact]
    public void Nothing_is_installed_during_a_wave_away_from_home_or_outside_a_game()
    {
        var wave = Game(Pack(banked: 1));
        wave.IsWaveLocked = true;
        var away = Game(Pack(banked: 1));
        away.IsHome = false;
        var title = Game(Pack(banked: 1));
        title.IsNoGame = true;

        Assert.Equal(new DefenseOutcome.WaveLocked(), DefenseRun.Install(wave, "s1", Tesla));
        Assert.Equal(new DefenseOutcome.AwayFromHome(), DefenseRun.Install(away, "s1", Tesla));
        Assert.Equal(new DefenseOutcome.NoGame(), DefenseRun.Install(title, "s1", Tesla));
        Assert.Empty(wave.TakenOut.Concat(away.TakenOut).Concat(title.TakenOut));
    }

    [Fact]
    public void Install_messages_name_the_defense_and_say_when_it_came_out_of_the_bank()
    {
        Assert.Equal("Installing Tesla coil.", OutcomeMessage.Describe(new DefenseOutcome.Installing("Tesla coil", false)));
        Assert.Equal("Installing Tesla coil. Took it out of Package Bank, it is in the yard now.", OutcomeMessage.Describe(new DefenseOutcome.Installing("Tesla coil", true)));
        Assert.Equal("Tesla coil is in Package Bank, but every yard package spot is full.", OutcomeMessage.Describe(new DefenseOutcome.NoFreeSpot("Tesla coil")));
        Assert.Equal("The game would not install Tesla coil there.", OutcomeMessage.Describe(new DefenseOutcome.NotInstalled("Tesla coil", false)));
    }
}

public class PackageSpotsTests
{
    [Theory]
    [InlineData("HomePackage1", true)]
    [InlineData("HomePackage12", true)]
    [InlineData("HomePackage", false)]
    [InlineData("HomePackage_car", false)]
    [InlineData("HomePackagesuliaodai1", false)]
    [InlineData("homepackage1", false)]
    [InlineData(null, false)]
    public void Only_the_numbered_yard_spots_hold_packages_to_install(string? node, bool isSpot)
    {
        Assert.Equal(isSpot, PackageSpots.IsYardSpot(node));
    }

    [Fact]
    public void Packages_are_grouped_by_defense_with_yard_packages_in_order_and_the_bank_counted()
    {
        var packages = PackageSpots.Gather(
            new[] { (30, 7L), (20, 5L), (30, 9L) },
            new[] { 20, 40, 20 },
            configId => ($"Defense {configId}", null));

        Assert.Equal(new[] { 20, 30, 40 }, packages.Select(package => package.ConfigId));
        Assert.Equal(new long[] { 5 }, packages[0].YardBags);
        Assert.Equal(2, packages[0].Banked);
        Assert.Equal(new long[] { 7, 9 }, packages[1].YardBags);
        Assert.Equal(0, packages[1].Banked);
        Assert.Empty(packages[2].YardBags);
        Assert.Equal(1, packages[2].Count);
    }

    [Fact]
    public void Furniture_that_is_not_a_defense_is_left_out()
    {
        var packages = PackageSpots.Gather(new[] { (30, 7L), (99, 8L) }, new[] { 98 }, configId => configId == 30 ? ("Spikes", "icon.png") : null);

        Assert.Equal("Spikes", Assert.Single(packages).Name);
    }
}
