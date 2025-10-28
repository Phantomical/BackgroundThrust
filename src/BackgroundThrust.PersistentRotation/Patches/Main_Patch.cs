using HarmonyLib;
using PersistentRotation;

namespace BackgroundThrust.PersistentRotation.Patches;

[HarmonyPatch(typeof(Main), nameof(Main.GetStabilityMode))]
internal static class Main_GetStabilityMode_Patch
{
    static bool Prefix(Vessel vessel, ref Main.StabilityMode __result)
    {
        if (!vessel.loaded)
            return true;

        var module = vessel.GetBackgroundThrust();
        if (module is null)
            return true;

        if (!module.Active)
            return true;

        // If storedAngularMomentum is 0 this results in no rotation being
        // applied, which is what we want.
        __result = Main.StabilityMode.ABSOLUTE;
        return false;
    }
}
