namespace BackgroundThrust.Heading;

public class SurfacePrograde : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(Vessel.srf_velocity.normalized, Vessel);
}

public class SurfaceRetrograde : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(-Vessel.srf_velocity.normalized, Vessel);
}

public class SurfaceNormal : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(Vessel.mainBody.RotationAxis, Vessel);
}

public class SurfaceAntiNormal : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(-Vessel.mainBody.RotationAxis, Vessel);
}

public class SurfaceRadialIn : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) => new(Vessel.upAxis, Vessel);
}

public class SurfaceRadialOut : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) => new(-Vessel.upAxis, Vessel);
}
