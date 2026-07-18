using System.Collections.Generic;
using System.Reflection;
using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Converter;
using UnityEngine.UI;

namespace BackgroundThrust.BRP;

public class BackgroundEngineConverter : BackgroundConverter<BackgroundEngine>
{
    static readonly FieldInfo AlternatorEngineField = typeof(ModuleAlternator).GetField(
        "engine",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    public override ModuleBehaviour GetBehaviour(BackgroundEngine module)
    {
        if (Config.VesselInfoProvider is not BRPVesselInfoProvider)
            return null;
        if (!module.IsEnabled || !module.AllowBackgroundProcessing)
            return null;
        if (module.Engine is null)
            return null;
        if (!BackgroundThrustVessel.IsThrustPermitted(module.vessel))
            return null;

        var engine = module.Engine;
        if (!engine.isOperational)
            return null;

        // Mirrors the branch order in ModuleEngines.UpdateThrottle: a locked
        // throttle runs at the limiter and ignores both the independent and the
        // vessel throttle. A null throttle falls back to the vessel's.
        double? throttle = null;
        if (engine.throttleLocked)
            throttle = 1.0;
        else if (engine.independentThrottle)
            throttle = engine.independentThrottlePercentage * 0.01;

        // ModuleEngines.UpdateThrottle folds the thrust limiter into the
        // requested throttle, and the fuel flow then lerps from minFuelFlow
        // to maxFuelFlow. Both have to combine inside the lerp, so hand them
        // to the behaviour instead of baking them into the ratios.
        var limiter = engine.thrustPercentage * 0.01;
        var minFlowFraction =
            engine.maxFuelFlow > 0f
                ? UtilMath.Clamp01(engine.minFuelFlow / engine.maxFuelFlow)
                : 0.0;
        var maxThrust = engine.maxThrust;

        var inputs = new List<ResourceRatio>(engine.propellants.Count);
        var outputs = new List<ResourceRatio>();
        var totalFuelFlow = 0.0;

        foreach (var propellant in engine.propellants)
        {
            var input = new ResourceRatio()
            {
                ResourceName = propellant.resourceDef.name,
                Ratio = engine.getMaxFuelFlow(propellant),
                FlowMode = propellant.GetFlowMode(),
                DumpExcess = false,
            };

            totalFuelFlow += input.Ratio;
            inputs.Add(input);
        }

        // We don't handle cases where the engine consumes no fuel, since that
        // would mean it would keep going forever.
        //
        // This pops up when there are SRBs with a wind-down, where they are
        // still technically running, but have no fuel.
        if (totalFuelFlow == 0.0)
            return null;

        var alternators = module.part.FindModulesImplementing<ModuleAlternator>();
        foreach (var alternator in alternators)
        {
            var alternatorEngine = (IEngineStatus)AlternatorEngineField.GetValue(alternator);
            if (alternatorEngine is MultiModeEngine)
            {
                if (!ReferenceEquals(module.MultiModeEngine, alternatorEngine))
                    continue;
            }
            else if (alternatorEngine is ModuleEngines)
            {
                if (!ReferenceEquals(module.Engine, alternatorEngine))
                    continue;
            }
            else
            {
                continue;
            }

            foreach (var resource in alternator.resHandler.inputResources)
            {
                inputs.Add(
                    new ResourceRatio
                    {
                        ResourceName = resource.name,
                        Ratio = resource.rate,
                        FlowMode = resource.flowMode,
                    }
                );
            }

            foreach (var resource in alternator.resHandler.outputResources)
            {
                outputs.Add(
                    new ResourceRatio
                    {
                        ResourceName = resource.name,
                        Ratio = resource.rate,
                        FlowMode = resource.flowMode,
                        DumpExcess = true,
                    }
                );
            }
        }

        return new(
            new BackgroundEngineBehaviour(inputs, outputs)
            {
                MaxThrust = maxThrust,
                Throttle = throttle,
                ThrustLimiter = limiter,
                MinFlowFraction = minFlowFraction,
            }
        );
    }
}
