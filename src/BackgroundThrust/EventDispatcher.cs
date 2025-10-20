using System;
using System.Collections.Generic;
using BackgroundThrust.Utils;
using UnityEngine;

namespace BackgroundThrust;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
internal class EventDispatcher : MonoBehaviour
{
    internal static EventDispatcher Instance;

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
        GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        Config.OnAutopilotModeChange.Add(OnVesselAutopilotModeChanged);

        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;

        GameEvents.onVesselWasModified.Remove(OnVesselPartCountChanged);
        GameEvents.onMultiModeEngineSwitchActive.Remove(OnMultiModeEngineSwitchActive);
        GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        Config.OnAutopilotModeChange.Remove(OnVesselAutopilotModeChanged);
    }

    void OnVesselPartCountChanged(Vessel vessel)
    {
        var module = vessel.GetBackgroundThrust();

        // Avoid proactively rescanning all vessel modules in onVesselWasModified
        // since that callback tends to be quite slow in KSP already.
        module.Engines = null;
    }

    void OnVesselAutopilotModeChanged(
        GameEvents.HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode> evt
    )
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

    void OnVesselDestroy(Vessel vessel)
    {
        modules.Remove(vessel.id);
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

        if (!vessel.IsOrbiting())
        {
            vessel.ctrlState.mainThrottle = 0f;
            return;
        }

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
    }
    #endregion
}
