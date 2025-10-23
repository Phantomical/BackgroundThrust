using static VesselAutopilot;

namespace BackgroundThrust.Heading;

public abstract class TargetBase : TargetHeadingProvider
{
    public ITargetable Target => Vessel.targetObject;

    protected TargetBase() { }
}

public class Target : TargetBase
{
    public Target() { }

    protected Vector3d? GetTargetDirection(double UT)
    {
        var target = Target;
        if (target is null)
            return null;

        Vector3d vpos = Vessel.ReferenceTransform.position;
        Vector3d tpos = target.GetTransform().position;

        return (tpos - vpos).normalized;
    }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetTargetDirection(UT) is not Vector3d target)
            return default;

        return TargetHeading.PointAt(Vessel, target);
    }
}

public class AntiTarget : Target
{
    public AntiTarget() { }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetTargetDirection(UT) is not Vector3d target)
            return default;
        return TargetHeading.PointAt(Vessel, -target);
    }
}

public class TargetPrograde : TargetBase, ISASHeading
{
    public virtual AutopilotMode Mode => AutopilotMode.Target;

    public TargetPrograde() { }

    protected Vector3d? GetRelVelocityDirection(double UT)
    {
        var target = Target;
        if (target is null)
            return null;

        Vector3d vvel = Vessel.obt_velocity;
        Vector3d tvel = target.GetObtVelocity();

        return (vvel - tvel).normalized;
    }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetRelVelocityDirection(UT) is not Vector3d heading)
            return default;

        return TargetHeading.PointAt(Vessel, heading);
    }
}

public class TargetRetrograde : TargetPrograde
{
    public override AutopilotMode Mode => AutopilotMode.AntiTarget;

    public TargetRetrograde() { }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetRelVelocityDirection(UT) is not Vector3d heading)
            return default;

        return TargetHeading.PointAt(Vessel, -heading);
    }
}
