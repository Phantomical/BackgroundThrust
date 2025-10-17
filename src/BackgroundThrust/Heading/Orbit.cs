namespace BackgroundThrust.Heading;

public sealed class OrbitPrograde() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitProgradeAtUT(Vessel, UT);
}

public sealed class OrbitRetrograde() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitRetrogradeAtUT(Vessel, UT);
}

public sealed class OrbitNormal() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitNormalAtUT(Vessel, UT);
}

public sealed class OrbitAntiNormal() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitAntiNormalAtUT(Vessel, UT);
}

public sealed class OrbitRadialIn() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitRadialInAtUT(Vessel, UT);
}

public sealed class OrbitRadialOut() : TargetHeadingProvider
{
    public override Vector3d GetTargetHeading(double UT) =>
        OrbitMath.GetOrbitRadialOutAtUT(Vessel, UT);
}
