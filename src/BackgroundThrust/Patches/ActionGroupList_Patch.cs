using System.Collections;
using HarmonyLib;

namespace BackgroundThrust.Patches;

internal static class ActionGroupList_ToggleGroup_Patch
{
    static readonly int SASIndex = BaseAction.GetGroupIndex(KSPActionGroup.SAS);

    static void Prefix(ActionGroupList __instance, KSPActionGroup group, ref bool __state)
    {
        if (group != KSPActionGroup.SAS)
            return;

        __state = __instance.groups[SASIndex];
    }

    static void Postfix(ActionGroupList __instance, KSPActionGroup group, bool __state)
    {
        if (group != KSPActionGroup.SAS)
            return;

        var active = __instance.groups[SASIndex];
        if (active == __state)
            return;

        var module = __instance.v?.GetBackgroundThrust();
        if (module is null)
            return;

        module.OnSasToggled(active);
    }
}
