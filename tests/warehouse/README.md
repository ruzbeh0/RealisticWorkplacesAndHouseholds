# Warehouse behavioral regression harness

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/warehouse/WarehouseRegression.csproj --configuration Release
```

This dependency-free console harness compiles the **actual production sources**
for `WarehouseUpdateSystem`, `UpdateWarehouseJob`, `BuildingUtils`, and the three
warehouse components. It does not copy their algorithms or assert source text.
The harness stays outside the mod project and is not included in the mod DLL.

`Stubs.cs` supplies a deliberately small managed entity store, query-description
evaluator, component lookups, chunk view, deferred command recorder, and game API
types. Scheduled jobs execute only when the test drains the queue; commands
execute only at explicit playback. Adding an existing component/buffer throws,
which catches duplicate additions and unintended employee-buffer replacement.
There are no test framework or NuGet package dependencies.

The cases exercise:

- Repair OR-query membership: each missing component, initialized companies,
  non-company property renters, and direct warehouse targets.
- Outside connection, deleted/temp, service and transport entity/prefab protection.
- Immediate component repair before deferred capacity jobs and ECB playback.
- Shared property employee retention and independent property component loss.
- Zero commands for unchanged capacity, changed capacity and provider cooldown.
- Preservation of prior workplace version/multiplier normalization semantics.
- ABC overrides on storage and property prefabs, immediate default initialization
  under an active override, and capacity updates after override removal.
- Subsequent settings, footprint, property-prefab, and mesh changes.
- Minimum capacity when property/prefab mesh data is absent.
- Consecutive prefab reuse for A,A,B,A,Null,Null,A, including distinct properties
  sharing one prefab, with geometry reads counted through the lookup double.
- A 1,000-company steady state that avoids synchronous dependency completion
  and repair-query materialization while retaining capacity updates.

## Limits

These are portable **behavioral** tests, not native Unity ECS or Burst runtime
tests. The test doubles do not implement native archetype transitions, change
versions, job safety checks, dependency completion semantics, enabled masks,
parallel workers, or Unity ECB scheduling/playback rules. Chunk grouping here
uses property-renter presence; real ECS chunk boundaries can reduce cache reuse.
Completion counts only record calls to a fake `Complete`; geometry-read counts
describe the linked code paths under this managed model. Query timing and
performance are not measured, and these are not FPS measurements. Game components contain
only the fields used by these production sources, so actual game assemblies and
Burst compilation still need the official mod build.

The real game remains necessary to validate vanilla system ordering, shared
target behavior under parallel scheduling, saved cities, mod compatibility, and
benchmark performance. The harness provides repeatable coverage of the changed
warehouse logic without pretending to replace those checks.
