# SuperliminalTools

A BepInEx plugin for Superliminal for speedrun practice and tool assisted speedrunning.

## Installation

Download the release matching your game version and extract the contents into your game folder.

## Usage

Use the included `.bat` files to launch the game in practice mod, TAS mod, or with no mods.

When run manually, the plugin loads in practice mod by default. Use `--tas` as a launch argument to start in TAS mod.

## Compilation

The project expects the game libraries to be placed in `GameLibs/$GameVersion` (e.g. `GameLibs/Legacy/`, `GameLibs/Modern/`). These directories are tracked but their contents are gitignored.
