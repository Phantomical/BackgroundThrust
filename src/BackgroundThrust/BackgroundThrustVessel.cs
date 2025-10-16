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
                Config.onBackgroundThrottleChanged.Fire(new(this, prev, throttle));
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
            Config.onHeadingChanged.Fire(new(this, prev, heading));
    }
    #endregion

    #region FixedUpdate for unpacked vessels
    void UnpackedFixedUpdate()
    {
        LastUpdateTime = Planetarium.GetUniversalTime();
        LastUpdateMass = vessel.totalMass;
        throttle = vessel.ctrlState.mainThrottle;
    }
    #endregion

    #region Packed Vessel Handling
    void PackedFixedUpdate()
    {
        throttle = vessel.ctrlState.mainThrottle;

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

        if (TargetHeading?.GetTargetHeading(now) is not Vector3d target)
        {
            SetThrottle(0.0);
            SetTargetHeading(null);
            TimeWarp.SetRate(0, instant: true);
            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            return;
        }

        // Protect against invalid heading vectors before they cause the vessel
        // to get deleted because its state is NaN.
        var mag2 = target.sqrMagnitude;
        if (mag2 == 0.0 || double.IsInfinity(mag2) || double.IsNaN(mag2))
        {
            var tname = TargetHeading.GetType().Name;

            if (double.IsInfinity(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned infinite heading vector");
            else if (double.IsNaN(mag2))
                LogUtil.Error($"{tname}.GetTargetHeading returned NaN heading vector");
            else
                LogUtil.Error($"{tname}.GetTargetHeading returned zero heading vector");

            SetThrottle(0.0);
            SetTargetHeading(null);
            TimeWarp.SetRate(0, instant: true);
            ScreenMessages.PostScreenMessage("Recieved invalid heading vector. Cutting thrust.");
            return;
        }

        target = target.normalized;

        foreach (var engine in Engines)
            engine.PackedEngineUpdate();

        var thrust = provider.GetVesselThrust(this, now);
        if (thrust == Vector3d.zero)
            return;

        // Make sure that the vessel is pointing in the target direction.
        var heading = Heading;
        vessel.transform.Rotate(
            Quaternion.FromToRotation(heading, (Vector3)target).eulerAngles,
            Space.World
        );
        vessel.SetRotation(vessel.transform.rotation);

        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = now,
            StartMass = lastUpdateMass,
            StopMass = vessel.totalMass,
            Thrust = thrust,
        };

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

        if (TargetHeading.GetTargetHeading(UT) is null)
        {
            SetTargetHeading(null);
            SetThrottle(0.0, UT);
            return;
        }

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

        TargetHeading.IntegrateThrust(this, parameters);
    }
    #endregion

    #region Helpers
    private IEnumerator DelayPreserveThrottle(float throttle, int frames = 1)
    {
        for (int i = 0; i < frames; ++i)
            yield return new WaitForEndOfFrame();

        vessel.ctrlState?.mainThrottle = throttle;
    }

    private TargetHeadingProvider GetNewHeadingProvider()
    {
        var provider = Config.HeadingProvider.GetCurrentHeading(this);
        provider.Vessel = Vessel;
        return provider;
    }

    public FixedHeading GetFixedHeading()
    {
        return new(Heading) { Vessel = vessel };
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
        if (TargetHeading?.GetTargetHeading(now) is Vector3d target)
        {
            // Make sure that the vessel is pointing in the target direction.
            vessel.transform.Rotate(
                Quaternion.FromToRotation(Heading, (Vector3)target).eulerAngles,
                Space.World
            );
            vessel.SetRotation(vessel.transform.rotation);
        }
    }

    public override void OnGoOnRails()
    {
        TargetHeading = GetNewHeadingProvider();
        StartCoroutine(DelayPreserveThrottle(vessel.ctrlState.mainThrottle));
    }

    public override void OnGoOffRails()
    {
        TargetHeading = null;

        var ctrlState = vessel.setControlStates[Vessel.GroupOverride];
        ctrlState.mainThrottle = vessel.ctrlState.mainThrottle;
    }

    internal void OnVesselAutopilotModeChanged(
        VesselAutopilot.AutopilotMode from,
        VesselAutopilot.AutopilotMode to
    )
    {
        if (from == to)
            return;

        SetTargetHeading(GetNewHeadingProvider());
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
        base.OnSave(node);

        TargetHeading ??= GetFixedHeading();
        TargetHeading.Save(node.AddNode("TARGET_HEADING"));

        if (vessel?.ctrlState?.mainThrottle is float throttle)
            node.AddValue("throttle", throttle);

        if (vessel.loaded)
            DryMass = ComputeDryMass();
        if (DryMass is double dryMass)
            node.AddValue(nameof(DryMass), dryMass);
    }
    #endregion
}
