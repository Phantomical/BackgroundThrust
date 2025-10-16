using static GameEvents;

namespace BackgroundThrust;

public static class Config
{
    public static readonly EventData<GameEvents.HostedFromToAction<
        Vessel,
        VesselAutopilot.AutopilotMode
    >> onAutopilotModeChange = new("onAutopilotModeChange");

    /// <summary>
    /// This event is emitted when the throttle is changed on the vessel.
    /// </summary>
    public static readonly EventData<
        HostedFromToAction<BackgroundThrustVessel, double>
    > onBackgroundThrottleChanged = new("onBackgroundThrottleChanged");

    /// <summary>
    /// This event is emitted when the target heading provider on the vessel
    /// changes.
    /// </summary>
    public static readonly EventData<
        HostedFromToAction<BackgroundThrustVessel, TargetHeadingProvider>
    > onHeadingChanged = new("onBackgroundHeadingChanged");

    /// <summary>
    /// This controls how <see cref="BackgroundThrustVessel"/> determines the
    /// current vessel mass and thrust. If you are implementing an integration
    /// that adds custom resource handling or background processing then this
    /// is how you integrate that.
    /// </summary>
    public static VesselInfoProvider VesselInfoProvider = new StockVesselInfoProvider();

    /// <summary>
    /// This returns the current heading provider that
    /// <see cref="BackgroundThrustVessel"/> will use when the ship goes on rails.
    /// </summary>
    public static ICurrentHeadingProvider HeadingProvider = new DefaultHeadingProvider();

    /// <summary>
    /// An extra multiplier on the buffer capacity for persistent engines.
    /// </summary>
    public static readonly double BufferCapacityMult = 2.0;
}
