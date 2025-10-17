using System;

namespace BackgroundThrust.Heading;

public abstract class TargetBase : TargetHeadingProvider
{
    private ProtoTargetInfo targetInfo;
    private ITargetable target;

    public ProtoTargetInfo TargetInfo => targetInfo;
    public ITargetable Target => target ??= TargetInfo?.FindTarget();

    protected TargetBase() { }

    protected TargetBase(ITargetable target)
    {
        targetInfo = new(target);
        this.target = target;
    }

    protected override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);
        targetInfo = new(node);
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);
        targetInfo?.Save(node);
    }
}

public class Target : TargetBase
{
    public Target() { }

    public Target(ITargetable target)
        : base(target) { }

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

        return new(target, Vessel);
    }
}

public class AntiTarget : Target
{
    public AntiTarget() { }

    public AntiTarget(ITargetable target)
        : base(target) { }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetTargetDirection(UT) is not Vector3d target)
            return default;
        return new(-target, Vessel);
    }
}

public class TargetPrograde : TargetBase
{
    public TargetPrograde() { }

    public TargetPrograde(ITargetable target)
        : base(target) { }

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

        return new(heading, Vessel);
    }
}

public class TargetRetrograde : TargetPrograde
{
    public TargetRetrograde() { }

    public TargetRetrograde(ITargetable target)
        : base(target) { }

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (GetRelVelocityDirection(UT) is not Vector3d heading)
            return default;

        return new(-heading, Vessel);
    }
}
