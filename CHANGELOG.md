# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
Note: Spacedock's markdown doesn't recognize lists using `-`, so make sure to
      use `*` for all list entries.
-->

## Unreleased
### Changed
* Thrust in warp no longer has to explicitly run the engine module `FixedUpdate`
  since it now runs itself as if it wasn't in warp.

### Fixed
* MechJeb `TARGET`, `RELATIVE VELOCITY`, and `MANEUVER NODE` reference frames
  now work correctly.

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
