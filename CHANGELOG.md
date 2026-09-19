# Changelog

All notable changes to this fork are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this fork uses [MAJOR.MINOR.PATCH](https://semver.org/). See `AGENTS.md`
for when to bump each number.

The launcher pin `DisplayVersion` must match the latest dated heading below.
The upstream playable download remains GitHub **v0.3**; that is the runtime
package, not this fork's version.

## [Unreleased]

## [0.4.0] - 2026-09-19

### Fixed

- The launcher no longer overwrites Windowed mode on every Enter Realm launch.
  A new AppData profile still defaults to borderless fullscreen; an existing
  `user.dat` display choice and resolution are left alone.

## [0.3.1] - 2026-09-19

### Added

- Fork changelog and MAJOR.MINOR.PATCH versioning rules in `AGENTS.md`.
- Launcher pin set to `0.3.1` so this fork is distinct from upstream v0.3
  and from the original author's private 0.4 label.

## [0.3.0] - 2026-09-15

Upstream Offline DAoC v0.3 public baseline. No reconstructed author history.
The playable runtime, world data, and navigation meshes still come from that
GitHub release.
