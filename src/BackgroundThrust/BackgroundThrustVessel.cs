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

    public TargetHeadingProvider TargetHeadingProvider;

    private List<BackgroundEngine> _engines = null;
    public List<BackgroundEngine> Engines
    {
        get => _engines ??= vessel?.FindPartModulesImplementing<BackgroundEngine>();
        set => _engines = value;
    }

    protected override void OnStart()
    {
        if (LastUpdateTime == 0.0)
            LastUpdateTime = Planetarium.GetUniversalTime();
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
    }

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

        TargetHeadingProvider ??= new FixedHeading(vessel.transform.up);

        var ntarget = TargetHeadingProvider.GetTargetHeading(this, now);
        if (ntarget is null)
        {
            vessel.ctrlState.mainThrottle = 0f;

            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            TimeWarp.SetRate(0, instant: true);
            return;
        }

        var target = (Vector3d)ntarget;
        var deltaT = now - lastUpdateTime;

        double thrust = 0.0;
        foreach (var engine in Engines)
        {
            engine.PackedEngineUpdate();
            thrust += engine.Thrust;
        }
        Thrust = thrust;

        // Make sure that the vessel is pointing in the target direction.
        vessel.transform.rotation *= Quaternion.FromToRotation(
            vessel.transform.up,
            (Vector3)target
        );

        var parameters = new ThrustParameters
        {
            StartUT = lastUpdateTime,
            StopUT = now,
            StartMass = lastUpdateMass,
            StopMass = vessel.totalMass,
            Thrust = Thrust,
        };

        TargetHeadingProvider.IntegrateThrust(this, parameters);
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

    private IEnumerator DelayPreserveThrottle(float throttle)
    {
        yield return new WaitForEndOfFrame();

        vessel.ctrlState?.mainThrottle = throttle;
    }

    public override void OnGoOnRails()
    {
        TargetHeadingProvider ??= new FixedHeading(vessel.transform.up);
        StartCoroutine(DelayPreserveThrottle(vessel.ctrlState.mainThrottle));
    }

    protected override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        ConfigNode child = null;
        if (node.TryGetNode("TARGET_HEADING", ref child))
            TargetHeadingProvider = TargetHeadingProvider.Load(node);
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        TargetHeadingProvider?.Save(node.AddNode("TARGET_HEADING"));
    }
}
