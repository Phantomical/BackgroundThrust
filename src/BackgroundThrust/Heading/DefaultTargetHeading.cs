using UnityEngine;

namespace BackgroundThrust.Heading;

public class CurrentHeading() : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    QuaternionD orientation;

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (Vessel.loaded)
        {
            orientation = Vessel.ReferenceTransform.rotation;
            return new(Vessel.ReferenceTransform);
        }

        return new(orientation);
    }

    protected override void OnSave(ConfigNode node)
    {
        if (Vessel?.loaded ?? false)
            orientation = Vessel.ReferenceTransform.rotation;

        base.OnSave(node);
    }
}
