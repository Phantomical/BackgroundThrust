using UnityEngine;

namespace BackgroundThrust.Integration.MechJeb2;

[KSPAddon(KSPAddon.Startup.Instantly, once: true)]
internal class Loader : MonoBehaviour
{
    void Start()
    {
        Config.AddHeadingProvider(new MechJebCurrentHeadingProvider());
    }
}
