using System;
using BackgroundThrust.Utils;
using static BackgroundThrust.Utils.MathUtil;

namespace BackgroundThrust.Heading;

public class Maneuver : TargetHeadingProvider
{
    ManeuverNode Node
    {
        get
        {
            var nodes = Vessel?.patchedConicSolver?.maneuverNodes;
            if (nodes is null)
                return null;
            if (nodes.Count == 0)
                return null;
            return nodes[0];
        }
    }

    public Maneuver() { }

    public override Vector3d? GetTargetHeading(double UT)
    {
        var node = Node;
        if (node is null)
            return null;

        var heading = node.GetBurnVector(Vessel.orbit);
        if (heading == Vector3d.zero)
            return null;

        return heading.normalized;
    }

    public override void IntegrateThrust(BackgroundThrustVessel module, ThrustParameters parameters)
    {
        var heading = module.Heading;
        var deltaV = parameters.ComputeDeltaV();
        if (!IsFinite(deltaV))
        {
            LogUtil.Error("deltaV was infinite or NaN");
            return;
        }

        var mag2 = heading.sqrMagnitude;
        if (mag2 == 0.0 || !IsFinite(mag2))
            return;

        var node = Node;
        var startDeltaV = node.GetBurnVector(Vessel.orbit);

        Vessel.orbit.Perturb(heading * deltaV, parameters.StopUT);

        var endDeltaV = node.GetBurnVector(Vessel.orbit);

        // We consider a node to have completed when its deltaV vector flips
        // around after perturbing the orbit.
        if (Vector3d.Dot(startDeltaV, endDeltaV) > 0.0)
        {
            // We only do warp adjustments for the active vessel.
            if (Vessel != FlightGlobals.ActiveVessel)
                return;

            var remaining = endDeltaV.magnitude;
            var estimateUT = parameters.GetUTAtDeltaV(deltaV + remaining);
            var remainingT = Math.Max(estimateUT - parameters.StartUT, 0.0);

            SetTargetWarpRate(remainingT);
        }
        else
        {
            module.SetThrottle(0.0);
            module.SetTargetHeading(null);

            // We only display screen messages for the active vessel.
            if (Vessel != FlightGlobals.ActiveVessel)
                return;

            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            TimeWarp.SetRate(0, instant: true);
        }
    }

    void SetTargetWarpRate(double remaining)
    {
        if (TimeWarp.CurrentRate < 10f)
            return;

        bool instant = TimeWarp.fixedDeltaTime <= remaining;
        var cRate = TimeWarp.CurrentRate;
        if (cRate < remaining * 0.5)
            return;

        var timeWarp = TimeWarp.fetch;
        var rateIdx = TimeWarp.CurrentRateIndex;
        var minOnRailsIndex = timeWarp.maxPhysicsRate_index + 1;
        if (rateIdx <= minOnRailsIndex)
            return;

        for (int index = rateIdx - 1; index > minOnRailsIndex; index -= 1)
        {
            var rate = timeWarp.warpRates[index - minOnRailsIndex];
            if (rate < remaining * 0.1)
            {
                TimeWarp.SetRate(index, instant);
                return;
            }
        }

        TimeWarp.SetRate(minOnRailsIndex, instant);
    }
}
