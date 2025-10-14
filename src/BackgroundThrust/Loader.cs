using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
internal class Loader : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust");
        harmony.PatchAll(typeof(Loader).Assembly);
    }

    void Start()
    {
        TargetHeadingProvider.RegisterAll();
    }
}
