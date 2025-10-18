using MuMech;
using UnityEngine;

namespace BackgroundThrust.Integration.MechJeb2;

public class SmartASS : TargetHeadingProvider
{
    [KSPField(isPersistant = true)]
    QuaternionD orientation;
    MechJebModuleAttitudeController controller;

    public override TargetHeading GetTargetHeading(double UT)
    {
        if (!Vessel.loaded)
            return new(orientation);

        var controller = GetController();
        if (controller is not null)
            orientation = controller.RequestedAttitude;

        return new(orientation);
    }

    protected override void OnSave(ConfigNode node)
    {
        var controller = GetController();
        if (controller is not null)
            orientation = controller.RequestedAttitude;

        base.OnSave(node);
    }

    MechJebModuleAttitudeController GetController()
    {
        return controller ??= Vessel.GetMasterMechJeb()?.attitude;
    }
}
