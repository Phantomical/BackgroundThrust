using System;
using BackgroundResourceProcessing;
using UnityEngine;

namespace BackgroundThrust.Integration.BRP;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
public class EventDispatcher : MonoBehaviour
{
    static bool InChangepointCallback = false;

    void Awake()
    {
        Config.BackgroundProcessing = true;
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
    }

    void OnVesselChangepoint(BackgroundResourceProcessor processor, ChangepointEvent evt)
    {
        using var guard = new InChangepointCallbackGuard();

        double thrust = 0.0;
        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;

            thrust += engine.Thrust * converter.Rate;
        }

        var vessel = processor.Vessel;
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();
        var mass = processor.GetWetMass();
        var dryMass = module.DryMass ?? vessel.totalMass;

        module.BackgroundFixedUpdate(evt.CurrentChangepoint);
        module.LastUpdateMass = dryMass + mass.amount;
        module.MassChangeRate = mass.rate;
        module.SetThrust(thrust, evt.CurrentChangepoint);
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

    readonly struct InChangepointCallbackGuard : IDisposable
    {
        public InChangepointCallbackGuard() => InChangepointCallback = true;

        public void Dispose() => InChangepointCallback = false;
    }
}
