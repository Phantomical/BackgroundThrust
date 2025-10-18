using HarmonyLib;
using SolverEngines;

namespace BackgroundThrust.Integration.SolverEngines.Patches;

[HarmonyPatch(typeof(BackgroundEngine), "EngineFixedUpdate")]
internal static class BackgroundEngine_EngineFixedUpdate_Patch
{
    static bool Prefix(BackgroundEngine __instance)
    {
        if (__instance.Engine is ModuleEnginesSolver engine)
        {
            engine.FixedUpdate();
            return false;
        }

        return true;
    }
}
