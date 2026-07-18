using System.Collections.Generic;
using BackgroundResourceProcessing;
using BackgroundResourceProcessing.Behaviour;

namespace BackgroundThrust.BRP;

public class BackgroundEngineBehaviour : ConstantConverter
{
    /// <summary>
    /// The thrust produced by this engine at full throttle, before the
    /// thrust limiter is applied.
    /// </summary>
    ///
    /// <remarks>
    /// Note that this isn't quite the same as the full throttle for the vessel,
    /// if the engine has a target throttle that is not full throttle then this
    /// will reflect that.
    /// </remarks>
    [KSPField(isPersistant = true)]
    public double MaxThrust = 0.0;

    /// <summary>
    /// The engine's thrust limiter, as a fraction in [0, 1].
    /// </summary>
    [KSPField(isPersistant = true)]
    public double ThrustLimiter = 1.0;

    /// <summary>
    /// <c>minFuelFlow / maxFuelFlow</c> for the engine. Engines with a
    /// nonzero minimum fuel flow (e.g. RealFuels) do not scale their fuel
    /// flow linearly with the throttle.
    /// </summary>
    [KSPField(isPersistant = true)]
    public double MinFlowFraction = 0.0;

    public double? Throttle = null;

    public BackgroundEngineBehaviour() { }

    public BackgroundEngineBehaviour(List<ResourceRatio> inputs, List<ResourceRatio> outputs)
        : base(inputs, outputs, []) { }

    /// <summary>
    /// The fraction of the max fuel flow (and thrust) that the engine
    /// produces at the given throttle. Mirrors ModuleEngines, which folds
    /// the thrust limiter into the requested throttle and then lerps the
    /// fuel flow from minFuelFlow to maxFuelFlow.
    /// </summary>
    ///
    /// <remarks>
    /// A throttle of exactly 0 cuts the engine entirely, even though a stock
    /// ignited engine would still flow minFuelFlow: the mod treats zero warp
    /// throttle as engines-off.
    /// </remarks>
    public double GetFlowFraction(double throttle)
    {
        throttle = UtilMath.Clamp01(throttle);
        if (throttle == 0.0)
            return 0.0;

        return MinFlowFraction + (1.0 - MinFlowFraction) * (throttle * ThrustLimiter);
    }

    public override ConverterResources GetResources(VesselState state)
    {
        var module = EventDispatcher.Instance.GetVesselModule(Vessel);
        var throttle = Throttle ?? module.Throttle;
        if (module.TargetHeading is null)
            throttle = 0.0;

        var fraction = GetFlowFraction(throttle);
        if (fraction >= 1.0)
            return base.GetResources(state);

        var inputs = new List<ResourceRatio>(Inputs.Count);
        var outputs = new List<ResourceRatio>(Outputs.Count);

        foreach (var input in Inputs)
            inputs.Add(input with { Ratio = input.Ratio * fraction });
        foreach (var output in Outputs)
            outputs.Add(output with { Ratio = output.Ratio * fraction });

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
