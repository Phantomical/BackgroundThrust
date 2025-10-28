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

        // We want the reference transform to point in the target direction
        // so we need to correct the orientation to apply correctly.
        var relative =
            vessel.transform.rotation * Quaternion.Inverse(vessel.ReferenceTransform.rotation);
        orientation *= relative;

        pr.storedAngularMomentum = Vector3d.zero;
        pr.rotation = orientation;
    }
}
