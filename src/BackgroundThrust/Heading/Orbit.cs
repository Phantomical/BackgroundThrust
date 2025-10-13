namespace BackgroundThrust.Heading;

public sealed class OrbitPrograde() : TargetHeadingProvider
{
    public static readonly OrbitPrograde Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitProgradeAtUT(module.Vessel, UT);
}

public sealed class OrbitRetrograde() : TargetHeadingProvider
{
    public static readonly OrbitRetrograde Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitRetrogradeAtUT(module.Vessel, UT);
}

public sealed class OrbitNormal() : TargetHeadingProvider
{
    public static readonly OrbitNormal Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitNormalAtUT(module.Vessel, UT);
}

public sealed class OrbitAntiNormal() : TargetHeadingProvider
{
    public static readonly OrbitAntiNormal Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitAntiNormalAtUT(module.Vessel, UT);
}

public sealed class OrbitRadialIn() : TargetHeadingProvider
{
    public static readonly OrbitRadialIn Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitRadialInAtUT(module.Vessel, UT);
}

public sealed class OrbitRadialOut() : TargetHeadingProvider
{
    public static readonly OrbitRadialOut Instance = new();

    public override Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT) =>
        OrbitMath.GetOrbitRadialOutAtUT(module.Vessel, UT);
}
