using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BackgroundThrust.Patches;
using BackgroundThrust.Utils;
using UnityEngine;

namespace BackgroundThrust;

public class BackgroundEngine : PartModule
{
    public static bool InPackedUpdate { get; private set; }
    public static Vector3d ThrustAccumulator = Vector3d.zero;

    public MultiModeEngine MultiModeEngine { get; private set; }
    public ModuleEngines Engine { get; private set; }

    [KSPField(isPersistant = true)]
    public Vector3d Thrust;

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

    protected virtual void OnTimeWarpRateChanged()
    {
        if (vessel.packed)
            UpdateBuffers();
    }

    protected virtual void OnVesselGoOffRails(Vessel vessel)
    {
        if (vessel != this.vessel)
            return;

        ClearBuffers();
    }
    #endregion

    #region Packed Update
    public virtual void PackedEngineUpdate()
    {
        if (Engine is null)
        {
            Thrust = Vector3d.zero;
            return;
        }

        if (!IsEnabled)
        {
            Thrust = Vector3d.zero;
            Engine.finalThrust = 0f;
            Engine.DeactivateLoopingFX();
            ClearBuffers();
            return;
        }

        using var guard = GetPackedUpdateGuard();

        // Various patches to ModuleEngines take care of changing the relevant
        // behaviour when InPackedUpdate is set to true.
        Engine.FixedUpdate();
        Thrust = ThrustAccumulator;
    }

    protected PackedUpdateGuard GetPackedUpdateGuard()
    {
        ThrustAccumulator = Vector3d.zero;
        return new();
    }

    protected readonly struct PackedUpdateGuard : IDisposable
    {
        public PackedUpdateGuard()
        {
            InPackedUpdate = true;
        }

        public void Dispose()
        {
            InPackedUpdate = false;
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

    #region Kerbalism Shims
    // The actual code to implement this is in a different assembly. However,
    // we provide it here so that it can be harmony patched.
    //
    // This avoids needing to create a new module type for compatibility with
    // Kerbalism, which would cause issues when adding/removing kerbalism from
    // an install.

    /// <summary>
    /// We're always going to call you for resource handling.  You tell us what to produce or consume.  Here's how it'll look when your vessel is NOT loaded
    /// </summary>
    /// <param name="v">the vessel (unloaded)</param>
    /// <param name="part_snapshot">proto part snapshot (contains all non-persistant KSPFields)</param>
    /// <param name="module_snapshot">proto part module snapshot (contains all non-persistant KSPFields)</param>
    /// <param name="proto_part_module">proto part module snapshot (contains all non-persistant KSPFields)</param>
    /// <param name="proto_part">proto part snapshot (contains all non-persistant KSPFields)</param>
    /// <param name="availableResources">key-value pair containing all available resources and their currently available amount on the vessel. if the resource is not in there, it's not available</param>
    /// <param name="resourceChangeRequest">key-value pair that contains the resource names and the units per second that you want to produce/consume (produce: positive, consume: negative)</param>
    /// <param name="elapsed_s">how much time elapsed since the last time. note this can be very long, minutes and hours depending on warp speed</param>
    /// <returns>the title to be displayed in the resource tooltip</returns>
    public static string BackgroundUpdate(
        Vessel v,
        ProtoPartSnapshot part_snapshot,
        ProtoPartModuleSnapshot module_snapshot,
        PartModule proto_part_module,
        Part proto_part,
        Dictionary<string, double> availableResources,
        List<KeyValuePair<string, double>> resourceChangeRequest,
        double elapsed_s
    )
    {
        return "bt-engine";
    }
    #endregion
}
