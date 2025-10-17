namespace BackgroundThrust.Heading;

public sealed class OrbitPrograde() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitProgradeAtUT(Vessel, UT), Vessel);
}

public sealed class OrbitRetrograde() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitRetrogradeAtUT(Vessel, UT), Vessel);
}

public sealed class OrbitNormal() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitNormalAtUT(Vessel, UT), Vessel);
}

public sealed class OrbitAntiNormal() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitAntiNormalAtUT(Vessel, UT), Vessel);
}

public sealed class OrbitRadialIn() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitRadialInAtUT(Vessel, UT), Vessel);
}

public sealed class OrbitRadialOut() : TargetHeadingProvider
{
    public override TargetHeading GetTargetHeading(double UT) =>
        new(OrbitMath.GetOrbitRadialOutAtUT(Vessel, UT), Vessel);
}
