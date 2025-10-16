using System;

namespace BackgroundThrust;

/// <summary>
/// The parameters needed to determine the change in the vessel's orbit due to
/// its thrust.
/// </summary>
///
/// <remarks>
/// You should assume that mass was consumed (and/or) produced at a constant
/// rate between <see cref="StartUT"/> and <see cref="StopUT"/>. Note that
/// this doesn't mean that the vessel was accelerating at a constant rate.
/// </remarks>
public struct ThrustParameters
{
    /// <summary>
    /// The starting UT for thrust integration.
    /// </summary>
    public double StartUT;

    /// <summary>
    /// The ending UT for thrust integration.
    /// </summary>
    public double StopUT;

    /// <summary>
    /// The vessel mass at <see cref="StartUT"/>.
    /// </summary>
    public double StartMass;

    /// <summary>
    /// The vessel mass at <see cref="StopUT"/>.
    /// </summary>
    public double StopMass;

    /// <summary>
    /// The thrust emitted by the vessel between <see cref="StartUT"/>
    /// and <see cref="StopUT"/>.
    /// </summary>
    public Vector3d Thrust;

    public readonly double DeltaT => StopUT - StartUT;
    public readonly double DeltaM => StopMass - StartMass;

    public readonly double ComputeDeltaV()
    {
        var thrust = Thrust.magnitude;
        var deltaM = DeltaM;
        if (Math.Abs(deltaM) < 1e-6)
            return thrust / StopMass * DeltaT;

        return thrust / deltaM * Math.Log(StopMass / StartMass) * DeltaT;
    }

    /// <summary>
    /// Assuming that the thrust and mass consumption rate remains constant,
    /// returns the UT at which <paramref name="dv"/> delta-v will have been
    /// applied, with 0 at <see cref="StartUT"/>.
    /// </summary>
    /// <param name="dv"></param>
    /// <returns></returns>
    public readonly double GetUTAtDeltaV(double dv)
    {
        var dm = DeltaM / DeltaT;
        var thrust = Thrust.magnitude;

        return StartUT + StartMass / dm * (Math.Exp(dv * dm / thrust) - 1);
    }
}
