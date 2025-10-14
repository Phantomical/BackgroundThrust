# Background Thrust

This is a mod for KSP that allows you to perform burns during timewarp or, if
certain dependencies are available, completely in the background.

## Features
* Make burns while in time warp!
* Make burns while the ship is out of focus! (With the right dependencies)
* The vessel automatically tracks your SAS mode.
* Automatic management of resource buffers so that your ion engines keep working
  at high warp factors.

Beyond that, BackgroundThrust was designed to allow you to swap out the target
control loop so that you can hook a custom autopilot module in to do whatever
types of maneuvers you want to.

## Dependencies
#### Required Dependencies
- [ModuleManager](https://github.com/sarbian/ModuleManager)
- [HarmonyKSP](https://github.com/KSPModdingLibs/HarmonyKSP)

#### Optional Dependencies:
In order to allow thrust to happen in the background you will need to also add
one of the following mods:
* [BackgroundResourceProcessing](https://forum.kerbalspaceprogram.com/topic/228375-1125-background-resource-processing)
