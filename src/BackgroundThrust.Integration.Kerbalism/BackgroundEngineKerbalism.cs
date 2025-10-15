using System.Collections.Generic;
using BackgroundThrust.Patches;
using KSP.Localization;

namespace BackgroundThrust.Integration.Kerbalism;

public class BackgroundEngineKerbalism : BackgroundEngine
{
    static readonly string cacheAutoLOC_220370 = Localizer.Format("#autoLOC_220370");
    static readonly string cacheAutoLOC_220377 = Localizer.Format("#autoLOC_220377");

    // Since kerbalism is handling resource consumption we do not need to
    // create buffers.
    protected override void OnTimeWarpRateChanged() { }

    // This gets handled by ResourceUpdate instead.
    public override void PackedEngineUpdate() { }

    public string ResourceUpdate(
        Dictionary<string, double> available,
        List<KeyValuePair<string, double>> requests
    )
    {
        if (Engine is null || !vessel.packed)
        {
            Thrust = 0.0;
            return "bt-engine";
        }

        Engine.UpdateThrottle();
        Engine.currentThrottle = ModuleEngines_Patch.ApplyThrottleAdjustments(
            Engine,
            Engine.currentThrottle
        );
        if (Engine.EngineIgnited)
            ModuleEngines_Patch.UpdatePropellantStatus(Engine);

        return "bt-engine";
    }

    private void GetRequiredPropellants(List<KeyValuePair<string, double>> requests)
    {
        var engine = Engine;

        double propellantReqMet = 0.0;
        double fuelUsage = vessel.VesselValues.FuelUsage.value;

        if (fuelUsage == 0.0)
            fuelUsage = 1.0;

        double massFlow = ModuleEngines_Patch.RequiredPropellantMass(
            engine,
            engine.currentThrottle
        );

        if (engine.flowMultiplier < engine.flameoutBar)
        {
            engine.Flameout(cacheAutoLOC_220370);
        }
    }
}
