using System;
using MuMech;
using UnityEngine;

namespace BackgroundThrust.MechJeb2;

internal static class EventDispatcher
{
    internal static void OnAttitudeControllerEnabled(MechJebModuleAttitudeController ac)
    {
        try
        {
            var vessel = AccessUtils.GetComputerModuleVessel(ac);
            if (!vessel.loaded || !vessel.packed)
                return;

            var module = vessel.GetBackgroundThrust();
            module.RefreshTargetHeading();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    internal static void OnAttitudeControllerDisabled(MechJebModuleAttitudeController ac)
    {
        try
        {
            var vessel = AccessUtils.GetComputerModuleVessel(ac);
            if (!vessel.loaded || !vessel.packed)
                return;

            var module = vessel.GetBackgroundThrust();
            if (module.TargetHeading is SmartASS)
                module.RefreshTargetHeading();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
