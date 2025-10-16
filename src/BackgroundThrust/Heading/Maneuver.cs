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
        var deltaV = parameters.ComputeDeltaV();
        var node = Node;
        var nodeDeltaVV = node.GetBurnVector(Vessel.orbit);
        var nodeDeltaV = nodeDeltaVV.magnitude;

        base.IntegrateThrust(module, parameters);

        if (nodeDeltaV > deltaV)
            return;

        module.SetTargetHeading(null);
        module.SetThrottle(0.0);

        if (Vessel == FlightGlobals.ActiveVessel)
        {
            ScreenMessages.PostScreenMessage("Maneuver Complete. Cutting thrust.");
            TimeWarp.SetRate(0, instant: true);
            module.SetThrottle(0.0);
            module.SetTargetHeading(null);
        }
    }
}
