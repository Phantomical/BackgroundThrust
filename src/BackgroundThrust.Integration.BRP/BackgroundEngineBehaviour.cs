using System.Collections.Generic;
using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Behaviour;

namespace BackgroundThrust.Integration.BRP;

public class BackgroundEngineBehaviour : ConstantConverter
{
    /// <summary>
    /// The thrust produced by this engine at full throttle.
    /// </summary>
    ///
    /// <remarks>
    /// Note that this isn't quite the same as the full throttle for the vessel,
    /// if the engine has a target throttle that is not full throttle then this
    /// will reflect that.
    /// </remarks>
    [KSPField(isPersistant = true)]
    public double MaxThrust = 0.0;

    public double? Throttle = 0.0;

    public BackgroundEngineBehaviour() { }

    public BackgroundEngineBehaviour(List<ResourceRatio> inputs, List<ResourceRatio> outputs)
        : base(inputs, outputs, []) { }

    public override ConverterResources GetResources(VesselState state)
    {
        var module = EventDispatcher.Instance.GetVesselModule(Vessel);
        var throttle = Throttle ?? module.Throttle;
        if (module.Throttle == 0.0)
            throttle = 0.0;

        if (throttle >= 1.0)
            return base.GetResources(state);

        if (throttle < 0.0)
            throttle = 0.0;

        var inputs = new List<ResourceRatio>(Inputs.Count);
        var outputs = new List<ResourceRatio>(Outputs.Count);

        foreach (var input in Inputs)
            inputs.Add(input with { Ratio = input.Ratio * throttle });
        foreach (var output in Outputs)
            outputs.Add(output with { Ratio = output.Ratio * throttle });

        return new() { Inputs = inputs, Outputs = outputs };
    }

    protected override void OnLoad(ConfigNode node)
    {
        base.OnLoad(node);

        double throttle = 0.0;
        if (node.TryGetValue(nameof(Throttle), ref throttle))
            Throttle = throttle;
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);

        if (Throttle is double throttle)
            node.AddValue(nameof(Throttle), throttle);
    }
}
