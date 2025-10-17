namespace BackgroundThrust.Heading;

public class CurrentHeading() : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    Vector3d heading = Vector3d.zero;

    public override Vector3d GetTargetHeading(double UT)
    {
        if (Vessel is null)
            return heading;

        if (Vessel.loaded)
            return Vessel.ReferenceTransform.up;

        return heading;
    }

    protected override void OnSave(ConfigNode node)
    {
        heading = Vessel?.ReferenceTransform?.up ?? Vector3d.zero;

        base.OnSave(node);
    }
}
