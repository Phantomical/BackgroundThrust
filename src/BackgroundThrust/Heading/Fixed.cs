using UnityEngine;

namespace BackgroundThrust.Heading;

public sealed class FixedHeading() : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    Quaternion orientation;

    public Quaternion Orientation => orientation;

    public FixedHeading(Quaternion orientation)
        : this()
    {
        this.orientation = orientation;
    }

    public FixedHeading(Transform transform)
        : this(transform.rotation) { }

    public override TargetHeading GetTargetHeading(double UT) => new(orientation);

    public override bool Equals(object obj)
    {
        if (obj is not FixedHeading other)
            return false;
        if (!base.Equals(obj))
            return false;

        return other.orientation == orientation;
    }

    public override int GetHashCode() => base.GetHashCode();
}
