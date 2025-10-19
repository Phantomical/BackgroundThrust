using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.Integration.RealFuels;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust.Integration.RealFuels");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }
}
