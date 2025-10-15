namespace BackgroundThrust;

public static class Config
{
    public static readonly EventData<GameEvents.HostedFromToAction<
        Vessel,
        VesselAutopilot.AutopilotMode
    >> onAutopilotModeChange = new("onAutopilotModeChange");

    /// <summary>
    /// This event is fired when thrust changes from 0 to non-zero on an unloaded vessel.
    /// </summary>
    public static readonly EventData<BackgroundThrustVessel> onUnloadedThrustStarted = new(
        "onUnloadedThrustStarted"
    );

    /// <summary>
    /// This event is fired when the thrust goes to 0 on an unloaded vessel.
    /// </summary>
    public static readonly EventData<BackgroundThrustVessel> onUnloadedThrustStopped = new(
        "onUnloadedThrustStopped"
    );

    public static VesselInfoProvider VesselInfoProvider = new StockVesselInfoProvider();

    /// <summary>
    /// If <c>true</c> then BackgroundThrust will not perform any resource
    /// updates during <c>FixedUpdate</c>. It will then be assumed that
    /// some other method is setting the <c>Thrust</c> field appropriately.
    /// </summary>
    ///
    /// <remarks>
    /// If you plan to override how BackgroundThrust handles resources then
    /// setting this to <c>true</c> will allow you to do so.
    /// </remarks>
    public static bool LoadedResourceProcessing = true;

    /// <summary>
    /// If <c>true</c> then BackgroundThrust will not do any resource handling
    /// in the background.
    /// </summary>
    public static bool BackgroundProcessing = true;

    /// <summary>
    /// An extra multiplier on the buffer capacity for persistent engines.
    /// </summary>
    public static readonly double BufferCapacityMult = 2.0;

    public static ICurrentHeadingProvider HeadingProvider = new DefaultHeadingProvider();
}
