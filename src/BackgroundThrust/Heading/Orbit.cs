namespace BackgroundThrust.Heading;

public sealed class OrbitPrograde() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitProgradeAtUT(Vessel, UT));
}

public sealed class OrbitRetrograde() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRetrogradeAtUT(Vessel, UT));
}

public sealed class OrbitNormal() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitNormalAtUT(Vessel, UT));
}

public sealed class OrbitAntiNormal() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitAntiNormalAtUT(Vessel, UT));
}

public sealed class OrbitRadialIn() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRadialInAtUT(Vessel, UT));
}

public sealed class OrbitRadialOut() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        TargetHeading.PointAt(Vessel, OrbitMath.GetOrbitRadialOutAtUT(Vessel, UT));
}
