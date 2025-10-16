using System;
using UnityEngine;

namespace BackgroundThrust;

[KSPAddon(KSPAddon.Startup.AllGameScenes, once: false)]
internal class EventDispatcher : MonoBehaviour
{
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
        Config.onAutopilotModeChange.Add(OnVesselAutopilotModeChanged);
    }

    void OnDestroy()
    {
        GameEvents.onVesselWasModified.Remove(OnVesselPartCountChanged);
        GameEvents.onMultiModeEngineSwitchActive.Remove(OnMultiModeEngineSwitchActive);
        Config.onAutopilotModeChange.Remove(OnVesselAutopilotModeChanged);
    }

    void OnVesselPartCountChanged(Vessel vessel)
    {
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();

        // Avoid proactively rescanning all vessel modules in onVesselWasModified
        // since that callback tends to be quite slow in KSP already.
        module.Engines = null;
    }

    void OnVesselAutopilotModeChanged(
        GameEvents.HostedFromToAction<Vessel, VesselAutopilot.AutopilotMode> evt
    )
    {
        var vessel = evt.host;
        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();

        module.OnVesselAutopilotModeChanged(evt.from, evt.to);
    }

    void OnMultiModeEngineSwitchActive(MultiModeEngine engine)
    {
        var module = engine.part.FindModuleImplementing<BackgroundEngine>();
        if (module is null)
            return;

        module.OnMultiModeEngineSwitchActive();
    }

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
}
