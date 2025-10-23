using System.Runtime.InteropServices;
using BackgroundThrust.Heading;
using static VesselAutopilot;
using SpeedDisplayModes = FlightGlobals.SpeedDisplayModes;

namespace BackgroundThrust;

/// <summary>
/// An interface for getting the current <see cref="TargetHeadingProvider"/>
/// for a vessel.
/// </summary>
public interface ICurrentHeadingProvider
{
    /// <summary>
    /// Get a <see cref="TargetHeadingProvider"/> for the vessel. This should
    /// return <c>null</c> if the provider has no target heading to provide
    /// so that the next configured heading provider will get a chance.
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module);
}

/// <summary>
/// A heading provider for all SAS modes.
/// </summary>
public class SASHeadingProvider : ICurrentHeadingProvider
{
    public static readonly SASHeadingProvider Instance = new();

    /// <summary>
    /// Get the current target heading for a vessel. If this returns <c>null</c>
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;
        if (autopilot is null)
            return null;
        if (!vessel.ActionGroups[KSPActionGroup.SAS])
            return null;

        var displayMode = FlightGlobals.speedDisplayMode;
        var heading = displayMode switch
        {
            SpeedDisplayModes.Orbit => GetProviderOrbit(module),
            SpeedDisplayModes.Surface => GetProviderSurface(module),
            SpeedDisplayModes.Target => GetProviderTarget(module),
            _ => GetProviderCommon(module),
        };

        if (heading is ISASHeading sasHeading)
        {
            if (!autopilot.CanSetMode(sasHeading.Mode))
                return null;
        }

        return heading;
    }

    private TargetHeadingProvider GetProviderOrbit(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;

        return autopilot.Mode switch
        {
            AutopilotMode.Prograde => new OrbitPrograde(),
            AutopilotMode.Retrograde => new OrbitRetrograde(),
            AutopilotMode.Normal => new OrbitNormal(),
            AutopilotMode.Antinormal => new OrbitAntiNormal(),
            AutopilotMode.RadialIn => new OrbitRadialIn(),
            AutopilotMode.RadialOut => new OrbitRadialOut(),
            _ => GetProviderCommon(module),
        };
    }

    private TargetHeadingProvider GetProviderSurface(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;

        return autopilot.Mode switch
        {
            AutopilotMode.Prograde => new SurfacePrograde(),
            AutopilotMode.Retrograde => new SurfaceRetrograde(),
            AutopilotMode.Normal => new SurfaceNormal(),
            AutopilotMode.Antinormal => new SurfaceAntiNormal(),
            AutopilotMode.RadialIn => new SurfaceRadialIn(),
            AutopilotMode.RadialOut => new SurfaceRadialOut(),
            _ => GetProviderCommon(module),
        };
    }

    private TargetHeadingProvider GetProviderTarget(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;

        switch (autopilot.Mode)
        {
            case AutopilotMode.Prograde:
                if (vessel.targetObject is not null)
                    return new TargetPrograde();
                goto default;
            case AutopilotMode.Retrograde:
                if (vessel.targetObject is not null)
                    return new TargetRetrograde();
                goto default;

            default:
                return GetProviderSurface(module);
        }
    }

    private TargetHeadingProvider GetProviderCommon(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;

        switch (autopilot.Mode)
        {
            case AutopilotMode.StabilityAssist:
                goto default;

            case AutopilotMode.Target:
                if (vessel.targetObject is not null)
                    return new Target();
                goto default;
            case AutopilotMode.AntiTarget:
                if (vessel.targetObject is not null)
                    return new AntiTarget();
                goto default;

            case AutopilotMode.Maneuver:
                if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                    return new Maneuver();
                goto default;

            default:
                return new FixedHeading(vessel.ReferenceTransform.rotation);
        }
    }
}
