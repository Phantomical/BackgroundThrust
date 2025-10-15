using UnityEngine;

namespace BackgroundThrust.Integration.BRP;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class Loader : MonoBehaviour
{
    void Start()
    {
        Config.VesselInfoProvider = new BRPVesselInfoProvider();
    }
}
