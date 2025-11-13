using System.Collections.Generic;
using BackgroundThrust.Heading;
using UnityEngine;
using static GameEvents;

namespace BackgroundThrust;

public static class Config
{
    public static EventData<
        HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode>
    > OnAutopilotModeChange { get; } = new("onAutopilotModeChange");

    /// <summary>
    /// This event is emitted when the throttle is changed on the vessel.
    /// </summary>
    public static EventData<
        HostedFromToAction<BackgroundThrustVessel, double>
    > OnBackgroundThrottleChanged { get; } = new("onBackgroundThrottleChanged");

    /// <summary>
    /// This event is emitted when the throttle changes while in timewarp.
    /// </summary>
    public static EventData<FromToAction<double, double>> OnWarpThrottleChanged { get; } =
        new("onWarpThrottleChanged");

    /// <summary>
    /// This event is emitted when the target heading provider on the vessel
    /// changes.
    /// </summary>
    public static EventData<
        HostedFromToAction<BackgroundThrustVessel, TargetHeadingProvider>
    > OnTargetHeadingProviderChanged { get; } = new("onTargetHeadingProviderChanged");

    /// <summary>
    /// This event is emitted whenever the target heading is updated for a
    /// loaded vessel in timewarp.
    /// </summary>
    public static EventData<BackgroundThrustVessel, Quaternion> OnTargetHeadingUpdate { get; } =
        new("onTargetHeadingUpdate");

    /// <summary>
    /// This event is emitted whenever the target heading for a vessel in the
    /// background is updated.
    /// </summary>
    public static EventData<
        BackgroundThrustVessel,
        Quaternion
    > OnBackgroundTargetHeadingUpdate { get; } = new("onLoadedTargetHeadingUpdate");

    /// <summary>
    /// This controls how <see cref="BackgroundThrustVessel"/> determines the
    /// current vessel mass and thrust. If you are implementing an integration
    /// that adds custom resource handling or background processing then this
    /// is how you integrate that.
    /// </summary>
    public static VesselInfoProvider VesselInfoProvider { get; set; } = null;

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
