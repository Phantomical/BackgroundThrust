using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BackgroundThrust.Patches;
using UnityEngine;

namespace BackgroundThrust;

public class EngineInfo
{
    /// <summary>
    /// The independent throttle for this engine, if it has one.
    /// </summary>
    public double? Throttle = null;

    // public List<EnginePropellant> Propellants;

    // public EngineInfo(ModuleEngines engine)
    // {
    //     if (engine.independentThrottle)
    //         Throttle =
    // }
}

public class BackgroundEngine : PartModule
{
    const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly FieldInfo IsThrustingField = typeof(ModuleEngines).GetField(
        "isThrusting",
        Instance
    );

    public MultiModeEngine MultiModeEngine { get; private set; }
    public ModuleEngines Engine { get; private set; }

    [KSPField(isPersistant = true)]
    public double Thrust;

    [KSPField(
        isPersistant = true,
        guiActive = true,
        guiActiveEditor = true,
        guiActiveUnfocused = true,
        guiName = "#LOC_BT_BackgroundThrust"
    )]
    [UI_Toggle(
        disabledText = "#autoLOC_900890",
        enabledText = "#autoLOC_900889",
        affectSymCounterparts = UI_Scene.All
    )]
    public bool IsEnabled = true;

    public override void OnStart(StartState state)
    {
        base.OnStart(state);

        if (state == StartState.Editor)
        {
            enabled = false;
            return;
        }

        FindModuleEngines();
    }

    internal void PackedEngineUpdate()
    {
        if (Engine is null)
        {
            Thrust = 0.0;
            return;
        }

        if (!IsEnabled)
        {
            Thrust = 0.0;
            Engine.DeactivateLoopingFX();
            return;
        }

        Engine.UpdateThrottle();
        Engine.currentThrottle = ModuleEngines_Patch.ApplyThrottleAdjustments(
            Engine,
            Engine.currentThrottle
        );
        if (Engine.EngineIgnited)
            ModuleEngines_Patch.UpdatePropellantStatus(Engine);
        PackedThrustUpdate();
        Engine.FXUpdate();
    }

    private void PackedThrustUpdate()
    {
        var engine = Engine;

        if (!engine.EngineIgnited)
        {
            ModuleEngines_Patch.ThrustUpdate(engine);
            return;
        }

        if (Config.LoadedResourceProcessing)
            engine.finalThrust = engine.CalculateThrust() * vessel.VesselValues.EnginePower.value;
        if (engine.finalThrust > 0f)
        {
            IsThrustingField.SetValue(engine, true);

            double thrust = 0.0;
            var up = vessel.transform.up;

            int count = engine.thrustTransforms.Count;
            for (int i = 0; i < count; ++i)
            {
                var transform = engine.thrustTransforms[i];
                var mult = engine.thrustTransformMultipliers[i];

                var force = -transform.forward * engine.finalThrust * mult;
                // We only take into account thrust applied along the vessel direction
                thrust += Vector3d.Dot(force, up);
            }

            double kilowatts =
                engine.heatProduction
                * (engine.finalThrust / engine.maxThrust)
                * vessel.VesselValues.HeatProduction.value
                * PhysicsGlobals.InternalHeatProductionFactor
                * part.thermalMass;
            if (engine.normalizeHeatForFlow)
                kilowatts /= engine.flowMultiplier;

            part.AddThermalFlux(kilowatts);
            Thrust = thrust;
        }
        else
        {
            IsThrustingField.SetValue(engine, false);
            Thrust = 0.0;
        }
    }

    private void FindModuleEngines()
    {
        MultiModeEngine = part.FindModuleImplementing<MultiModeEngine>();
        if (MultiModeEngine is not null)
        {
            if (MultiModeEngine.runningPrimary)
                Engine = MultiModeEngine.PrimaryEngine;
            else
                Engine = MultiModeEngine.SecondaryEngine;
        }
        else
        {
            Engine = part.FindModuleImplementing<ModuleEngines>();
        }
    }

    /// <summary>
    /// Get the list of resources that would be requested by this engine at the
    /// provided throttle.
    /// </summary>
    /// <param name="throttle"></param>
    /// <returns></returns>
    public ResourceRequest[] GetEngineFuelRequests(double throttle)
    {
        if (throttle <= 0.0 || Engine is null)
            return [];

        double mass = GetRequiredPropellantMass(Engine, (float)throttle);
        double required = mass * Engine.mixtureDensityRecip;
        if (required <= 0.0)
            return [];

        var requests = new ResourceRequest[Engine.propellants.Count];
        for (int i = 0; i < requests.Length; ++i)
        {
            var propellant = Engine.propellants[i];

            var request = new ResourceRequest
            {
                ResourceId = propellant.resourceDef.id,
                Amount = propellant.ratio * required,
                FlowMode = propellant.GetFlowMode(),
            };

            requests[i] = request;
        }

        return requests;
    }

    private static double GetRequiredPropellantMass(ModuleEngines engine, float throttle) =>
        ModuleEngines_Patch.RequiredPropellantMass(engine, throttle);

    [DebuggerDisplay("{ResourceName} {Amount}")]
    public struct ResourceRequest
    {
        public int ResourceId;
        public double Amount;
        public ResourceFlowMode FlowMode;

        // Available for debugging only.
        private readonly string ResourceName =>
            PartResourceLibrary.Instance?.GetDefinition(ResourceId)?.name;
    }
}
