using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.RealFuels;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust.RealFuels");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }
}
