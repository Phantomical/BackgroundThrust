using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.SolverEngines;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust.SolverEngines");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }
}
