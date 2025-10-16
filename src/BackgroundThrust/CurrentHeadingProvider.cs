using System;
using BackgroundThrust.Heading;
using static VesselAutopilot;
using SpeedDisplayModes = FlightGlobals.SpeedDisplayModes;

namespace BackgroundThrust;

public interface ICurrentHeadingProvider
{
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module);
}

public class DefaultHeadingProvider : ICurrentHeadingProvider
{
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var autopilot = vessel.Autopilot;

        if (!autopilot.Enabled)
            return new FixedHeading(vessel.transform.up);

        var displayMode = FlightGlobals.speedDisplayMode;
        return displayMode switch
        {
            SpeedDisplayModes.Orbit => GetProviderOrbit(module),
            SpeedDisplayModes.Surface => GetProviderSurface(module),
            SpeedDisplayModes.Target => GetProviderTarget(module),
            _ => GetProviderCommon(module),
        };
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
                    return new TargetPrograde(vessel.targetObject);
                goto default;
            case AutopilotMode.Retrograde:
                if (vessel.targetObject is not null)
                    return new TargetRetrograde(vessel.targetObject);
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
                    return new Target(vessel.targetObject);
                goto default;
            case AutopilotMode.AntiTarget:
                if (vessel.targetObject is not null)
                    return new AntiTarget(vessel.targetObject);
                goto default;

            default:
                return new FixedHeading(vessel.transform.up);
        }
    }
}
