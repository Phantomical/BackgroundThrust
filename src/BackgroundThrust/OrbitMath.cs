namespace BackgroundThrust;

/// <summary>
/// Helpers for orbital math.
/// </summary>
public static class OrbitMath
{
    public static Vector3d GetOrbitProgradeAtUT(Vessel vessel, double UT) =>
        vessel.orbit.getOrbitalVelocityAtUT(UT).normalized;

    public static Vector3d GetOrbitRetrogradeAtUT(Vessel vessel, double UT) =>
        -GetOrbitProgradeAtUT(vessel, UT);

    public static Vector3d GetOrbitNormalAtUT(Vessel vessel, double UT)
    {
        var ppos = vessel.mainBody.getPositionAtUT(UT);
        var vpos = vessel.orbit.getPositionAtUT(UT);

        return Vector3d.Cross(GetOrbitProgradeAtUT(vessel, UT), vpos - ppos).normalized;
    }

    public static Vector3d GetOrbitAntiNormalAtUT(Vessel vessel, double UT) =>
        -GetOrbitNormalAtUT(vessel, UT);

    public static Vector3d GetOrbitRadialOutAtUT(Vessel vessel, double UT)
    {
        var obtvel = vessel.orbit.getOrbitalVelocityAtUT(UT).normalized;
        var ppos = vessel.mainBody.getPositionAtUT(UT);
        var vpos = vessel.orbit.getPositionAtUT(UT);

        return Vector3d.Cross(obtvel, Vector3d.Cross(obtvel, vpos - ppos)).normalized;
    }

    public static Vector3d GetOrbitRadialInAtUT(Vessel vessel, double UT) =>
        GetOrbitRadialOutAtUT(vessel, UT);
}
