using System;
using System.Collections.Generic;
using BackgroundThrust.Heading;
using UnityEngine;
using static GameEvents;

namespace BackgroundThrust;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
internal class EventDispatcher : MonoBehaviour
{
    internal static EventDispatcher Instance { get; private set; }

    #region VesselModule Caching
    readonly Dictionary<Guid, BackgroundThrustVessel> modules = [];

    internal BackgroundThrustVessel GetVesselModule(Vessel v)
    {
        if (!modules.TryGetValue(v.id, out var module))
        {
            module = v.FindVesselModuleImplementing<BackgroundThrustVessel>();
            modules[v.id] = module;
        }

        return module;
    }
    #endregion

    #region PartModule Caching
    readonly Dictionary<Part, BackgroundEngine> engines = [];

    internal BackgroundEngine GetBackgroundEngine(Part p)
    {
        if (!engines.TryGetValue(p, out var engine))
        {
            engine = p.FindModuleImplementing<BackgroundEngine>();
            engines[p] = engine;
        }

        return engine;
    }
    #endregion


    #region Event Handlers
    void Awake()
    {
        if (HighLogic.LoadedSceneIsEditor)
        {
            enabled = false;
            Destroy(this);
        }
    }

    void Start()
    {
        GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
        GameEvents.onMultiModeEngineSwitchActive.Add(OnMultiModeEngineSwitchActive);
        GameEvents.onVesselUnloaded.Add(OnVesselUnloaded);
        GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        GameEvents.onEngineThrustPercentageChanged.Add(OnEngineThrustPercentageChanged);
        Config.OnAutopilotModeChange.Add(OnVesselAutopilotModeChanged);
        Config.OnTargetHeadingProviderChanged.Add(OnTargetHeadingProviderChanged);

        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;

        GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
        GameEvents.onMultiModeEngineSwitchActive.Remove(OnMultiModeEngineSwitchActive);
        GameEvents.onVesselUnloaded.Remove(OnVesselUnloaded);
        GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        GameEvents.onEngineThrustPercentageChanged.Remove(OnEngineThrustPercentageChanged);
        Config.OnAutopilotModeChange.Remove(OnVesselAutopilotModeChanged);
        Config.OnTargetHeadingProviderChanged.Remove(OnTargetHeadingProviderChanged);
    }

    void OnVesselPartCountChanged(Vessel vessel)
    {
        var module = vessel.GetBackgroundThrust();

        // Avoid proactively rescanning all vessel modules in onVesselWasModified
        // since that callback tends to be quite slow in KSP already.
        module.Engines = null;

        // We don't want the cache to keep part modules around when it shouldn't.
        engines.Clear();
    }

    void OnVesselAutopilotModeChanged(HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode> evt)
    {
        if (!evt.host.packed || !evt.host.loaded)
            return;

        var module = evt.host.GetBackgroundThrust();
        module.RefreshTargetHeading();
    }

    void OnMultiModeEngineSwitchActive(MultiModeEngine engine)
    {
        var module = engine.part.FindModuleImplementing<BackgroundEngine>();
        if (module is null)
            return;

        module.OnMultiModeEngineSwitchActive();
    }

    void OnTargetHeadingProviderChanged(
        HostedFromToAction<BackgroundThrustVessel, TargetHeadingProvider> evt
    )
    {
        var vessel = evt.host.Vessel;
        if (vessel != FlightGlobals.ActiveVessel)
            return;

        var autopilot = vessel.Autopilot;
        if (evt.to is ISASHeading to)
        {
            var mode = to.Mode;

            if (autopilot.Mode != mode)
                autopilot.SetMode(mode);
        }
        else if (!vessel.packed)
        {
            // do nothing, preserve SAS state when coming out of warp.
        }
        else
        {
            autopilot.Disable();
        }
    }

    void OnVesselDestroy(Vessel vessel)
    {
        modules.Remove(vessel.id);

        if (vessel.loaded)
            engines.Clear();
    }

    void OnVesselUnloaded(Vessel vessel)
    {
        engines.Clear();
    }

    void OnEngineThrustPercentageChanged(ModuleEngines engines)
    {
        var btengine = engines.part.FindModuleImplementing<BackgroundEngine>();
        if (btengine is null)
            return;

        btengine.OnEngineThrustPercentageChanged(engines);
    }
    #endregion

    #region FixedUpdate
    // Even if we clear the input locks then stock doesn't seem to want to
    // allow the throttle to be adjusted. As such, we implement that ourselves.
    void FixedUpdate()
    {
        if (FlightDriver.Pause)
            return;

        var vessel = FlightGlobals.ActiveVessel;
        if (vessel == null)
            return;

        if (!vessel.packed)
            return;

        if (!BackgroundThrustVessel.IsThrustPermitted(vessel))
        {
            vessel.ctrlState.mainThrottle = 0f;
            return;
        }

        double oldMainThrottle = vessel.ctrlState.mainThrottle;
        double throttle = vessel.ctrlState.mainThrottle * 2.0 - 1.0;

        if (InputLockManager.IsUnlocked(ControlTypes.THROTTLE))
        {
            if (GameSettings.THROTTLE_UP.GetKey())
                throttle = Math.Min(1.0, throttle + Time.deltaTime);
            else if (GameSettings.THROTTLE_DOWN.GetKey())
                throttle = Math.Max(-1.0, throttle - Time.deltaTime);

            throttle += UtilMath.Clamp(
                GameSettings.AXIS_THROTTLE_INC.GetAxis() * Time.deltaTime,
                -1.0,
                1.0
            );
        }

        if (InputLockManager.IsUnlocked(ControlTypes.THROTTLE_CUT_MAX))
        {
            if (!GameSettings.MODIFIER_KEY.GetKey())
            {
                if (GameSettings.THROTTLE_CUTOFF.GetKeyDown())
                    throttle = -1.0;
                if (GameSettings.THROTTLE_FULL.GetKeyDown())
                    throttle = 1.0;
            }
        }

        vessel.ctrlState.mainThrottle = (float)(throttle + 1.0) * 0.5f;

        if (Math.Abs(vessel.ctrlState.mainThrottle - oldMainThrottle) > 1e-6)
        {
            Config.OnWarpThrottleChanged.Fire(new(oldMainThrottle, vessel.ctrlState.mainThrottle));
        }
    }
    #endregion
}
