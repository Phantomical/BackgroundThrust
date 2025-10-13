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

        switch (autopilot.Mode)
        {
            case AutopilotMode.StabilityAssist:
                return new FixedHeading(vessel.transform.up);

            case AutopilotMode.Prograde:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitPrograde();
                break;
            case AutopilotMode.Retrograde:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitRetrograde();
                break;
            case AutopilotMode.Normal:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitNormal();
                break;
            case AutopilotMode.Antinormal:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitAntiNormal();
                break;
            case AutopilotMode.RadialIn:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitRadialIn();
                break;
            case AutopilotMode.RadialOut:
                if (displayMode == SpeedDisplayModes.Orbit)
                    return new OrbitRadialOut();
                break;

            default:
                break;
        }

        return new FixedHeading(vessel.transform.up);
    }
}
