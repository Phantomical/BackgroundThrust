# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Note: Spacedock's markdown doesn't recognize lists using `-`, so make sure to
      use `*` for all list entries.
-->

## Unreleased
### Fixed
* Vessels using a reversed control point (or any control point that is rotated
  relative to the part it sits on) are no longer pointed 180 degrees away from
  the selected SAS mode while in time warp or in the background.

## 0.6.11
### Added
* You can now toggle SAS while in time warp.

### Fixed
* Fixed an issue where the calculated remaining burn time was calculated to
  be 0 when in low thrust or no-mass-change conditions. Thanks to Jaheay on
  github for the fix PR.
* Background thrust for unloaded ships now actually uses the rocket equation
  to calculate delta-v instead of wrongly assuming the mass stays constant.
* Vessel flip protection now uses the thrust axis, instead of the forward axis.
* Fixed a NRE if a maneuver node is deleted while using the maneuver targeting
  mode.
* Fixed a bug where saving during thrust could permanently increase the size of
  resource buffer tanks.
* Fixed a bunch of bugs related to using MechJeb2 SmartASS targeting modes in
  time warp.
* Ullage for RealFuels' engines in time warp now includes thrust in the correct
  axis, not a rotated one.
* Background engines now respect the thrust limiter and throttle locking.
* Fixed a couple of issues with kerbalism, though it is still untested.

## 0.6.10
### Added
* Added some new patches to properly allow switching scenes while under thrust.

### Fixed
* Fixed a memory leak in the BackgroundEngine module.
* BackgroundThrustVessel modules now properly disable themselves when they are
  not being used.
* Fixed a bunch of issues that would cause thrust in the background with BRP to
  not work.

## 0.6.9
### Fixed
* Disable heat production for engines when KSP uses the analytical heat mode.
* BackgroundThrust will no longer change the current SAS mode in any way.

## 0.6.8
### Fixed
* Fixed a bug where SAS would be disabled when coming out of warp.

## 0.6.7
### Fixed
* Fixed a bug where empty SRBs would occasionally keep running in the background.
* Fixed logspam due to missing simulation resources.
* Simplify the code that slows down time warps when executing maneuver nodes.

## 0.6.6
### Fixed
* Fixed an incompatibility due to different types between MechJeb 2.14.3 and 2.15.

## 0.6.5
### Fixed
* Fixed compatibility issues with the new MechJeb2 release. BackgroundThrust
  is now compatible with both MechJeb 2.14.3 and 2.15.
* Avoid updating the vessel heading if the throttle is non-zero but the overall
  thrust is zero.
* Avoid a nullref in OnSave if the mechjeb attitude controller is null.
* Guard against future bugs when creating a target heading provider causing
  OnSave to throw an exception.

## 0.6.4
### Fixed
* The warp buffer for engines now scales with both throttle and time warp rate.

## 0.6.3
### Fixed
* Fixed an `InvalidOperationException` being thrown during `BackgroundEngine`
  OnSave.

## 0.6.2
### Fixed
* Warp buffer now keeps the buffer inventories around as zero-sized part
  resources instead of destroying and recreating them each time you go into or
  out of time warp. This should (hopefully) fix the stutter when entering or
  leaving time warp.

## 0.6.1
### Fixed
* Point mechjeb `TARGET_RELATIVE` orientation frame in the correct direction.

## 0.6.0
### Added
* Added support for PersistentRotation.

### Fixed
* Correctly handle cases where the control point is not aligned with the ship
  for real this time (hopefully).

## 0.5.1
### Fixed
* Correctly handle cases where the control point is not aligned with the ship
  orientation.

## 0.5.0
### Changed
* Added a log message when the target orientation mode gets kicked to a fixed
  heading mode.
* SAS mode is now synchronized with the target heading while in warp.

### Fixed
* Fixed an error where an engine would fail to be recorded for Background
  Resource Processing if it had an alternator linked to a `MultiModeEngine`.
* Fixed an error where SAS would sometimes switch to a fixed heading mode when
  switching between headings that faced in opposite directions.
* Fixed an error where cutting thrust would result in the vessel continuing
  to accelerate.

## 0.4.0
### Changed
* Thrust in warp no longer has to explicitly run the engine module `FixedUpdate`
  since it now runs itself as if it wasn't in warp.

### Fixed
* MechJeb `TARGET`, `RELATIVE VELOCITY`, and `MANEUVER NODE` reference frames
  now work correctly.
* Avoid setting vessel orientation if both thrust and throttle are zero.

## 0.3.0
### Added
* Added a new module for any engine using SolverEngines.
* Added support for using RealFuels engines with BackgroundThrust.
* Added support for RealismOverhaul persistent rotation.

### Changed
* Integration DLLs have been renamed to remove `.Integration` from their names.
* Thrust in warp now literally just runs the engine module `FixedUpdate` method.

### Fixed
* Fixed a bug where SAS mode orientations were being incorrectly calculated.

## 0.2.0
### Added
* Added ability to use all MechJeb smart ASS modes in warp.

### Changed
* Background Thrust is now marked as conflicting with Persistent Thrust on CKAN.
* Reworked a bunch of the internal APIs to work better when there are multiple
  possible SAS providers.

## 0.1.3
### Fixed
* Fix normal and antinormal SAS modes to work the same way as KSP's SAS modes.

## 0.1.2
### Changed
* Adjust patch for `TimeWarp.setTimeRate` to be more compatible with other mods
  making similar patches.

## 0.1.1
### Added
* Allow entering time warp while the vessel is under thrust.
* Accurately update the G-force meter while in time warp.

## 0.1.0
This is the initial release of Background Thrust.
