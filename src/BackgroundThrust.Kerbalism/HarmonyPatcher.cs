using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.Kerbalism;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust.Kerbalism");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }

    void Start()
    {
        Config.VesselInfoProvider = new KerbalismVesselInfoProvider();
    }
}
