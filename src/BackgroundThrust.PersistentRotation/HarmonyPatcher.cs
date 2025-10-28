using HarmonyLib;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class HarmonyPatcher : MonoBehaviour
{
    void Awake()
    {
        Harmony harmony = new("BackgroundThrust.PersistentRotation");
        harmony.PatchAll(typeof(HarmonyPatcher).Assembly);
    }
}
