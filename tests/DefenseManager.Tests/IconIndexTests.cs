using DefenseManager.Core;
using Xunit;

namespace DefenseManager.Tests;

public class IconIndexTests
{
    private static readonly IconIndex Icons = IconIndex.Build(new[]
    {
        "Res/Furniture/UI_Item_Icon_Furnit_M_831300313.png",
        "Res\\Item\\UI_Item_Icon_Wood.png",
        "Res/Item/UI_Item_Icon_Part.png",
        "Res/Big/UI_Item_Icon_Part.png",
    });

    [Fact]
    public void A_defense_icon_named_by_the_tables_page_path_resolves_to_the_web_icon()
    {
        Assert.Equal("Res/Furniture/UI_Item_Icon_Furnit_M_831300313.png", Icons.Resolve("../../Res/Furniture/UI_Item_Icon_Furnit_M_831300313.png"));
    }

    [Fact]
    public void A_bare_icon_name_without_folder_or_extension_resolves()
    {
        Assert.Equal("Res/Item/UI_Item_Icon_Wood.png", Icons.Resolve("ui_item_icon_wood"));
    }

    [Fact]
    public void When_two_folders_hold_the_name_the_folder_the_table_names_wins()
    {
        Assert.Equal("Res/Big/UI_Item_Icon_Part.png", Icons.Resolve("../Res/Big/UI_Item_Icon_Part.png"));
        Assert.Equal("Res/Item/UI_Item_Icon_Part.png", Icons.Resolve("UI_Item_Icon_Part"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UI_Item_Icon_Missing.png")]
    public void An_icon_the_game_has_no_web_picture_for_resolves_to_nothing(string? named)
    {
        Assert.Null(Icons.Resolve(named));
        Assert.Null(IconIndex.Empty.Resolve("UI_Item_Icon_Wood"));
    }
}
