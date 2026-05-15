# Deprecated Scripts

This folder contains scripts that have been replaced or are no longer active in the project.

## Why are they here?

- **Safety**: Kept as backup in case serialized references still exist
- **History**: Can be referenced for old implementations
- **Migration**: Easier to restore if needed

## What to do with them?

1. If absolutely no GameObject/Scene references them → Can be deleted
2. If uncertain → Keep in _Deprecated folder, marked with [Obsolete] attribute
3. If needed → Can be restored to main codebase

## Current Content

- Subsystem duplicates (old architecture)
- Deprecated movement controllers
- Old UI implementations
- Other replaced systems
