using static VesselAutopilot;

namespace BackgroundThrust.Heading;

public class SurfacePrograde : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Prograde;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.srf_velocity.normalized);
}

public class SurfaceRetrograde : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Retrograde;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.srf_velocity.normalized);
}

public class SurfaceNormal : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Normal;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.mainBody.RotationAxis);
}

public class SurfaceAntiNormal : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Antinormal;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.mainBody.RotationAxis);
}

public class SurfaceRadialIn : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.RadialIn;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, Vessel.upAxis);
}

public class SurfaceRadialOut : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.RadialOut;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, -Vessel.upAxis);
}
