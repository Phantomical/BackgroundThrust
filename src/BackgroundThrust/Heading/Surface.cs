namespace BackgroundThrust.Heading;

public class SurfacePrograde : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => Vessel.srf_velocity.normalized;
}

public class SurfaceRetrograde : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => -Vessel.srf_velocity.normalized;
}

public class SurfaceNormal : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => Vessel.mainBody.RotationAxis;
}

public class SurfaceAntiNormal : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => -Vessel.mainBody.RotationAxis;
}

public class SurfaceRadialIn : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => Vessel.upAxis;
}

public class SurfaceRadialOut : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) => -Vessel.upAxis;
}
