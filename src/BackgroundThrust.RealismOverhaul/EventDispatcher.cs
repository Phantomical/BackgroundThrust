using RealismOverhaul;
using UnityEngine;

namespace BackgroundThrust.RealismOverhaul;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
public class EventDispatcher : MonoBehaviour
{
    void Awake()
    {
        if (!VesselModuleRotationRO.IsEnabled)
        {
            enabled = false;
            Destroy(this);
        }
    }

    void Start()
    {
        Config.OnTargetHeadingUpdate.Add(OnTargetHeadingUpdate);
    }

    void OnDestroy()
    {
        Config.OnTargetHeadingUpdate.Remove(OnTargetHeadingUpdate);
    }

    void OnTargetHeadingUpdate(BackgroundThrustVessel tvmodule, Quaternion target)
    {
        var vessel = tvmodule.Vessel;
        var module = vessel.FindVesselModuleImplementing<VesselModuleRotationRO>();
        if (module is null)
            return;

        module.angularVelocity = Vector3d.zero;
    }
}
