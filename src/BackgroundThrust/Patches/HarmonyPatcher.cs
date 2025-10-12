using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.Patches;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }
}
