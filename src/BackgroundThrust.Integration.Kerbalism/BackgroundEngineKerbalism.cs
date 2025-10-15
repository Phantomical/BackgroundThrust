using System;
using System.Collections.Generic;

namespace BackgroundThrust.Integration.Kerbalism;

public class BackgroundEngineKerbalism : BackgroundEngine
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

    public override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        if (Engine is null)
            return;

        if (Engine.independentThrottle)
            node.AddValue("Throttle", Engine.independentThrottlePercentage * 0.01);
        node.AddValue("Thrust", Engine.maxThrust);

        foreach (var propellant in Engine.propellants)
        {
            var info = new PropellantInfo()
            {
                ResourceName = propellant.resourceDef.name,
                Rate = Engine.getMaxFuelFlow(propellant),
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
        var module = v.FindVesselModuleImplementing<BackgroundThrustVessel>();
        var node = module_snapshot.moduleValues;

        if (Config.VesselInfoProvider is not KerbalismVesselInfoProvider)
            return KerbalismToolTipName;
        if (module.TargetHeading is null)
            return KerbalismToolTipName;

        double throttle = module.Throttle;
        node.TryGetValue("Throttle", ref throttle);
        if (throttle == 0.0)
            return "bt-engine";

        double thrust = node.GetDouble("Thrust");
        resourceChangeRequest.Add(new("BackgroundThrust", thrust * throttle));

        foreach (var propNode in node.GetNodes("PROPELLANT"))
        {
            var propellant = new PropellantInfo(propNode);
            resourceChangeRequest.Add(new(propellant.ResourceName, -propellant.Rate * throttle));
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
