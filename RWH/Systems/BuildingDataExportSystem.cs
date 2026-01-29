// <copyright file="BuildingDataExportSystem.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using System;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    /// <summary>
    /// A system that, when triggered from the mod's settings, exports detailed data about all building prefabs to CSV files.
    /// This serves as a diagnostic and data generation tool, helping to create default configurations for the mod
    /// and allowing players or asset creators to see how the game and this mod interpret building data.
    /// The export is a one-time operation initiated by the user.
    /// </summary>
    public partial class BuildingDataExportSystem : GameSystemBase
    {
        private EntityQuery m_PrefabQuery;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Called when the system is created. This method sets up the necessary systems and queries.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // Cache the PrefabSystem for efficient access to prefab data.
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // This query is designed to find all relevant building prefabs.
            // It looks for entities that have PrefabData and BuildingData, which are common to all buildings.
            // The 'Any' clause includes a wide range of components to capture all types of buildings:
            // residential, commercial, industrial, offices, and various city services.
            // The 'None' clause filters out temporary or deleted entities to ensure data integrity.
            m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<BuildingData>()
                },
                Any = new[] {
                    ComponentType.ReadOnly<BuildingPropertyData>(),
                    ComponentType.ReadOnly<WorkplaceData>(),
                    ComponentType.ReadOnly<SchoolData>(),
                    ComponentType.ReadOnly<HospitalData>(),
                    ComponentType.ReadOnly<PrisonData>(),
                    ComponentType.ReadOnly<PoliceStationData>(),
                    ComponentType.ReadOnly<FireStationData>(),
                    ComponentType.ReadOnly<ParkData>(),
                    ComponentType.ReadOnly<WelfareOfficeData>(),
                    ComponentType.ReadOnly<MaintenanceDepotData>(),
                    ComponentType.ReadOnly<PostFacilityData>(),
                    ComponentType.ReadOnly<TransportDepotData>(),
                    ComponentType.ReadOnly<GarbageFacilityData>(),
                    ComponentType.ReadOnly<PowerPlantData>(),
                    ComponentType.ReadOnly<WaterPumpingStationData>(),
                    ComponentType.ReadOnly<SewageOutletData>(),
                    ComponentType.ReadOnly<ParkingFacilityData>(),
                    ComponentType.ReadOnly<SpawnableBuildingData>(),
                },
                None = new[] {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });
        }

        /// <summary>
        /// Called every frame. The system continuously checks if the user has requested a data export
        /// via a button in the mod's settings menu.
        /// </summary>
        protected override void OnUpdate()
        {
            // The 'export_data_requested' boolean is a flag set by the mod's UI when the user clicks the export button.
            if (Mod.m_Setting.export_data_requested)
            {
                Mod.log.Info("[RWH Export] Export requested via Settings.");
                // Trigger the main export logic.
                ExportDataToCSV();
                // Reset the flag immediately to prevent the export from running on every frame until the setting is saved again.
                Mod.m_Setting.export_data_requested = false;
            }
        }

        /// <summary>
        /// Provides a multiplier for industrial workplace calculations based on the manufactured resource.
        /// This models the concept that different industries have different labor requirements.
        /// For example, textile manufacturing is more labor-intensive than vehicle assembly per square meter.
        /// </summary>
        /// <param name="resource">The primary resource produced by the industrial building.</param>
        /// <returns>A float multiplier to adjust worker density.</returns>
        private float GetIndustryFactor(Game.Economy.Resource resource)
        {
            // This switch statement assigns a factor based on the resource type.
            // Values > 1.0f mean more workers per area, < 1.0f mean fewer workers.
            return resource switch
            {
                Game.Economy.Resource.Furniture => 1.2f,
                Game.Economy.Resource.Textiles => 1.3f,
                Game.Economy.Resource.Chemicals => 0.8f,
                Game.Economy.Resource.Plastics => 0.9f,
                Game.Economy.Resource.Electronics => 0.7f,
                Game.Economy.Resource.Vehicles => 0.6f,
                Game.Economy.Resource.Food => 1.1f,
                Game.Economy.Resource.Beverages => 1.05f,
                Game.Economy.Resource.Paper => 0.9f,
                Game.Economy.Resource.Pharmaceuticals => 0.75f,
                Game.Economy.Resource.Fish => 3f, // Fishing is very labor-intensive
                _ => 1.0f // Default factor for any unlisted or new resources.
            };
        }

        /// <summary>
        /// Calculates the number of workers for a RICO (Residential, Industrial, Commercial, Office) building.
        /// This logic is a recreation of the calculations used by the mod at runtime to determine workplace counts,
        /// ensuring that the exported data accurately reflects what the mod will do in the game.
        /// </summary>
        /// <param name="width">The building's footprint width.</param>
        /// <param name="length">The building's footprint length.</param>
        /// <param name="height">The building's total height.</param>
        /// <param name="zoneData">The ZoneData component from the building's zone prefab.</param>
        /// <param name="ambienceData">The GroupAmbienceData from the zone prefab, used for theme identification.</param>
        /// <param name="prefabEntity">The entity of the building prefab itself.</param>
        /// <param name="propertyLookup">A lookup for accessing BuildingPropertyData components.</param>
        /// <param name="industrialProcessLookup">A lookup for accessing IndustrialProcessData components.</param>
        /// <returns>The calculated number of workers for the building.</returns>
        private int CalculateRICOWorkers(float width, float length, float height, ZoneData zoneData, GroupAmbienceData ambienceData, Entity prefabEntity,
            ComponentLookup<BuildingPropertyData> propertyLookup, ComponentLookup<IndustrialProcessData> industrialProcessLookup)
        {
            int calculatedWorkers = 0;

            // Assumes taller buildings have a non-working ground floor (lobby, etc.).
            int floor_offset = (zoneData.m_MaxHeight > 25) ? 1 : 0;

            // The UsableFootprintFactor is a runtime value, so we assume a default for the prefab export.
            // This matches the default used in the mod's job system.

            if (zoneData.m_AreaType == Game.Zones.AreaType.Residential)
            {
                // In this mod's model, only "Mixed" residential buildings contain workplaces.
                if (ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed)
                {
                    // These are typically ground-floor commercial shops.
                    calculatedWorkers = BuildingUtils.GetPeople(width, length, height, Mod.m_Setting.commercial_avg_floor_height, Mod.m_Setting.commercial_sqm_per_worker, 0, 0, 1);
                }
            }
            else if (zoneData.m_AreaType == Game.Zones.AreaType.Commercial)
            {
                float area = Mod.m_Setting.commercial_sqm_per_worker;
                if (propertyLookup.TryGetComponent(prefabEntity, out var buildingPropertyData))
                {
                    var resource = buildingPropertyData.m_AllowedSold;
                    // Apply different worker densities based on the type of goods sold.
                    if (resource == Game.Economy.Resource.Petrochemicals && Mod.m_Setting.commercial_self_service_gas)
                        area *= 1.8f; // Gas stations have few workers for their size.
                    else if (resource == Game.Economy.Resource.Meals)
                        area = Mod.m_Setting.commercial_sqm_per_worker_restaurants;
                    else if (resource == Game.Economy.Resource.Beverages || resource == Game.Economy.Resource.ConvenienceFood || resource == Game.Economy.Resource.Food)
                        area = Mod.m_Setting.commercial_sqm_per_worker_supermarket;
                    else if (resource == Game.Economy.Resource.Recreation || resource == Game.Economy.Resource.Entertainment)
                        area = Mod.m_Setting.commercial_sqm_per_worker_rec_entertainment;
                    
                    // Adjust worker density based on footprint size to model economies of scale.
                    area *= BuildingUtils.smooth_area_factor(70 * 70, width, length);
                }
                calculatedWorkers = BuildingUtils.GetPeople(width, length, height, Mod.m_Setting.commercial_avg_floor_height, area, 0, Mod.m_Setting.office_elevators_per_sqm);
            }
            else if ((zoneData.m_ZoneFlags & ZoneFlags.Office) != 0)
            {
                float employee_area = Mod.m_Setting.office_sqm_per_worker;
                // Account for non-usable space (hallways, elevators, etc.).
                float area = employee_area * (1 + Mod.m_Setting.office_non_usable_space / 100f);
                // Taller office buildings are slightly less space-efficient.
                float height_factor = BuildingUtils.smooth_height_factor(Mod.m_Setting.office_height_base, height / Mod.m_Setting.commercial_avg_floor_height);
                calculatedWorkers = BuildingUtils.GetPeople(width, length, height, Mod.m_Setting.commercial_avg_floor_height, area * height_factor, floor_offset, Mod.m_Setting.office_elevators_per_sqm);
            }
            else // Industrial
            {
                float area_factor = BuildingUtils.smooth_area_factor4(Mod.m_Setting.industry_area_base, width, length);
                if (industrialProcessLookup.TryGetComponent(prefabEntity, out var industrialProcessData))
                {
                    var resource = industrialProcessData.m_Output.m_Resource;
                    // Extractor buildings (farms, mines, etc.) are handled by a different system and are skipped here.
                    if (resource == Game.Economy.Resource.Wood || resource == Game.Economy.Resource.Vegetables || resource == Game.Economy.Resource.Cotton || resource == Game.Economy.Resource.Grain ||
                        resource == Game.Economy.Resource.Livestock || resource == Game.Economy.Resource.Oil || resource == Game.Economy.Resource.Ore || resource == Game.Economy.Resource.Coal || resource == Game.Economy.Resource.Stone)
                    {
                        return 0;
                    }
                    // Apply the industry-specific labor multiplier.
                    area_factor *= GetIndustryFactor(resource);
                }
                // Industrial buildings are assumed to have a maximum of 2 usable floors for worker calculations.
                calculatedWorkers = BuildingUtils.GetPeople(width, length, height, Mod.m_Setting.industry_avg_floor_height, Mod.m_Setting.industry_sqm_per_worker * area_factor, 0, 0, 2);
            }

            // Apply the global results reduction percentage from settings, which acts as a final tuning parameter.
            return (int)(calculatedWorkers * (1f - Mod.m_Setting.results_reduction / 100f));
        }


        /// <summary>
        /// This is the main export function. It iterates through all prefabs found by the system's query,
        /// categorizes them, calculates relevant data (like worker or household counts), and writes the
        /// results to several CSV files in the user's Documents folder.
        /// </summary>
        public void ExportDataToCSV()
        {
            Mod.log.Info("[RWH Export] Starting Prefab Export...");

            var entities = m_PrefabQuery.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"[RWH Export] Found {entities.Length} prefabs to process.");

            // --- 1. Component Lookups ---
            // ComponentLookups are cached here for performance. Accessing them inside a tight loop
            // is much faster than calling World.GetComponentLookup repeatedly. The 'true' argument
            // indicates that these are read-only lookups, which can be faster.
            var buildingDataLookup = GetComponentLookup<BuildingData>(true);
            var propertyLookup = GetComponentLookup<BuildingPropertyData>(true);
            var workplaceDataLookup = GetComponentLookup<WorkplaceData>(true);
            var industrialProcessLookup = GetComponentLookup<IndustrialProcessData>(true);
            var subMeshLookup = GetBufferLookup<SubMesh>(true);
            var meshDataLookup = GetComponentLookup<MeshData>(true);
            var schoolLookup = GetComponentLookup<SchoolData>(true);
            var hospitalLookup = GetComponentLookup<HospitalData>(true);
            var prisonLookup = GetComponentLookup<PrisonData>(true);
            var spawnableLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);
            var ambienceLookup = GetComponentLookup<GroupAmbienceData>(true);
            var signatureLookup = GetComponentLookup<Signature>(true);
            var assetPackBufferLookup = GetBufferLookup<AssetPackElement>(true);
            var geometryLookup = GetComponentLookup<ObjectGeometryData>(true);

            // --- 2. CSV Builders ---
            // StringBuilders are used to efficiently construct the CSV content line-by-line.
            // A common header is defined for consistency across all output files.
            string commonHeader = "Name,Type,Theme,AssetPack,Level,Height,Width,Length,Signature";
            var resCsv = new StringBuilder(commonHeader + ",Households,HouseholdsPerCell\n");
            var workCsv = new StringBuilder(commonHeader + ",Workers,WorkersPerCell\n");
            var schoolCsv = new StringBuilder(commonHeader + ",StudentCapacity,StudentCapacityPerCell\n");
            var hospitalCsv = new StringBuilder(commonHeader + ",PatientCapacity,PatientCapacityPerCell\n");
            var prisonCsv = new StringBuilder(commonHeader + ",PrisonerCapacity,PrisonerCapacityPerCell\n");

            int countRes = 0, countWork = 0, countSchool = 0, countHosp = 0, countPrison = 0;

            // --- 3. Iterate Through Prefabs ---
            // This is the main loop where each building prefab is processed.
            foreach (var entity in entities)
            {
                // Get the prefab's name for identification in the CSV file.
                string name = "";
                if (m_PrefabSystem.GetPrefab<PrefabBase>(entity) is PrefabBase prefabBase)
                {
                    name = prefabBase.name;
                }
                else
                {
                    // Fallback to the entity name if prefab base is not available.
                    name = EntityManager.GetName(entity);
                }

                // A "Signature" building is a unique, ploppable building (like a landmark).
                bool isSignature = signatureLookup.HasComponent(entity);

                // Calculate the building's dimensions directly from its meshes. This is more reliable
                // than using prefab data, which can sometimes be inaccurate.
                float width = 0, length = 0, height = 0;
                if (subMeshLookup.TryGetBuffer(entity, out var subMeshes) && subMeshes.Length > 0)
                {
                    var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                    var size = Game.Objects.ObjectUtils.GetSize(dimensions);
                    width = size.x;
                    length = size.z;
                    height = size.y;
                }

                // For spawnable (growable) buildings, get their level and a reference to their parent zone prefab.
                int level = 5; // Default level for buildings where it's not specified (e.g., services).
                Entity zoneEntity = Entity.Null;
                if (spawnableLookup.TryGetComponent(entity, out var sData))
                {
                    level = sData.m_Level;
                    zoneEntity = sData.m_ZonePrefab;
                }

                float area = (width * length);
                if (area == 0) area = 1f; // Avoid division by zero for buildings with no valid footprint.

                // --- Category & Type Logic ---
                // Determine the building's primary category (e.g., Police, Commercial, Industry).
                string type = "OTHER";
                ZoneData zoneData = default;
                GroupAmbienceData ambienceData = default;
                bool isRICOWorkplace = false;

                // First, check if the prefab is a known city service.
                string serviceType = GetServiceCategory(entity);
                if (serviceType != null)
                {
                    type = serviceType;
                }
                else
                {
                    // If not a service, check if it's a spawnable (growable) RICO building.
                    // This requires a valid link to a ZonePrefab via SpawnableBuildingData.
                    if (zoneEntity != Entity.Null &&
                        zoneDataLookup.TryGetComponent(zoneEntity, out zoneData) &&
                        ambienceLookup.TryGetComponent(zoneEntity, out ambienceData))
                    {
                        type = DetermineCategory(zoneData, ambienceData);

                        // Check if the zone type is one that this mod considers a workplace.
                        if ((zoneData.m_AreaType == AreaType.Residential && ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed) ||
                            zoneData.m_AreaType == AreaType.Commercial ||
                            zoneData.m_AreaType == AreaType.Industrial)
                        {
                            isRICOWorkplace = true;
                        }
                    }
                    // If it's not a service or a spawnable RICO, but it has a WorkplaceData component,
                    // it's likely a ploppable-only unique building that provides jobs.
                    else if (workplaceDataLookup.HasComponent(entity))
                    {
                        type = "WORKPLACE_UNCATEGORIZED";
                    }
                }

                // --- Asset Pack & Theme ---
                // Try to determine the asset's theme and its parent asset pack for better identification.
                string theme = "No Theme";
                string assetPack = "Default";

                if (assetPackBufferLookup.TryGetBuffer(entity, out var packs) && packs.Length > 0)
                {
                    assetPack = GetPackName(packs[0].m_Pack);
                }
                else if (zoneEntity != Entity.Null && assetPackBufferLookup.TryGetBuffer(zoneEntity, out packs) && packs.Length > 0)
                {
                    // Fallback to the zone's asset pack if the building doesn't have one directly.
                    assetPack = GetPackName(packs[0].m_Pack);
                }

                // This is the common data string shared by all CSV files for a single asset.
                string commonLine = $"{name},{type},{theme},{assetPack},{level},{height:F1},{width:F1},{length:F1},{isSignature}";

                // --- 4. Append Data to the correct CSV Builders ---

                // Handle specialized service buildings first, as they have unique capacity types.
                if (schoolLookup.TryGetComponent(entity, out var school))
                {
                    schoolCsv.AppendLine($"{commonLine},{school.m_StudentCapacity},{(school.m_StudentCapacity / area):F2}");
                    countSchool++;
                }
                if (hospitalLookup.TryGetComponent(entity, out var hospital))
                {
                    hospitalCsv.AppendLine($"{commonLine},{hospital.m_PatientCapacity},{(hospital.m_PatientCapacity / area):F2}");
                    countHosp++;
                }
                if (prisonLookup.TryGetComponent(entity, out var prison))
                {
                    prisonCsv.AppendLine($"{commonLine},{prison.m_PrisonerCapacity},{(prison.m_PrisonerCapacity / area):F2}");
                    countPrison++;
                }

                // Handle residential buildings (which provide households, not jobs).
                if (propertyLookup.TryGetComponent(entity, out var prop))
                {
                    if (prop.m_ResidentialProperties > 0)
                    {
                        resCsv.AppendLine($"{commonLine},{prop.m_ResidentialProperties},{(prop.m_ResidentialProperties / area):F2}");
                        countRes++;
                    }
                }

                // --- Workplace Calculation Logic ---
                bool hasWorkplaceData = workplaceDataLookup.HasComponent(entity);

                // A building is considered a potential workplace if it's an identified spawnable RICO building
                // OR if it explicitly has a WorkplaceData component (e.g., city services, unique buildings).
                if (hasWorkplaceData || isRICOWorkplace)
                {
                    int workers = 0;
                    if (isRICOWorkplace)
                    {
                        // For spawnable RICO buildings, ALWAYS recalculate the worker count using the mod's detailed logic.
                        // This overrides any default value in the prefab, which is often 0 or a placeholder.
                        workers = CalculateRICOWorkers(width, length, height, zoneData, ambienceData, entity, propertyLookup, industrialProcessLookup);
                    }
                    else if (hasWorkplaceData)
                    {
                        // For other workplaces (services, unique buildings), read the value directly from the prefab.
                        // This mod generally does not override the values for these building types.
                        workers = workplaceDataLookup[entity].m_MaxWorkers;

                        // This log is crucial for asset creators. It helps identify assets that are intended to be "ploppable RICO"
                        // but are not configured correctly (they lack SpawnableBuildingData). These assets often have 0 workers
                        // in their prefab, expecting a mod like this one to calculate the "real" value at runtime.
                        if (workers == 0 && GetServiceCategory(entity) == null)
                        {
                             Mod.log.Info($"[RWH Export] Non-service workplace '{name}' has 0 workers in prefab and was not identified as a spawnable RICO for calculation. It may be a ploppable-only RICO asset that needs manual configuration.");
                        }
                    }

                    // If a valid number of workers was found or calculated, add an entry to the workplaces CSV.
                    if (workers > 0)
                    {
                        workCsv.AppendLine($"{commonLine},{workers},{(workers / area):F2}");
                        countWork++;
                    }
                    // Also add an entry if it was a RICO building that calculated to 0 workers.
                    // This is useful for debugging why a certain building isn't getting any jobs.
                    else if (isRICOWorkplace)
                    {
                        workCsv.AppendLine($"{commonLine},0,0.00");
                        countWork++;
                        Mod.log.Info($"[RWH Export] Spawnable RICO Workplace '{name}' has 0 calculated workers.");
                    }
                }
            }

            Mod.log.Info($"[RWH Export] Summary: Residential={countRes}, Workplaces={countWork}, Schools={countSchool}, Hospitals={countHosp}, Prisons={countPrison}");

            // --- 5. Save CSV Files ---
            // The CSV files are saved to the user's "My Documents" folder, which is a standard, easily accessible location.
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            Mod.log.Info($"[RWH Export] Saving files to: {basePath}");

            try
            {
                // Write all the generated CSV content to their respective files using UTF8 encoding to support all character sets.
                File.WriteAllText(Path.Combine(basePath, "RWH_Residential.csv"), resCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Workplaces.csv"), workCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Schools.csv"), schoolCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Hospitals.csv"), hospitalCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Prisons.csv"), prisonCsv.ToString(), Encoding.UTF8);
                Mod.log.Info("[RWH Export] Successfully saved all CSV files!");
            }
            catch (System.Exception ex)
            {
                // Log any errors that occur during file writing, such as permission issues.
                Mod.log.Error($"[RWH Export] File Write Error: {ex.Message}");
            }

            // Clean up the native array to prevent memory leaks.
            entities.Dispose();
        }
        
        /// <summary>
        /// Determines a human-readable category string based on a building's zone and ambience data.
        /// This is used to make the exported CSV data easier for users to understand.
        /// </summary>
        /// <param name="zoneData">The building's associated ZoneData.</param>
        /// <param name="ambienceData">The building's associated GroupAmbienceData.</param>
        /// <returns>A string representing the building category (e.g., "LOW RESIDENTIAL", "HIGH COMMERCIAL").</returns>
        private string DetermineCategory(ZoneData zoneData, GroupAmbienceData ambienceData)
        {
            if (zoneData.m_AreaType == AreaType.Residential)
            {
                if (ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialLowRent) return "LOW RENT";
                if (ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed) return "MIXED HOUSING";
                if ((zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) != 0) return "ROW HOUSE";

                return ambienceData.m_AmbienceType switch
                {
                    GroupAmbienceType.ResidentialLow => "LOW RESIDENTIAL",
                    GroupAmbienceType.ResidentialMedium => "MEDIUM RESIDENTIAL",
                    GroupAmbienceType.ResidentialHigh => "HIGH RESIDENTIAL",
                    _ => "RESIDENTIAL"
                };
            }
            if (zoneData.m_AreaType == AreaType.Commercial)
            {
                if (ambienceData.m_AmbienceType == GroupAmbienceType.CommercialLow) return "LOW COMMERCIAL";
                if (ambienceData.m_AmbienceType == GroupAmbienceType.CommercialHigh) return "HIGH COMMERCIAL";
                return "COMMERCIAL";
            }
            if (zoneData.m_AreaType == AreaType.Industrial)
            {
                if ((zoneData.m_ZoneFlags & ZoneFlags.Office) != 0)
                    return ambienceData.m_AmbienceType == GroupAmbienceType.OfficeHigh ? "HIGH OFFICE" : "LOW OFFICE";

                if (ambienceData.m_AmbienceType == GroupAmbienceType.Industrial) return "INDUSTRY";
                return "INDUSTRY_SPECIALIZED";
            }
            return "OTHER";
        }

        /// <summary>
        /// Retrieves the name of an Asset Pack from its prefab entity.
        /// </summary>
        /// <param name="packEntity">The entity of the asset pack prefab.</param>
        /// <returns>The name of the asset pack, or the entity name as a fallback.</returns>
        private string GetPackName(Entity packEntity)
        {
            if (m_PrefabSystem.GetPrefab<PrefabBase>(packEntity) is PrefabBase packPrefab)
            {
                return packPrefab.name;
            }
            return EntityManager.GetName(packEntity);
        }

        /// <summary>
        /// Determines if a building belongs to a specific city service category by checking for the presence of specific components.
        /// </summary>
        /// <param name="entity">The building prefab entity to check.</param>
        /// <returns>A string representing the service category (e.g., "POLICE"), or null if it's not a recognized service.</returns>
        private string GetServiceCategory(Entity entity)
        {
            if (EntityManager.HasComponent<PoliceStationData>(entity)) return "POLICE";
            if (EntityManager.HasComponent<FireStationData>(entity)) return "FIRE";
            if (EntityManager.HasComponent<HospitalData>(entity)) return "HOSPITAL";
            if (EntityManager.HasComponent<SchoolData>(entity)) return "SCHOOL";
            if (EntityManager.HasComponent<ParkData>(entity)) return "PARK";
            if (EntityManager.HasComponent<WelfareOfficeData>(entity)) return "WELFARE";
            if (EntityManager.HasComponent<MaintenanceDepotData>(entity)) return "MAINTENANCE";
            if (EntityManager.HasComponent<PostFacilityData>(entity)) return "POST";
            if (EntityManager.HasComponent<TransportDepotData>(entity)) return "DEPOT";
            if (EntityManager.HasComponent<GarbageFacilityData>(entity)) return "GARBAGE";
            if (EntityManager.HasComponent<PowerPlantData>(entity)) return "POWER PLANT";
            if (EntityManager.HasComponent<WaterPumpingStationData>(entity)) return "WATER";
            if (EntityManager.HasComponent<SewageOutletData>(entity)) return "SEWAGE";
            if (EntityManager.HasComponent<ParkingFacilityData>(entity)) return "PARKING";
            if (EntityManager.HasComponent<PrisonData>(entity)) return "PRISON";

            // If no specific service component is found, it's not a service building.
            return null;
        }
    }
}