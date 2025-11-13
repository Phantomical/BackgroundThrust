using System;
using System.Collections;
using System.Collections.Generic;
using BackgroundThrust.Utils;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace BackgroundThrust;

public class BackgroundEngine : PartModule
{
    public MultiModeEngine MultiModeEngine { get; private set; }
    public ModuleEngines Engine { get; private set; }

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
    public bool AllowBackgroundProcessing = true;

    #region Event Handlers
    public override void OnStart(StartState state)
    {
        if (state == StartState.Editor)
        {
            this.enabled = false;
            this.isEnabled = false;
            return;
        }

        FindModuleEngines();

        GameEvents.onTimeWarpRateChanged.Add(OnTimeWarpRateChanged);
        GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
        GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
        Config.OnWarpThrottleChanged.Add(OnThrottleChanged);

        var isEnabled =
            state == StartState.Editor
                ? (UI_Toggle)Fields[nameof(IsEnabled)].uiControlEditor
                : (UI_Toggle)Fields[nameof(IsEnabled)].uiControlFlight;

        isEnabled.onFieldChanged = (a, b) => OnEnabledChanged();
    }

    protected virtual void OnDestroy()
    {
        GameEvents.onTimeWarpRateChanged.Remove(OnTimeWarpRateChanged);
        GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
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
        UpdateBuffers();
    }

    protected virtual void OnTimeWarpRateChanged()
    {
        if (vessel.packed)
            UpdateBuffers();
    }

    void OnVesselGoOnRails(Vessel vessel)
    {
        if (vessel != this.vessel)
            return;

        OnGoOnRails();
    }

    void OnVesselGoOffRails(Vessel vessel)
    {
        if (vessel != this.vessel)
            return;

        OnGoOffRails();
    }

    protected virtual void OnGoOnRails()
    {
        UpdateBuffers();
    }

    protected virtual void OnGoOffRails()
    {
        ClearBuffers();
    }

    void OnEnabledChanged()
    {
        UpdateBuffers();
    }

    void OnThrottleChanged(GameEvents.FromToAction<double, double> _)
    {
        if (vessel != FlightGlobals.ActiveVessel)
            return;

        UpdateBuffers();
    }

    internal void OnEngineThrustPercentageChanged(ModuleEngines engines)
    {
        if (engines != Engine)
            return;

        UpdateBuffers();
    }
    #endregion

    #region Buffer Handling
    class Buffer : IConfigNode
    {
        public double OriginalMaxAmount;

        public PartResource Resource;
        public Propellant Propellant;

        public void Load(ConfigNode node)
        {
            node.TryGetValue(nameof(OriginalMaxAmount), ref OriginalMaxAmount);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue(nameof(OriginalMaxAmount), OriginalMaxAmount);
        }
    }

    readonly Dictionary<string, Buffer> buffers = [];

    private void ClearBuffers()
    {
        List<string> dead = null;

        foreach (var (resourceName, buffer) in buffers)
        {
            var propellant = buffer.Propellant;
            var resource = buffer.Resource;
            if (resource is null)
            {
                dead ??= [];
                dead.Add(resourceName);
                continue;
            }

            resource.maxAmount = buffer.OriginalMaxAmount;

            var extra = resource.amount - resource.maxAmount;
            if (extra > 0.0)
            {
                resource.amount = resource.maxAmount;

                var transferred = part.RequestResource(
                    resource.resourceName,
                    -extra,
                    propellant.GetFlowMode(),
                    simulate: false
                );

                resource.amount += extra - Math.Abs(transferred);
            }

            buffer.Propellant = null;
        }

        if (dead is not null)
        {
            foreach (var res in dead)
                buffers.Remove(res);
        }
    }

    private void UpdateBuffers()
    {
        if (!IsEnabled || Engine is null)
        {
            ClearBuffers();
            return;
        }

        DisableUpdateResourcesOnEventGuard? guard = null;

        try
        {
            double warp = TimeWarp.fixedDeltaTime;
            if (!vessel.packed)
                warp = 0.0;

            float throttle = Engine.currentThrottle;
            float fuelFlow = Mathf.Lerp(Engine.minFuelFlow, Engine.maxFuelFlow, throttle);

            foreach (var propellant in Engine.propellants)
            {
                if (!buffers.TryGetValue(propellant.name, out var buffer))
                {
                    var resource = part.Resources.Get(propellant.id);
                    buffer = new Buffer()
                    {
                        OriginalMaxAmount = resource?.maxAmount ?? 0.0,
                        Resource = resource,
                    };
                    buffers[propellant.name] = buffer;
                }
                else
                {
                    buffer.Resource ??= part.Resources[propellant.id];
                }

                if (!ReferenceEquals(buffer.Propellant, propellant))
                    buffer.Propellant = propellant;

                if (buffer.Resource is null)
                {
                    guard ??= DisableUpdateResourcesOnEvent();
                    ConfigNode node = new("RESOURCE");
                    node.AddValue("name", propellant.name);
                    node.AddValue("maxAmount", 0);
                    node.AddValue("amount", 0);
                    node.AddValue("isVisible", false);

                    buffer.Resource = part.AddResource(node);
                }

                float propFuelFlow = Engine.getFuelFlow(propellant, fuelFlow);
                var amount = propFuelFlow * warp * Config.BufferCapacityMult;
                buffer.Resource.maxAmount = Math.Max(amount, buffer.OriginalMaxAmount);
            }
        }
        finally
        {
            guard?.Dispose();
        }
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

    #region Helpers
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
    #endregion

    #region Save & Load
    // The kerbalism integration patches this, so it needs to stay.
    public override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        if (buffers is null)
            return;

        foreach (var (resource, buffer) in buffers)
        {
            var bnode = node.AddNode("BUFFER");
            bnode.AddValue("ResourceName", resource);
            buffer.Save(bnode);
        }
    }

    public override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        var bnodes = node.GetNodes("BUFFER");
        foreach (var bnode in bnodes)
        {
            string resource = null;
            if (!bnode.TryGetValue("ResourceName", ref resource))
                continue;

            Buffer buffer = new();
            buffer.Load(bnode);
        }
    }

    #endregion

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
