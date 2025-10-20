using System;
using System.Collections.Generic;
using BackgroundResourceProcessing;
using BackgroundThrust.Heading;
using UnityEngine;
using UnityEngine.Rendering;
using VehiclePhysics;

namespace BackgroundThrust.BRP;

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

    #region Vessel Info
    readonly Dictionary<Guid, VesselInfo> vesselInfo = [];

    public VesselInfo GetVesselInfo(Vessel vessel)
    {
        if (vesselInfo.TryGetValue(vessel.id, out var info))
            return info;

        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        var mass = processor.GetWetMass();
        var module = GetVesselModule(vessel);
        var throttle = module.Throttle;

        double thrust = 0.0;
        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour engine)
                continue;
            thrust += engine.MaxThrust * (engine.Throttle ?? throttle) * converter.Rate;
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

    #region Vessel Module Cache
    readonly Dictionary<Guid, BackgroundThrustVessel> vesselModules = [];

    public BackgroundThrustVessel GetVesselModule(Vessel v)
    {
        if (vesselModules.TryGetValue(v.id, out var module))
            return module;

        module = v.GetBackgroundThrust();
        vesselModules[v.id] = module;
        return module;
    }
    #endregion

    #region Event Handlers
    void Awake()
    {
        if (Instance is not null)
            throw new InvalidOperationException(
                "Cannot create multiple EventDispatcher instances at once"
            );

        Instance = this;
    }

    void Start()
    {
        BackgroundResourceProcessor.onVesselChangepoint.Add(OnVesselChangepoint);
        Config.OnBackgroundThrottleChanged.Add(OnBackgroundThrottleChanged);
        Config.OnTargetHeadingProviderChanged.Add(OnHeadingChanged);
        GameEvents.onVesselDestroy.Add(OnVesselDestroy);
    }

    void OnDestroy()
    {
        BackgroundResourceProcessor.onVesselChangepoint.Remove(OnVesselChangepoint);
        Config.OnBackgroundThrottleChanged.Add(OnBackgroundThrottleChanged);
        Config.OnTargetHeadingProviderChanged.Add(OnHeadingChanged);
        GameEvents.onVesselDestroy.Remove(OnVesselDestroy);

        Instance = null;
    }

    void OnVesselChangepoint(BackgroundResourceProcessor processor, ChangepointEvent evt)
    {
        var vessel = processor.Vessel;
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();

        module.BackgroundFixedUpdate(evt.CurrentChangepoint);
        vesselInfo.Remove(vessel.id);
    }

    void OnBackgroundThrottleChanged(
        GameEvents.HostedFromToAction<BackgroundThrustVessel, double> evt
    )
    {
        var vessel = evt.host.Vessel;
        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        var dirty = false;

        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour)
                continue;

            converter.NextChangepoint = Planetarium.GetUniversalTime();
            dirty = true;
        }

        if (dirty)
            processor.MarkDirty();
    }

    void OnHeadingChanged(
        GameEvents.HostedFromToAction<BackgroundThrustVessel, TargetHeadingProvider> evt
    )
    {
        var vessel = evt.host.Vessel;
        if (vessel.loaded)
            return;

        // We only care about changing from null -> not null and vice versa
        if (evt.from is null == evt.to is null)
            return;

        var now = Planetarium.GetUniversalTime();
        var processor = vessel.FindVesselModuleImplementing<BackgroundResourceProcessor>();
        var dirty = false;

        foreach (var converter in processor.Converters)
        {
            if (converter.Behaviour is not BackgroundEngineBehaviour)
                continue;

            converter.NextChangepoint = now;
            dirty = true;
        }

        if (dirty)
            processor.MarkDirty();
    }

    void OnVesselDestroy(Vessel vessel)
    {
        vesselModules.Remove(vessel.id);
        vesselInfo.Remove(vessel.id);
    }
    #endregion
}
