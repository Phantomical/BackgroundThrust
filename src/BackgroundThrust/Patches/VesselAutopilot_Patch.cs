using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(VesselAutopilot), nameof(VesselAutopilot.SetMode))]
internal static class VesselAutopilot_SetMode_Patch
{
    static void Prefix(VesselAutopilot __instance, out VesselAutopilot.AutopilotMode __state)
    {
        __state = __instance.Mode;
    }

    static void Postfix(VesselAutopilot __instance, VesselAutopilot.AutopilotMode __state)
    {
        var from = __state;
        var to = __instance.Mode;

        if (from == to)
            return;

        Config.onAutopilotModeChange.Fire(
            new GameEvents.HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode>(
                __instance.Vessel,
                from,
                to
            )
        );
    }
}
