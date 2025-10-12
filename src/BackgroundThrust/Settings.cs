using KSP.Localization;

namespace BackgroundThrust;

public class Settings : GameParameters.CustomParameterNode
{
    static readonly string loc_Title = Localizer.Format("#LOC_BT_Settings_Title");
    static readonly string loc_Section = Localizer.Format("#LOC_BT_Settings_Section");

    public override string Title => loc_Title;
    public override string Section => "BackgroundThrust";
    public override string DisplaySection => Section;

    public override int SectionOrder => 1;
    public override bool HasPresets => false;
    public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

    [GameParameters.CustomFloatParameterUI(
        "#LOC_BT_Settings_RotationThreshold",
        toolTip = "#LOC_BT_Settings_RotationThreshold_ToolTip"
    )]
    public float RotationThreshold = 100.0f;
}
