using HarmonyLib;
using UnityEngine;

namespace BackgroundThrust.Integration.MechJeb2;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class Loader : MonoBehaviour
{
    void Awake()
    {
        var harmony = new Harmony("BackgroundThrust.Integration.MechJeb2");
        harmony.PatchAll(typeof(Loader).Assembly);
    }

    void Start()
    {
        Config.AddHeadingProvider(new MechJebCurrentHeadingProvider());
    }
}
