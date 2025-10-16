using BackgroundThrust.Utils;
using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(Vessel), nameof(Vessel.UpdateAcceleration))]
internal static class Vessel_UpdateAcceleration_Patch
{
    static void Postfix(Vessel __instance)
    {
        var vessel = __instance;
        if (!vessel.loaded || !vessel.packed)
            return;

        if (vessel.LandedOrSplashed)
            return;
        if (!vessel.IsOrbiting())
            return;

        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();
        var accel = module.Thrust.magnitude / vessel.totalMass;
        if (MathUtil.IsFinite(accel))
            vessel.geeForce = accel / PhysicsGlobals.GravitationalAcceleration;
    }
}
