using System;
using System.Collections.Generic;
using BackgroundResourceProcessing;
using UnityEngine;
using UnityEngine.Rendering;

namespace BackgroundThrust.Integration.BRP;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
public class EventDispatcher : MonoBehaviour
{
    public struct VesselInfo
    {
        public double WetMass;
        public double WetMassRate;
        public double Thrust;
        public double LastUpdateUT;
    }

    public static EventDispatcher Instance { get; private set; } = null;

    static bool InChangepointCallback = false;

    #region Vessel Info
    readonly Dictionary<Guid, VesselInfo> vesselInfo = [];

    public VesselInfo GetVesselInfo(Vessel vessel)
    {
        if (vesselInfo.TryGetValue(vessel.id, out var info))
            return info;

        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        var mass = processor.GetWetMass();

        double thrust = 0.0;
        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;
            thrust += engine.Thrust;
        }

        info = new()
        {
            Thrust = thrust,
            WetMass = mass.amount,
            WetMassRate = mass.rate,
            LastUpdateUT = processor.LastChangepoint,
        };

        vesselInfo[vessel.id] = info;
        return info;
    }

    #endregion

    #region Event Handlers
    void Awake()
    {
        Config.BackgroundProcessing = true;

        if (Instance is not null)
            throw new InvalidOperationException(
                "Cannot create multiple EventDispatcher instances at once"
            );

        Instance = this;
    }

    void Start()
    {
        BackgroundResourceProcessor.onVesselChangepoint.Add(OnVesselChangepoint);
        BackgroundResourceProcessor.onVesselRecord.Add(OnVesselRecord);
        Config.onUnloadedThrustStarted.Add(OnThrustStarted);
        Config.onUnloadedThrustStopped.Add(OnThrustStopped);
    }

    void OnDestroy()
    {
        BackgroundResourceProcessor.onVesselChangepoint.Remove(OnVesselChangepoint);
        BackgroundResourceProcessor.onVesselRecord.Remove(OnVesselRecord);
        Config.onUnloadedThrustStarted.Remove(OnThrustStarted);
        Config.onUnloadedThrustStopped.Remove(OnThrustStopped);

        Instance = null;
    }

    void OnVesselChangepoint(BackgroundResourceProcessor processor, ChangepointEvent evt)
    {
        using var guard = new InChangepointCallbackGuard();

        var vessel = processor.Vessel;
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();

        module.BackgroundFixedUpdate(evt.CurrentChangepoint);
        vesselInfo.Remove(vessel.id);
    }

    void OnVesselRecord(BackgroundResourceProcessor processor)
    {
        var vessel = processor.Vessel;
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();

        if (module.TargetHeading is not null)
            return;

        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;

            engine.Enabled = false;
        }
    }

    void OnThrustStarted(BackgroundThrustVessel module)
    {
        if (InChangepointCallback)
            return;

        var vessel = module.Vessel;
        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        bool dirty = false;

        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;

            if (!engine.Enabled)
            {
                dirty = true;
                converter.NextChangepoint = Planetarium.GetUniversalTime();
            }
            engine.Enabled = true;
        }

        if (dirty)
            processor.MarkDirty();
    }

    void OnThrustStopped(BackgroundThrustVessel module)
    {
        if (InChangepointCallback)
            return;

        var vessel = module.Vessel;
        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        bool dirty = false;

        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;

            if (engine.Enabled)
            {
                dirty = true;
                converter.NextChangepoint = Planetarium.GetUniversalTime();
            }
            engine.Enabled = false;
        }

        if (dirty)
            processor.MarkDirty();
    }
    #endregion

    readonly struct InChangepointCallbackGuard : IDisposable
    {
        public InChangepointCallbackGuard() => InChangepointCallback = true;

        public void Dispose() => InChangepointCallback = false;
    }
}
