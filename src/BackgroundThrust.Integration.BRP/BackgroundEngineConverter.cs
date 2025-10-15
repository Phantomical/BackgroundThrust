using System.Collections.Generic;
using System.Reflection;
using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Converter;

namespace BackgroundThrust.Integration.BRP;

public class BackgroundEngineConverter : BackgroundConverter<BackgroundEngine>
{
    static readonly FieldInfo AlternatorEngineField = typeof(ModuleAlternator).GetField(
        "engine",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    public override ModuleBehaviour GetBehaviour(BackgroundEngine module)
    {
        if (!module.IsEnabled)
            return null;
        if (module.Engine is null)
            return null;

        var engine = module.Engine;
        double? throttle = null;
        if (engine.independentThrottle)
            throttle = engine.independentThrottlePercentage * 0.01;
        var maxThrust = engine.maxThrust;

        var inputs = new List<ResourceRatio>(engine.propellants.Count);
        var outputs = new List<ResourceRatio>();

        foreach (var propellant in engine.propellants)
        {
            inputs.Add(
                new()
                {
                    ResourceName = propellant.resourceDef.name,
                    Ratio = engine.getMaxFuelFlow(propellant),
                    FlowMode = propellant.GetFlowMode(),
                    DumpExcess = false,
                }
            );
        }

        var alternators = module.part.FindModulesImplementing<ModuleAlternator>();
        foreach (var alternator in alternators)
        {
            var alternatorEngine = (ModuleEngines)AlternatorEngineField.GetValue(alternator);
            if (!ReferenceEquals(engine, alternatorEngine))
                continue;

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
            }
        );
    }
}
