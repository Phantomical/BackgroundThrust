using System;
using System.Collections;
using System.Collections.Generic;
using BackgroundThrust.Heading;
using BackgroundThrust.Utils;
using UnityEngine;

namespace BackgroundThrust;

public class BackgroundThrustVessel : VesselModule
{
    /// <summary>
    /// The UT at which an update last occurred.
    /// </summary>
    [KSPField(isPersistant = true)]
    public double LastUpdateTime = 0.0;

    /// <summary>
    /// The vessel mass at the time that an update last occurred.
    /// </summary>
    [KSPField(isPersistant = true)]
    public double LastUpdateMass = 0.0;

    [KSPField(isPersistant = true)]
    private double throttle = 0.0;

    /// <summary>
    /// The current throttle for this vessel.
    /// </summary>
    ///
    /// <remarks>
    /// While the vessel is loaded this will mirror the current value of
    /// <c>vessel.ctrlState.mainThrottle</c>. If you are implementing background
    /// processing then you can use <see cref="SetThrottle(double)"/> in order to
    /// change the throttle up or down.
    /// </remarks>
    public double Throttle => throttle;

    /// <summary>
    /// The current vessel heading for the purposes of applying thrust.
    /// </summary>
    public Vector3d Heading => vessel?.ReferenceTransform?.up ?? vessel.transform.up;

    /// <summary>
    /// The known dry mass of the vessel. This is only updated when the vessel
    /// is unloaded.
    /// </summary>
    public double? DryMass = 0.0;

    /// <summary>
    /// The current thrust applied on this vessel. Zero if not packed or in the
    /// background.
    /// </summary>
    public Vector3d Thrust { get; private set; } = Vector3d.zero;

    public TargetHeadingProvider TargetHeading { get; private set; }

    private List<BackgroundEngine> _engines = null;
    public List<BackgroundEngine> Engines
    {
        get => _engines ??= vessel?.FindPartModulesImplementing<BackgroundEngine>();
        set => _engines = value;
    }

    void FixedUpdate()
    {
        if (!vessel.loaded)
            BackgroundFixedUpdate();
        else if (vessel.packed)
            PackedFixedUpdate();
        else
            UnpackedFixedUpdate();
    }

    #region Getters and Setters
    public void SetThrottle(double throttle) =>
        SetThrottle(throttle, Planetarium.GetUniversalTime());

    public void SetThrottle(double throttle, double UT)
    {
        throttle = UtilMath.Clamp01(throttle);

        if (vessel.loaded)
        {
            vessel.ctrlState.mainThrottle = (float)throttle;
            this.throttle = throttle;
        }
        else
        {
            var wasZero = this.throttle == 0.0;
            var nowZero = throttle == 0.0;

            if (wasZero)
                LastUpdateTime = Math.Max(LastUpdateTime, UT);

            if (!nowZero && TargetHeading is not null)
                enabled = true;

            var prev = this.throttle;
            this.throttle = throttle;

            if (!vessel.loaded && prev != throttle)
                Config.OnBackgroundThrottleChanged.Fire(new(this, prev, throttle));
        }
    }

    public void SetTargetHeading(TargetHeadingProvider heading) =>
        SetTargetHeading(heading, Planetarium.GetUniversalTime());

    public void SetTargetHeading(TargetHeadingProvider heading, double UT)
    {
        if (TargetHeading is null)
            LastUpdateTime = Math.Max(LastUpdateTime, UT);

        if (heading is not null)
            enabled = true;

        var prev = TargetHeading;

        heading?.Vessel = Vessel;
        TargetHeading = heading;

        if (!ReferenceEquals(prev, heading))
        {
            try
            {
                heading?.OnInstalled();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"{heading?.GetType().Name ?? "null"}.OnInstalled threw an exception"
                );
                Debug.LogException(e);

                heading = null;
                TargetHeading = null;
            }
        }

        if (!ReferenceEquals(prev, heading))
            Config.OnTargetHeadingProviderChanged.Fire(new(this, prev, heading));
    }

    /// <summary>
    /// An event that might change the target heading has occurred. Create a
    /// new heading provider and update the current one for this vessel if
    /// it is different from the current one.
    /// </summary>
    ///
    /// <remarks>
    /// This method is a no-op if the vessel is not currently loaded.
    /// </remarks>
    public void RefreshTargetHeading() => RefreshTargetHeading(Planetarium.GetUniversalTime());

    public void RefreshTargetHeading(double UT)
    {
        if (!Vessel.loaded)
            return;

        var heading = GetNewHeadingProvider();
        if (TargetHeading is null || !heading.Equals(TargetHeading))
            SetTargetHeading(heading, UT);
    }
    #endregion

    #region FixedUpdate for unpacked vessels
    void UnpackedFixedUpdate()
    {
        LastUpdateTime = Planetarium.GetUniversalTime();
        LastUpdateMass = vessel.totalMass;
        throttle = vessel.ctrlState.mainThrottle;
        Thrust = Vector3d.zero;
    }
    #endregion

    #region Packed Vessel Handling
    void PackedFixedUpdate()
    {
        throttle = vessel.ctrlState.mainThrottle;
        Thrust = Vector3d.zero;

        var provider = Config.VesselInfoProvider;
        var now = Planetarium.GetUniversalTime();
        var lastUpdateTime = LastUpdateTime;
        var lastUpdateMass = LastUpdateMass;
        var currentMass = provider.GetVesselMass(this, now);

        LastUpdateTime = now;
        LastUpdateMass = currentMass;

        if (lastUpdateTime == 0.0)
            return;

        if (TargetHeading is null)
            SetTargetHeading(GetFixedHeading());

        var target = TargetHeading.GetTargetHeading(now);

        // Protect against invalid heading vectors before they cause the vessel
        // to get deleted because its state is NaN.
        if (!target.IsValid())
        {
            var tname = TargetHeading.GetType().Name;
            var q = target.Orientation;
            var mag2 = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;

            if (double.IsInfinity(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned infinite quaternion");
            else if (double.IsNaN(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned NaN quaternion");
            else
                LogUtil.Error($"{tname}.GetTargetHeading returned zero quaternion");

            SetThrottle(0.0);
            SetTargetHeading(null);
            TimeWarp.SetRate(0, instant: true);
            ScreenMessages.PostScreenMessage("Recieved invalid heading vector. Cutting thrust.");
            return;
        }

        foreach (var engine in Engines)
            engine.PackedEngineUpdate();

        var thrust = provider.GetVesselThrust(this, now);
        if (thrust == Vector3d.zero)
            return;

        // Make sure that the vessel is pointing in the target direction.
        RotateToOrientation(target.Orientation);
        Config.OnTargetHeadingUpdate.Fire(this, target.Orientation);

        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = now,
            StartMass = lastUpdateMass,
            StopMass = vessel.totalMass,
            Thrust = thrust,
        };

        Thrust = thrust;
        TargetHeading.IntegrateThrust(this, parameters);
    }
    #endregion

    #region Unloaded Vessel Handling
    void BackgroundFixedUpdate() => BackgroundFixedUpdate(Planetarium.GetUniversalTime());

    public void BackgroundFixedUpdate(double UT)
    {
        if (vessel.loaded)
            return;

        var provider = Config.VesselInfoProvider;
        if (!provider.AllowBackground || TargetHeading is null)
        {
            // If we aren't set up to run any updates then we disable ourselves
            // to avoid extra overhead.
            enabled = false;
            return;
        }

        if (UT <= LastUpdateTime)
            return;

        var lastUpdateTime = LastUpdateTime;
        var lastUpdateMass = LastUpdateMass;

        LastUpdateTime = UT;

        var deltaT = UT - lastUpdateTime;
        if (deltaT <= 0.0)
            return;

        var currentMass = provider.GetVesselMass(this, UT);
        LastUpdateTime = UT;
        LastUpdateMass = currentMass;

        if (currentMass <= 0.0)
        {
            LastUpdateMass = lastUpdateMass;
            SetThrottle(0.0, UT);
            return;
        }

        var target = TargetHeading.GetTargetHeading(UT);

        // Protect against invalid heading vectors before they cause the vessel
        // to get deleted because its state is NaN.
        if (!target.IsValid())
        {
            var tname = TargetHeading.GetType().Name;
            var q = target.Orientation;
            var mag2 = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;

            if (double.IsInfinity(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned infinite quaternion");
            else if (double.IsNaN(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned NaN quaternion");
            else
                LogUtil.Error($"{tname}.GetTargetHeading returned zero quaternion");

            SetThrottle(0.0);
            SetTargetHeading(null);
            return;
        }

        // Make sure that the vessel is pointing in the target direction.
        RotateToOrientation(target.Orientation);
        Config.OnBackgroundTargetHeadingUpdate.Fire(this, target.Orientation);

        var thrust = provider.GetVesselThrust(this, UT);
        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = UT,
            StartMass = LastUpdateMass,
            StopMass = currentMass,
            Thrust = thrust,
        };

        if (thrust == Vector3d.zero)
        {
            if (throttle == 0.0)
                // Zero thrust and zero throttle means we disable ourselves
                // unconditionally.
                //
                // This may cause issues if there are resource-starved engines
                // with an independent throttle but if we don't do this then
                // we can potentially end up background vessels running extra
                // FixedUpdates unecessarily.
                enabled = false;
            else if (provider.DisableOnZeroThrustInBackground)
                enabled = false;
            return;
        }

        Thrust = thrust;
        TargetHeading.IntegrateThrust(this, parameters);
    }
    #endregion

    #region Helpers
    // This is its own method so that it can be patched in the future if needed.
    private void RotateToOrientation(Quaternion target)
    {
        vessel.SetRotation(target);
    }

    private IEnumerator DelayPreserveThrottle(float throttle, int frames = 1)
    {
        for (int i = 0; i < frames; ++i)
            yield return new WaitForEndOfFrame();

        vessel.ctrlState?.mainThrottle = throttle;
    }

    private TargetHeadingProvider GetNewHeadingProvider()
    {
        var provider = Config.GetTargetHeading(this);
        provider?.Vessel = Vessel;
        return provider;
    }

    public FixedHeading GetFixedHeading()
    {
        return new(vessel.ReferenceTransform.rotation) { Vessel = vessel };
    }

    public double ComputeDryMass()
    {
        if (vessel is null)
            return 0.0;

        var mass = 0.0;
        foreach (var part in vessel.parts)
            mass += part.mass;

        return mass;
    }
    #endregion

    #region Event Handlers
    protected override void OnStart()
    {
        if (LastUpdateTime == 0.0)
        {
            LastUpdateTime = Planetarium.GetUniversalTime();
            LastUpdateMass = vessel.totalMass;
        }

        if (Throttle == 0.0)
            return;

        // If we are actively thrusting towards a heading then rotate the vessel
        // to point that way.
        var now = Planetarium.GetUniversalTime();
        if (TargetHeading?.GetTargetHeading(now) is TargetHeading target && target.IsValid())
        {
            // Make sure that the vessel is pointing in the target direction.
            vessel.SetRotation(target.Orientation);
        }
    }

    public override void OnGoOnRails()
    {
        TargetHeading ??= GetNewHeadingProvider();
        if (vessel.IsOrbiting())
            StartCoroutine(DelayPreserveThrottle(vessel.ctrlState.mainThrottle));
    }

    public override void OnGoOffRails()
    {
        SetTargetHeading(null);

        var ctrlState = vessel.setControlStates[Vessel.GroupOverride];
        ctrlState.mainThrottle = vessel.ctrlState.mainThrottle;
    }

    public override void OnLoadVessel()
    {
        DryMass = null;
    }

    public override void OnUnloadVessel()
    {
        DryMass = ComputeDryMass();
    }

    protected override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        ConfigNode child = null;
        if (node.TryGetNode("TARGET_HEADING", ref child))
            TargetHeading = TargetHeadingProvider.Load(vessel, child);

        float throttle = 0f;
        if (node.TryGetValue("throttle", ref throttle))
            vessel?.ctrlState?.mainThrottle = throttle;

        double dryMass = 0.0;
        if (node.TryGetValue(nameof(DryMass), ref dryMass))
            DryMass = dryMass;
    }

    protected override void OnSave(ConfigNode node)
    {
        if (vessel.loaded && TargetHeading is null)
            RefreshTargetHeading();

        base.OnSave(node);

        TargetHeading?.Save(node.AddNode("TARGET_HEADING"));

        if (vessel?.ctrlState?.mainThrottle is float throttle)
            node.AddValue("throttle", throttle);

        if (vessel.loaded)
            DryMass = ComputeDryMass();
        if (DryMass is double dryMass)
            node.AddValue(nameof(DryMass), dryMass);
    }
    #endregion
}
