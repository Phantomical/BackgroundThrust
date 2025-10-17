using UnityEngine;

namespace BackgroundThrust.Heading;

public sealed class FixedHeading() : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    QuaternionD orientation;

    public QuaternionD Orientation => orientation;

    public FixedHeading(QuaternionD orientation)
        : this()
    {
        this.orientation = orientation;
    }

    public FixedHeading(Transform transform)
        : this(transform.rotation) { }

    public override TargetHeading GetTargetHeading(double UT) => new(orientation);
}
