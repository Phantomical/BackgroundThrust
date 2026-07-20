using BackgroundThrust;
using PersistentRotation;
using UnityEngine;

namespace BackgroundThrust.PersistentRotation;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
internal class EventDispatcher : MonoBehaviour
{
    void Start()
    {
        Config.OnTargetHeadingUpdate.Add(OnTargetHeadingUpdate);
    }

    void OnDestroy()
    {
        Config.OnTargetHeadingUpdate.Remove(OnTargetHeadingUpdate);
    }

    void OnTargetHeadingUpdate(BackgroundThrustVessel module, Quaternion orientation)
    {
        var vessel = module.Vessel;
        var pr = Data.instance?.FindPRVessel(vessel);
        if (pr is null)
            return;

        // The target orientation says where the control point should end up but
        // PersistentRotation stores the rotation of the vessel transform, so we
        // subtract the control point's rotation relative to the vessel the same
        // way that RotateToOrientation does.
        orientation *= Quaternion.Inverse(module.ControlPointRotation);

        pr.storedAngularMomentum = Vector3d.zero;
        pr.rotation = orientation;
    }
}
