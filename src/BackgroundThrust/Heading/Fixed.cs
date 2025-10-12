namespace BackgroundThrust.Heading;

public sealed class FixedHeading() : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    Vector3d heading;

    public Vector3d Heading => heading;

    public FixedHeading(Vector3d heading)
        : this()
    {
        if (heading != Vector3d.zero)
            heading = heading.normalized;
        this.heading = heading;
    }

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT)
    {
        if (heading == Vector3d.zero)
            return null;

        return heading;
    }
}
