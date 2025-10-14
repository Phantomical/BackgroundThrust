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

    static readonly List<ResourceConstraint> EmptyConstraints = [];

    public override ModuleBehaviour GetBehaviour(BackgroundEngine module)
    {
        if (!module.IsEnabled)
            return null;
        if (module.Engine is null)
            return null;

        var engine = module.Engine;
        var throttle = (double)engine.currentThrottle;
        if (throttle == 0.0 || !engine.EngineIgnited)
            return null;

        var thrust = engine.finalThrust;
        if (thrust == 0.0)
            return null;

        var inputs = new List<ResourceRatio>(engine.propellants.Count);
        var outputs = new List<ResourceRatio>();

        foreach (var propellant in engine.propellants)
        {
            inputs.Add(
                new()
                {
                    ResourceName = propellant.resourceDef.name,
                    Ratio = engine.getFuelFlow(propellant, engine.requestedMassFlow),
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
                        Ratio = resource.rate * throttle,
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
                        Ratio = resource.rate * throttle,
                        FlowMode = resource.flowMode,
                        DumpExcess = true,
                    }
                );
            }
        }

        return new(
            new BackgroundEngineBehaviour(inputs, outputs, EmptyConstraints)
            {
                Thrust = engine.resultingThrust,
            }
        );
    }
}
