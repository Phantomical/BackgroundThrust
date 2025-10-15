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
    private double thrust = 0.0;

    /// <summary>
    /// The current thrust being produced by this vessel.
    /// </summary>
    ///
    /// <remarks>
    /// It will automatically be updated while the ship is loaded, if you are
    /// implementing background processing then use <see cref="SetThrust(double)"/> to
    /// set this while the ship is unloaded.
    /// </remarks>
    public double Thrust => thrust;

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
    public void SetThrust(double thrust) => SetThrust(thrust, Planetarium.GetUniversalTime());

    public void SetThrust(double thrust, double UT)
    {
        var wasZero = this.thrust == 0.0;
        var nowZero = thrust == 0.0;

        if (wasZero)
            LastUpdateTime = Math.Max(LastUpdateTime, UT);

        if (!nowZero && TargetHeading is not null)
            enabled = true;

        this.thrust = thrust;

        if (!vessel.loaded)
        {
            if (wasZero && !nowZero)
                Config.onUnloadedThrustStarted.Fire(this);
            else if (!wasZero && nowZero)
                Config.onUnloadedThrustStopped.Fire(this);
        }
    }

    public void SetTargetHeading(TargetHeadingProvider heading) =>
        SetTargetHeading(heading, Planetarium.GetUniversalTime());

    public void SetTargetHeading(TargetHeadingProvider heading, double UT)
    {
        if (TargetHeading is null)
            LastUpdateTime = Math.Max(LastUpdateTime, UT);

        enabled = true;
        heading.Vessel = Vessel;
        TargetHeading = heading;
    }
    #endregion

    #region FixedUpdate for unpacked vessels
    void UnpackedFixedUpdate()
    {
        LastUpdateTime = Planetarium.GetUniversalTime();
        LastUpdateMass = vessel.totalMass;
    }
    #endregion

    #region Packed Vessel Handling
    void PackedFixedUpdate()
    {
        if (vessel.ctrlState.mainThrottle == 0f)
            return;

        var now = Planetarium.GetUniversalTime();
        var lastUpdateTime = LastUpdateTime;
        var lastUpdateMass = LastUpdateMass;
        var currentMass = Config.VesselInfoProvider.GetVesselMass(this, now);

        LastUpdateTime = now;
        LastUpdateMass = currentMass;

        if (lastUpdateTime == 0.0)
            return;

        TargetHeading ??= GetFixedHeading();

        if (TargetHeading.GetTargetHeading(now) is not Vector3d target)
        {
            PackedCutThrottle();
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

            PackedCutThrottle();
            ScreenMessages.PostScreenMessage("Recieved invalid heading vector. Cutting thrust.");
            return;
        }

        // Make sure that the vessel is pointing in the target direction.
        vessel.transform.Rotate(
            Quaternion.FromToRotation(vessel.transform.up, (Vector3)target).eulerAngles,
            Space.World
        );
        vessel.SetRotation(vessel.transform.rotation);

        foreach (var engine in Engines)
            engine.PackedEngineUpdate();

        var thrust = Config.VesselInfoProvider.GetVesselThrust(this, now);
        SetThrust(thrust, now);

        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = now,
            StartMass = lastUpdateMass,
            StopMass = vessel.totalMass,
            Thrust = Thrust,
        };

        TargetHeading.IntegrateThrust(this, parameters);
        LastUpdateTime = now;
    }

    private void PackedCutThrottle()
    {
        vessel.ctrlState.mainThrottle = 0f;
        TargetHeading = null;
        TimeWarp.SetRate(0, instant: true);
    }
    #endregion

    #region Unloaded Vessel Handling
    void BackgroundFixedUpdate() => BackgroundFixedUpdate(Planetarium.GetUniversalTime());

    public void BackgroundFixedUpdate(double UT)
    {
        if (vessel.loaded)
            return;

        if (!Config.VesselInfoProvider.AllowBackground || Thrust == 0.0 || TargetHeading is null)
        {
            // There's no point running fixed updates if thrust is 0, so we
            // disable ourselves here.
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

        var currentMass = Config.VesselInfoProvider.GetVesselMass(this, UT);
        LastUpdateTime = UT;
        LastUpdateMass = currentMass;

        if (currentMass <= 0.0)
        {
            LastUpdateMass = lastUpdateMass;
            SetThrust(0.0, UT);
            return;
        }

        if (TargetHeading.GetTargetHeading(UT) is null)
        {
            TargetHeading = null;
            SetThrust(0.0, UT);
            return;
        }

        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = UT,
            StartMass = LastUpdateMass,
            StopMass = currentMass,
            Thrust = Thrust,
        };

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
        return new(vessel.transform.up) { Vessel = vessel };
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

        if (Thrust == 0.0)
            return;

        // If we are actively thrusting towards a heading then rotate the vessel
        // to point that way.
        var now = Planetarium.GetUniversalTime();
        if (TargetHeading?.GetTargetHeading(now) is Vector3d target)
        {
            // Make sure that the vessel is pointing in the target direction.
            vessel.transform.Rotate(
                Quaternion.FromToRotation(vessel.transform.up, (Vector3)target).eulerAngles,
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

        TargetHeading = GetNewHeadingProvider();
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
