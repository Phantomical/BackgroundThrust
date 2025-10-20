namespace BackgroundThrust.Heading;

public class SurfacePrograde : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.srf_velocity.normalized);
}

public class SurfaceRetrograde : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.srf_velocity.normalized);
}

public class SurfaceNormal : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.mainBody.RotationAxis);
}

public class SurfaceAntiNormal : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.mainBody.RotationAxis);
}

public class SurfaceRadialIn : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.upAxis);
}

public class SurfaceRadialOut : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.upAxis);
}
