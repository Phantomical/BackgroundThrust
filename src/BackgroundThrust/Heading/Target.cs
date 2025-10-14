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

        var vpos = Vessel.orbit.getPositionAtUT(UT);
        var tpos = target.GetOrbit().getPositionAtUT(UT);

        return (tpos - vpos).normalized;
    }
}

public class AntiTarget : TargetBase
{
    public AntiTarget() { }

    public AntiTarget(ITargetable target)
        : base(target) { }

    public override Vector3d? GetTargetHeading(double UT)
    {
        var target = Target;
        if (target is null)
            return null;

        var vpos = Vessel.orbit.getPositionAtUT(UT);
        var tpos = target.GetOrbit().getPositionAtUT(UT);

        return (vpos - tpos).normalized;
    }
}
