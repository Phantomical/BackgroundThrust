using static VesselAutopilot;

namespace BackgroundThrust.Heading;

public sealed class OrbitPrograde() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Prograde;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitProgradeAtUT(Vessel, UT));
}

public sealed class OrbitRetrograde() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Retrograde;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRetrogradeAtUT(Vessel, UT));
}

public sealed class OrbitNormal() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Normal;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitNormalAtUT(Vessel, UT));
}

public sealed class OrbitAntiNormal() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.Antinormal;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitAntiNormalAtUT(Vessel, UT));
}

public sealed class OrbitRadialIn() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.RadialIn;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRadialInAtUT(Vessel, UT));
}

public sealed class OrbitRadialOut() : TargetHeadingProvider, ISASHeading
{
    public AutopilotMode Mode => AutopilotMode.RadialOut;

    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRadialOutAtUT(Vessel, UT));
}
