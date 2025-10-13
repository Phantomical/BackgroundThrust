using System.Collections;
using System.Collections.Generic;
using BackgroundThrust.Heading;
using BackgroundThrust.Utils;
using UnityEngine;

namespace BackgroundThrust;

public class BackgroundThrustVessel : VesselModule
{
    [KSPField(isPersistant = true)]
    public double LastUpdateTime = 0.0;

    [KSPField(isPersistant = true)]
    public double LastUpdateMass = 0.0;

    [KSPField(isPersistant = true)]
    public double Thrust = 0.0;

    public TargetHeadingProvider TargetHeading;

    private List<BackgroundEngine> _engines = null;
    public List<BackgroundEngine> Engines
    {
        get => _engines ??= vessel?.FindPartModulesImplementing<BackgroundEngine>();
        set => _engines = value;
    }

    protected override void OnStart()
    {
        if (LastUpdateTime == 0.0)
        {
            LastUpdateTime = Planetarium.GetUniversalTime();
            LastUpdateMass = vessel.totalMass;
        }
    }

    void FixedUpdate()
    {
        if (!vessel.loaded)
        {
            BackgroundFixedUpdate();
            return;
        }

        if (vessel.packed)
            PackedFixedUpdate();
        else
            UnpackedFixedUpdate();
    }

    #region FixedUpdate for unpacked vessels
    void UnpackedFixedUpdate()
    {
        LastUpdateTime = Planetarium.GetUniversalTime();
        LastUpdateMass = vessel.totalMass;
    }
    #endregion

    #region FixedUpdate for packed vessels
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

        TargetHeading ??= new FixedHeading(vessel.transform.up);

        var ntarget = TargetHeading.GetTargetHeading(this, now);
        if (ntarget is null)
        {
            vessel.ctrlState.mainThrottle = 0f;

            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            TimeWarp.SetRate(0, instant: true);
            return;
        }

        double thrust = 0.0;
        foreach (var engine in Engines)
        {
            engine.PackedEngineUpdate();
            thrust += engine.Thrust;
        }
        Thrust = thrust;

        var target = (Vector3d)ntarget;

        // Make sure that the vessel is pointing in the target direction.
        vessel.transform.Rotate(
            Quaternion.FromToRotation(vessel.transform.up, (Vector3)target).eulerAngles,
            Space.World
        );
        vessel.SetRotation(vessel.transform.rotation);

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

    #region FixedUpdate for unloaded vessels
    void BackgroundFixedUpdate()
    {
        // if (Thrust == 0.0)
        // {
        //     // There's no point running fixed updates if thrust is 0, so we
        //     // disable ourselves here.
        //     enabled = false;
        //     return;
        // }

        // if (TargetHeadingProvider is null)
        //     return;

        // var now = Planetarium.GetUniversalTime();
        // if (Config.UnloadedResourceProcessing)
        //     BackgroundEngineUpdate(LastUpdateTime, now);

        // TargetHeadingProvider?.IntegrateThrust(this, LastUpdateTime, now);
        // LastUpdateTime = now;
    }

    void BackgroundEngineUpdate(double startUT, double endUT) { }
    #endregion

    private IEnumerator DelayPreserveThrottle(float throttle, int frames = 1)
    {
        for (int i = 0; i < frames; ++i)
            yield return new WaitForEndOfFrame();

        vessel.ctrlState?.mainThrottle = throttle;
    }

    #region Event Handlers
    public override void OnGoOnRails()
    {
        TargetHeading = Config.HeadingProvider.GetCurrentHeading(this);
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

        TargetHeading = Config.HeadingProvider.GetCurrentHeading(this);
    }

    protected override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        ConfigNode child = null;
        if (node.TryGetNode("TARGET_HEADING", ref child))
            TargetHeading = TargetHeadingProvider.Load(node);
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        TargetHeading?.Save(node.AddNode("TARGET_HEADING"));
    }
    #endregion
}
