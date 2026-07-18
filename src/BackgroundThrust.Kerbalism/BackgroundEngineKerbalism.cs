using System;
using System.Collections.Generic;

namespace BackgroundThrust.Kerbalism;

internal class BackgroundEngineKerbalism
{
    struct PropellantInfo()
    {
        public string ResourceName;
        public double Rate;

        public PropellantInfo(ConfigNode node)
            : this()
        {
            Load(node);
        }

        public void Load(ConfigNode node)
        {
            node.TryGetValue(nameof(ResourceName), ref ResourceName);
            node.TryGetValue(nameof(Rate), ref Rate);
        }

        public readonly void Save(ConfigNode node)
        {
            node.AddValue(nameof(ResourceName), ResourceName);
            node.AddValue(nameof(Rate), Rate);
        }
    }

    public static void OnSave(BackgroundEngine module, ConfigNode node)
    {
        var engine = module.Engine;
        if (engine is null)
            return;
        // Mirrors the BRP converter: a shutdown or flamed-out engine produces
        // no thrust in the background, even if its throttle is locked.
        if (!engine.isOperational)
            return;
        if (!module.IsEnabled || !module.AllowBackgroundProcessing)
            return;
        if (!BackgroundThrustVessel.IsThrustPermitted(module.vessel))
            return;

        // Mirrors the branch order in ModuleEngines.UpdateThrottle: a locked
        // throttle runs at the limiter and ignores both the independent and the
        // vessel throttle. An absent Throttle key falls back to the vessel's.
        if (engine.throttleLocked)
            node.AddValue("Throttle", 1.0);
        else if (engine.independentThrottle)
            node.AddValue("Throttle", engine.independentThrottlePercentage * 0.01);

        // UpdateThrottle folds the thrust limiter into the requested throttle,
        // and the fuel flow then lerps from minFuelFlow to maxFuelFlow. The
        // limiter and throttle have to combine inside the lerp, so persist
        // them for BackgroundUpdate instead of baking them into the rates.
        node.AddValue("ThrustLimiter", engine.thrustPercentage * 0.01);
        node.AddValue(
            "MinFlowFraction",
            engine.maxFuelFlow > 0f
                ? UtilMath.Clamp01(engine.minFuelFlow / engine.maxFuelFlow)
                : 0.0
        );
        node.AddValue("Thrust", engine.maxThrust);

        foreach (var propellant in engine.propellants)
        {
            var info = new PropellantInfo()
            {
                ResourceName = propellant.resourceDef.name,
                Rate = engine.getMaxFuelFlow(propellant),
            };

            info.Save(node.AddNode("PROPELLANT"));
        }
    }

    #region Kerbalism Background Update
    const string KerbalismToolTipName = "bt-engine";

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
        if (Config.VesselInfoProvider is not KerbalismVesselInfoProvider)
            return KerbalismToolTipName;

        var module = v.FindVesselModuleImplementing<BackgroundThrustVessel>();
        var node = module_snapshot.moduleValues;

        if (module.TargetHeading is null)
            return KerbalismToolTipName;

        double throttle = module.Throttle;
        node.TryGetValue("Throttle", ref throttle);
        if (throttle == 0.0)
            return KerbalismToolTipName;

        double thrust = 0.0;
        if (!node.TryGetValue("Thrust", ref thrust))
            return KerbalismToolTipName;

        // Nodes saved by older versions bake the limiter into the Thrust and
        // Rate values instead; these defaults reproduce that behaviour.
        double limiter = 1.0;
        double minFlowFraction = 0.0;
        node.TryGetValue("ThrustLimiter", ref limiter);
        node.TryGetValue("MinFlowFraction", ref minFlowFraction);

        // The fraction of the max fuel flow (and thrust) that the engine
        // produces at this throttle. Mirrors ModuleEngines, which folds the
        // thrust limiter into the requested throttle and then lerps the fuel
        // flow from minFuelFlow to maxFuelFlow.
        var fraction =
            minFlowFraction + (1.0 - minFlowFraction) * (UtilMath.Clamp01(throttle) * limiter);

        resourceChangeRequest.Add(
            new(KerbalismVesselInfoProvider.ThrustResourceName, thrust * fraction)
        );

        foreach (var propNode in node.GetNodes("PROPELLANT"))
        {
            var propellant = new PropellantInfo(propNode);
            resourceChangeRequest.Add(new(propellant.ResourceName, -propellant.Rate * fraction));
        }

        return KerbalismToolTipName;
    }
    #endregion
}

internal static class ConfigNodeExt
{
    internal static double GetDouble(this ConfigNode node, string name)
    {
        string text = null;
        if (!node.TryGetValue(name, ref text))
            throw new KeyNotFoundException($"config node has no key `{name}`");

        if (!double.TryParse(text, out var value))
            throw new FormatException(
                $"config node value `{name}` did not contain a valid number (got `{text}` instead)"
            );

        return value;
    }
}
