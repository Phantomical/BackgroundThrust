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

    public override Vector3d? GetTargetHeading(double UT)
    {
        var target = Target;
        if (target is null)
            return null;

        var vpos = Vessel.ReferenceTransform.position;
        var tpos = target.GetTransform().position;

        return (tpos - vpos).normalized;
    }
}

public class AntiTarget : Target
{
    public AntiTarget() { }

    public AntiTarget(ITargetable target)
        : base(target) { }

    public override Vector3d? GetTargetHeading(double UT)
    {
        if (base.GetTargetHeading(UT) is Vector3d heading)
            return -heading;
        return null;
    }
}

public class TargetPrograde : TargetBase
{
    public TargetPrograde() { }

    public TargetPrograde(ITargetable target)
        : base(target) { }

    public override Vector3d? GetTargetHeading(double UT)
    {
        var target = Target;
        if (target is null)
            return null;

        var vvel = Vessel.obt_velocity;
        var tvel = target.GetObtVelocity();

        return (vvel - tvel).normalized;
    }
}

public class TargetRetrograde : TargetPrograde
{
    public TargetRetrograde() { }

    public TargetRetrograde(ITargetable target)
        : base(target) { }

    public override Vector3d? GetTargetHeading(double UT)
    {
        if (base.GetTargetHeading(UT) is Vector3d heading)
            return -heading;
        return null;
    }
}
