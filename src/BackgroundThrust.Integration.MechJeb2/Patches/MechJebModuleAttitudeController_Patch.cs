using HarmonyLib;
using MuMech;

namespace BackgroundThrust.Integration.MechJeb2.Patches;

[HarmonyPatch(
    typeof(MechJebModuleAttitudeController),
    nameof(MechJebModuleAttitudeController.OnModuleEnabled)
)]
internal static class MechJebModuleAttitudeController_OnModuleEnabled_Patch
{
    static void Postfix(MechJebModuleAttitudeController __instance)
    {
        EventDispatcher.OnAttitudeControllerEnabled(__instance);
    }
}

[HarmonyPatch(
    typeof(MechJebModuleAttitudeController),
    nameof(MechJebModuleAttitudeController.OnModuleDisabled)
)]
internal static class MechJebModuleAttitudeController_OnModuleDisabled_Patch
{
    static void Postfix(MechJebModuleAttitudeController __instance)
    {
        EventDispatcher.OnAttitudeControllerDisabled(__instance);
    }
}

[HarmonyPatch(
    typeof(MechJebModuleAttitudeController),
    nameof(MechJebModuleAttitudeController.attitudeDeactivate)
)]
internal static class MechJebModuleAttitudeController_AttitudeDeactivate_Patch
{
    static void Postfix(MechJebModuleAttitudeController __instance)
    {
        EventDispatcher.OnAttitudeControllerDisabled(__instance);
    }
}
