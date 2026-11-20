# Realistic Workplaces And Households (RWH)

RWH recalculates how many households, workplaces, and service staffing capacities buildings can hold using each building’s **actual dimensions** (width, length, height), then applies configurable “space-per-unit” rules.

This keeps building capacity behavior more consistent across different asset types and lot sizes instead of relying on fixed static values.

## What it changes

- Recomputes residential households and workers from building volume/area.
- Recomputes capacity and staffing for many city-service buildings and ploppables.
- Adds balance offsets for side effects (rent, upkeep, utility use, and noise).
- Allows per-asset-pack density tuning and CSV persistence.
- Exports building statistics for analysis.
- Zoned warehouses use one floor of usable footprint for staffing, with periodic capacity checks and immediate repair of missing workplace components.
- The 1.5 m residential roof allowance applies only to detached, low-density houses; row homes, apartments, and mixed-use buildings retain their full measured height.

## Building types covered

### Zoned buildings

- Residential (including row-home handling and density overrides).
- Commercial.
- Office.
- Industrial (standard/non-specialized industry).

### City services / ploppables

- Admin buildings, welfare offices, research facilities, transport stations, telecom facilities.
- Parks, police/fire stations, hospitals/clinics, schools, prisons, power plants.
- Depots, maintenance buildings, cargo/transport facilities, garbage buildings, and related service assets.

## How capacities are calculated (high-level)

1. Read building dimensions from prefab mesh.
2. Estimate floors using category-specific average floor height.
3. Compute effective floor area and convert area into units based on selected space-per-unit rules:
   - Households (with hallway/elevator/common-space deductions),
   - Workers (with category-specific deductions and multipliers),
   - School students/staff from area + students-per-teacher model.

## Asset pack tuning

- Configure multipliers per asset pack for:
  - Low density.
  - Row homes.
  - Medium/high density.
- Use **Load** to pull current pack factors into sliders.
- Use **Save** to write values to CSV and persist between sessions.

## Export and balancing tools

- **Export Building Statistics**: exports CSV files with prefab-level values to your Documents folder.
- Optional balancing helpers:
  - Service upkeep cost reduction.
  - Electricity/water consumption reduction.
  - Noise scaling factor.
  - Residential rent discount.
  - Global workplace/households reduction percentage.

## Controls

- Enable or disable each major category independently:
  - Households,
  - Workplaces,
  - City-service capacity calculations.

## Behavior in existing cities

Changing settings in an existing save can increase or decrease capacities. In cases where capacity drops, residents may relocate and population/employment can temporarily fluctuate until the simulation stabilizes.

## Known notes

- If demand behavior looks unusual after enabling, try temporarily increasing the game’s “Find Property” request limits and then lowering them back.
- Plop the Growables may produce odd employee counts for some buildings; reload can help. (Placing growables is not the recommended workflow.)

## Compatibility

Avoid running multiple mods that overwrite household/workplace capacities at the same time.

Advanced Building Control (ABC) household and workplace capacity overrides are respected. RWH calculates capacities for buildings without those overrides and supports both current and older ABC enum namespaces.

## Credits

- Inspired by Realistic Population (CS1), Realistic Building Occupancy, and Realistic Density (CS2) concepts.
- Thanks to Trejak, Mimonsi, and 89pleasure for ideas/reference implementations.
- Export CSV implementation by Logiteee.
- CSV locale fixes, depot and residential logic fixes, workplace update improvements, and household initialization/stability fixes by [jimocool2](https://github.com/jimocool2) in PRs [#8](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/pull/8), [#9](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/pull/9), and [#10](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/pull/10).
- Warehouse workplace fixes and building-size/household formula improvements by jimocool2 in PRs [#11](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/pull/11) and [#12](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds/pull/12), with the roof allowance restricted during integration to detached, low-density houses.
- Thanks to [qstar-inc](https://github.com/qstar-inc), the ABC author, for identifying the namespace change needed to restore compatibility.

## Mod metadata

- **Mod ID**: 87755
- **Version**: 2.4.1
- **Game version target**: 1.6.*
- **Forum**: [Realistic Workplaces and Households](https://forum.paradoxplaza.com/forum/threads/realistic-workplaces-and-households.1699586/)
- **GitHub**: [github.com/ruzbeh0/RealisticWorkplacesAndHouseholds](https://github.com/ruzbeh0/RealisticWorkplacesAndHouseholds)
- **Discord**: [Channel link](https://discord.com/channels/1024242828114673724/1273862795020206144)
