using System;
using System.Collections;
using System.Reflection;
using BackgroundThrust.Patches;
using UnityEngine;

namespace BackgroundThrust;

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

    [KSPField]
    private double RequiredEC = 0.0;

    #region Event Handlers
    public override void OnStart(StartState state)
    {
        if (state == StartState.Editor)
        {
            enabled = false;
            return;
        }

        FindModuleEngines();

        GameEvents.onTimeWarpRateChanged.Add(OnTimeWarpRateChanged);
        GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
    }

    void OnDestroy()
    {
        GameEvents.onTimeWarpRateChanged.Remove(OnTimeWarpRateChanged);
        GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
    }

    internal void OnMultiModeEngineSwitchActive()
    {
        if (MultiModeEngine is null)
            return;

        if (MultiModeEngine.runningPrimary)
            Engine = MultiModeEngine.PrimaryEngine;
        else
            Engine = MultiModeEngine.SecondaryEngine;

        ClearBuffers();
    }

    void OnTimeWarpRateChanged()
    {
        if (vessel.packed)
            UpdateBuffers();
    }

    void OnVesselGoOffRails(Vessel vessel)
    {
        if (vessel != this.vessel)
            return;

        ClearBuffers();
    }
    #endregion

    #region Packed Update
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
            ClearBuffers();
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
    #endregion

    #region Buffer Handling
    struct BufferInfo()
    {
        public double? OriginalMaxAmount = null;
        public double FuelFlow;
        public PartResource Resource;
        public Propellant Propellant;
    }

    BufferInfo[] buffers;

    private BufferInfo[] GetOrCreateBuffers()
    {
        if (this.buffers is not null)
            ClearBuffers();

        if (Engine is null)
            return [];

        var propellants = Engine.propellants;
        var buffers = new BufferInfo[propellants.Count];

        // Rebuilding resource sets is a fairly expensive operation.
        using var guard = DisableUpdateResourcesOnEvent();

        int index = 0;
        foreach (var propellant in propellants)
        {
            var resource = part.Resources[propellant.name];
            var info = new BufferInfo()
            {
                FuelFlow = Engine.getMaxFuelFlow(propellant),
                Resource = resource,
                Propellant = propellant,
                OriginalMaxAmount = resource?.maxAmount,
            };

            if (resource is null)
            {
                ConfigNode node = new("RESOURCE");
                node.AddValue("name", propellant.name);
                node.AddValue("maxAmount", 0);
                node.AddValue("amount", 0);
                node.AddValue("isVisible", false);

                info.Resource = part.AddResource(node);
            }

            if (propellant.id == PartResourceLibrary.ElectricityHashcode)
                RequiredEC = info.FuelFlow;

            buffers[index] = info;
            index += 1;
        }

        return buffers;
    }

    public void UpdateBuffers()
    {
        buffers ??= GetOrCreateBuffers();
        double warp = TimeWarp.fixedDeltaTime;

        foreach (var info in buffers)
        {
            var bufferAmount = info.FuelFlow * warp * Config.BufferCapacityMult;

            if (info.OriginalMaxAmount is double amount)
                bufferAmount = Math.Max(amount, bufferAmount);

            info.Resource.maxAmount = bufferAmount;
        }
    }

    public void ClearBuffers()
    {
        RequiredEC = 0.0;

        if (buffers is null)
            return;

        using var guard = DisableUpdateResourcesOnEvent();

        foreach (var buffer in buffers)
        {
            var resource = buffer.Resource;

            if (buffer.OriginalMaxAmount is double maxAmount)
                resource.maxAmount = maxAmount;
            else
                maxAmount = 0.0;

            resource.maxAmount = maxAmount;
            var extra = Math.Max(resource.amount - resource.maxAmount, 0.0);
            if (extra > 0.0)
            {
                resource.amount = maxAmount;

                var transferred = part.RequestResource(
                    buffer.Propellant.id,
                    -extra,
                    buffer.Propellant.GetFlowMode(),
                    simulate: false
                );

                resource.amount += extra - Math.Abs(transferred);
            }

            if (buffer.OriginalMaxAmount is null)
                part.RemoveResource(buffer.Resource);
        }

        buffers = null;
    }

    /// <summary>
    /// Adding/removing a resource from a part causes a vessel to rebuild
    /// all its partsets. This is ok if done once, but horrendously expensive
    /// if done repeatedly for every propellant on every active engine.
    ///
    /// This method disables that for the lifetime of the returned guard and
    /// starts up a coroutine that will do a single update of the resource sets
    /// at the end of the fixed update.
    /// </summary>
    /// <returns></returns>
    private DisableUpdateResourcesOnEventGuard DisableUpdateResourcesOnEvent()
    {
        if (vessel.updateResourcesOnEvent)
            StartCoroutine(DelayedUpdateResourceSets(vessel));
        return new(vessel);
    }

    private IEnumerator DelayedUpdateResourceSets(Vessel vessel)
    {
        yield return new WaitForFixedUpdate();

        if (vessel == null)
            yield break;

        vessel.UpdateResourceSetsIfDirty();
    }

    private readonly struct DisableUpdateResourcesOnEventGuard : IDisposable
    {
        readonly Vessel vessel;
        public readonly bool prev;

        public DisableUpdateResourcesOnEventGuard(Vessel vessel)
        {
            this.vessel = vessel;
            this.prev = vessel.updateResourcesOnEvent;

            vessel.updateResourcesOnEvent = false;
        }

        public void Dispose()
        {
            vessel.updateResourcesOnEvent = prev;
        }
    }
    #endregion

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
}
