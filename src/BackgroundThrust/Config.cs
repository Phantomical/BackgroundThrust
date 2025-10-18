using System.Collections.Generic;
using BackgroundThrust.Heading;
using static GameEvents;

namespace BackgroundThrust;

public static class Config
{
    public static readonly EventData<
        HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode>
    > onAutopilotModeChange = new("onAutopilotModeChange");

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
    /// An extra multiplier on the buffer capacity for persistent engines.
    /// </summary>
    public static readonly double BufferCapacityMult = 2.0;

    #region Heading Providers
    public static readonly List<ICurrentHeadingProvider> HeadingProviders =
    [
        SASHeadingProvider.Instance,
    ];

    public static TargetHeadingProvider GetTargetHeading(BackgroundThrustVessel module)
    {
        for (int i = HeadingProviders.Count - 1; i >= 0; --i)
        {
            var heading = HeadingProviders[i].GetCurrentHeading(module);
            if (heading is not null)
                return heading;
        }

        return new CurrentHeading();
    }

    /// <summary>
    /// Add a new heading provider to the list of configured providers.
    /// </summary>
    /// <param name="provider"></param>
    /// <remarks>
    /// Heading providers are queried in the reverse order in which they were
    /// added.
    /// </remarks>
    public static void AddHeadingProvider(ICurrentHeadingProvider provider) =>
        HeadingProviders.Add(provider);

    /// <summary>
    /// Remove an existing heading provider from the list. This uses
    /// <see cref="object.ReferenceEquals"/> in order to determine equality.
    /// </summary>
    /// <param name="provider"></param>
    public static void RemoveHeadingProvider(ICurrentHeadingProvider provider)
    {
        int i = 0;
        int j = 0;
        int count = HeadingProviders.Count;
        for (; i < count; ++i)
        {
            if (!ReferenceEquals(HeadingProviders[i], provider))
                HeadingProviders[j++] = HeadingProviders[i];
        }

        HeadingProviders.RemoveRange(j, count - j);
    }
    #endregion
}
