using System.Collections;
using System.Collections.Generic;
using BackgroundThrust.Heading;
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
    /// implementing background processing then use <see cref="SetThrust"/> to
    /// set this while the ship is unloaded.
    /// </remarks>
    public double Thrust => thrust;

    /// <summary>
    /// The rate at which the vessel mass is changing. This is used by
    /// background processing and needs to be set appropriately by
    /// whichever implementation of background processing is available.
    /// </summary>
    public double? MassChangeRate;

    /// <summary>
    /// The dry mass of the vessel. This is only used for background processing
    /// and is not updated when the module is loaded.
    /// </summary>
    public double? DryMass;

    public TargetHeadingProvider TargetHeading;

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
    public void SetThrust(double thrust)
    {
        if (this.thrust == 0.0)
            LastUpdateTime = Planetarium.GetUniversalTime();

        if (thrust > 0.0 && TargetHeading is not null && enabled)
            enabled = true;

        this.thrust = thrust;
    }
    #endregion

    #region FixedUpdate for unpacked vessels
    void UnpackedFixedUpdate()
    {
        LastUpdateTime = Planetarium.GetUniversalTime();
        LastUpdateMass = vessel.totalMass;
        MassChangeRate = null;
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
        LastUpdateTime = now;
        LastUpdateMass = vessel.totalMass;

        if (lastUpdateTime == 0.0)
            return;

        TargetHeading ??= new FixedHeading(vessel.transform.up) { Vessel = vessel };

        if (TargetHeading.GetTargetHeading(this, now) is not Vector3d target)
        {
            vessel.ctrlState.mainThrottle = 0f;

            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            TimeWarp.SetRate(0, instant: true);
            return;
        }

        // Make sure that the vessel is pointing in the target direction.
        vessel.transform.Rotate(
            Quaternion.FromToRotation(vessel.transform.up, (Vector3)target).eulerAngles,
            Space.World
        );
        vessel.SetRotation(vessel.transform.rotation);

        double thrust = 0.0;
        foreach (var engine in Engines)
        {
            engine.PackedEngineUpdate();
            thrust += engine.Thrust;
        }
        this.thrust = thrust;

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
    #endregion

    #region Unloaded Vessel Handling
    void BackgroundFixedUpdate() => BackgroundFixedUpdate(Planetarium.GetUniversalTime());

    public void BackgroundFixedUpdate(double UT)
    {
        if (vessel.loaded)
            return;

        if (!Config.BackgroundProcessing || Thrust == 0.0 || TargetHeading is null)
        {
            // There's no point running fixed updates if thrust is 0, so we
            // disable ourselves here.
            enabled = false;
            return;
        }

        var lastUpdateTime = LastUpdateTime;
        var lastUpdateMass = LastUpdateMass;
        LastUpdateTime = UT;

        var deltaT = UT - LastUpdateTime;
        if (deltaT <= 0.0)
            return;
        if (MassChangeRate is not double rate)
            return;

        var deltaM = rate * deltaT;
        var currentMass = lastUpdateMass + deltaM;
        var dryMass = DryMass ?? 0.0;

        LastUpdateMass = currentMass;

        if (currentMass <= dryMass)
        {
            LastUpdateMass = lastUpdateMass;
            thrust = 0.0;
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

    private double GetVesselDryMass()
    {
        if (vessel == null)
            return 0.0;

        double mass = 0f;
        foreach (var part in vessel.parts)
            mass += part.mass;

        return mass;
    }
    #endregion

    #region Event Handlers
    protected override void OnStart()
    {
        if (vessel.loaded)
        {
            MassChangeRate = null;
            DryMass = null;
        }

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
        if (TargetHeading?.GetTargetHeading(this, now) is Vector3d target)
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
        // This is only used for background processing and needs to be set
        // by whatever system is doing background processing.
        MassChangeRate = null;
        DryMass = null;
    }

    public override void OnUnloadVessel()
    {
        DryMass ??= GetVesselDryMass();
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

        double massChangeRate = 0.0;
        if (node.TryGetValue(nameof(MassChangeRate), ref massChangeRate))
            MassChangeRate = massChangeRate;
        double dryMass = 0.0;
        if (node.TryGetValue(nameof(DryMass), ref dryMass))
            DryMass = dryMass;
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        if (vessel.loaded)
            DryMass ??= GetVesselDryMass();

        TargetHeading?.Save(node.AddNode("TARGET_HEADING"));

        if (vessel?.ctrlState?.mainThrottle is float throttle)
            node.AddValue("throttle", throttle);
        if (MassChangeRate is double massChangeRate)
            node.AddValue(nameof(MassChangeRate), massChangeRate);

        double? dryMass = DryMass;
        if (vessel.loaded)
            dryMass ??= GetVesselDryMass();
        if (dryMass is double mass)
            node.AddValue(nameof(DryMass), mass);
    }
    #endregion
}
