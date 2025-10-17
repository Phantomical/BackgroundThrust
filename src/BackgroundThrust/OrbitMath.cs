namespace BackgroundThrust;

/// <summary>
/// Helpers for orbital math.
/// </summary>
public static class OrbitMath
{
    public static Vector3d GetOrbitProgradeAtUT(Vessel vessel, double UT) =>
        vessel.orbit.Prograde(UT);

    public static Vector3d GetOrbitRetrogradeAtUT(Vessel vessel, double UT) =>
        -GetOrbitProgradeAtUT(vessel, UT);

    public static Vector3d GetOrbitNormalAtUT(Vessel vessel, double UT) => vessel.orbit.h.xzy;

    public static Vector3d GetOrbitAntiNormalAtUT(Vessel vessel, double UT) =>
        -GetOrbitNormalAtUT(vessel, UT);

    public static Vector3d GetOrbitRadialOutAtUT(Vessel vessel, double UT) =>
        -GetOrbitRadialInAtUT(vessel, UT);

    public static Vector3d GetOrbitRadialInAtUT(Vessel vessel, double UT) =>
        vessel.orbit.Radial(UT);

    /// <summary>
    /// Perturbs an orbit by a <paramref name="deltaV"/> vector.
    /// </summary>
    internal static void Perturb(this Orbit orbit, Vector3d deltaV, double UT)
    {
        if (deltaV.sqrMagnitude == 0.0)
            return;

        deltaV = deltaV.xzy;
        Vector3d pos = orbit.getRelativePositionAtUT(UT);

        // Update with current position and new velocity
        orbit.UpdateFromStateVectors(
            pos,
            orbit.getOrbitalVelocityAtUT(UT) + deltaV,
            orbit.referenceBody,
            UT
        );
        orbit.Init();
        orbit.UpdateFromUT(UT);
    }
}
