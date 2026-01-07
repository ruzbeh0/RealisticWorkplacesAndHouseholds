using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using System.IO;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    /// <summary>
    /// System to export building prefab data to CSV files for analysis.
    /// Includes detailed metrics like Density per Cell, Zone Types, and Themes.
    /// </summary>
    public partial class BuildingDataExportSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            // Check if the export button was clicked in the settings
            if (Mod.m_Setting.export_data_requested)
            {
                Mod.log.Info("[RWH] Export started. Analyzing Zone Data Hierarchy...");
                ExportAllPrefabsToCSV();
                // Reset the flag to prevent multiple exports
                Mod.m_Setting.export_data_requested = false;
            }
        }

        private void ExportAllPrefabsToCSV()
        {
            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // 1. Prepare CSV Headers
            // Common columns for all building types
            string commonHeader = "Name,Type,Theme,AssetPack,Height,Width,Length,Workers,Households,Signature";

            // Specialized headers with 'Per Cell' density metrics
            var resCsv = new StringBuilder(commonHeader + ",HouseholdsPerCell\n");
            var workCsv = new StringBuilder(commonHeader + ",WorkersPerCell\n");
            var schoolCsv = new StringBuilder(commonHeader + ",StudentCapacity,StudentCapacityPerCell\n");
            var hospitalCsv = new StringBuilder(commonHeader + ",PatientCapacity,PatientCapacityPerCell\n");
            var prisonCsv = new StringBuilder(commonHeader + ",PrisonerCapacity,PrisonerCapacityPerCell\n");

            // 2. Query all Building Prefabs
            // We query PrefabData and BuildingData to get every buildable asset in the game
            EntityQuery prefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<BuildingData>());
            var prefabEntities = prefabQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in prefabEntities)
            {
                var prefabBase = prefabSystem.GetPrefab<BuildingPrefab>(entity);
                if (prefabBase == null) continue;

                string name = prefabBase.name;

                // Get Lot Size (Width x Length)
                if (!EntityManager.TryGetComponent<BuildingData>(entity, out var bData)) continue;
                int w = bData.m_LotSize.x;
                int l = bData.m_LotSize.y;
                // Calculate Area (Avoid division by zero)
                float area = math.max(1, w * l);

                // Get Building Height (from ObjectGeometryData, not BuildingData)
                float height = 0f;
                if (EntityManager.TryGetComponent<ObjectGeometryData>(entity, out var geom))
                {
                    height = geom.m_Size.y;
                }

                // 3. Extract Meta Data
                // Get Theme and Asset Pack using the Zone Hierarchy (No string parsing)
                GetThemeAndAssetPack(entity, prefabBase, prefabSystem, out string theme, out string assetPack);
                // Determine Building Type based on Zone properties (e.g., LOW RESIDENTIAL, OFFICE)
                string type = GetZoneBasedBuildingType(entity, prefabSystem);

                // 4. Extract Stats
                bool isSignature = EntityManager.HasComponent<SignatureBuildingData>(entity);
                int households = EntityManager.TryGetComponent<BuildingPropertyData>(entity, out var prop) ? prop.m_ResidentialProperties : 0;
                int workers = EntityManager.TryGetComponent<WorkplaceData>(entity, out var work) ? work.m_MaxWorkers : 0;

                // 5. Calculate Density Metrics (Value per 1x1 Lot)
                float householdsPerCell = households / area;
                float workersPerCell = workers / area;

                // Prepare the common data string
                string commonData = $"{name},{type},{theme},{assetPack},{height:F2},{w},{l},{workers},{households},{isSignature}";

                // 6. Append to respective CSVs
                // Residential
                if (households > 0)
                    resCsv.AppendLine($"{commonData},{householdsPerCell:F2}");

                // Workplaces
                if (workers > 0)
                    workCsv.AppendLine($"{commonData},{workersPerCell:F2}");

                // Schools
                if (EntityManager.TryGetComponent<SchoolData>(entity, out var school))
                {
                    float capPerCell = school.m_StudentCapacity / area;
                    schoolCsv.AppendLine($"{commonData},{school.m_StudentCapacity},{capPerCell:F2}");
                }

                // Hospitals
                if (EntityManager.TryGetComponent<HospitalData>(entity, out var hospital))
                {
                    float capPerCell = hospital.m_PatientCapacity / area;
                    hospitalCsv.AppendLine($"{commonData},{hospital.m_PatientCapacity},{capPerCell:F2}");
                }

                // Prisons
                if (EntityManager.TryGetComponent<PrisonData>(entity, out var prison))
                {
                    float capPerCell = prison.m_PrisonerCapacity / area;
                    prisonCsv.AppendLine($"{commonData},{prison.m_PrisonerCapacity},{capPerCell:F2}");
                }
            }

            // 7. Save Files to 'Documents' folder
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            SaveFile(Path.Combine(docPath, "RWH_All_Residential.csv"), resCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Workplaces.csv"), workCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Schools.csv"), schoolCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Hospitals.csv"), hospitalCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Prisons.csv"), prisonCsv);

            Mod.log.Info($"[RWH] Export Complete. Density metrics included.");
            prefabEntities.Dispose();
        }

        /// <summary>
        /// Retrieves Theme and AssetPack by traversing the Zone Hierarchy.
        /// This ensures accurate detection even if the building name doesn't contain country codes.
        /// </summary>
        private void GetThemeAndAssetPack(Entity entity, BuildingPrefab buildingPrefab, PrefabSystem prefabSystem, out string theme, out string assetPack)
        {
            theme = "No Theme";
            assetPack = "Default";

            // Case 1: Growable Buildings (Residential/Commercial/Industrial)
            // These buildings belong to a Zone, so we check the ZonePrefab.
            if (EntityManager.TryGetComponent<SpawnableBuildingData>(entity, out var spawnData))
            {
                if (prefabSystem.TryGetPrefab<ZonePrefab>(spawnData.m_ZonePrefab, out var zonePrefab))
                {
                    // A. Check Theme from Zone
                    var zoneThemeObj = zonePrefab.GetComponent<ThemeObject>();
                    if (zoneThemeObj != null && zoneThemeObj.m_Theme != null)
                        theme = zoneThemeObj.m_Theme.name;

                    // B. Check AssetPack from Zone
                    // ZonePrefabs use 'AssetPackItem' instead of a buffer.
                    var zoneAssetPackItem = zonePrefab.GetComponent<AssetPackItem>();
                    if (zoneAssetPackItem != null && zoneAssetPackItem.m_Packs != null && zoneAssetPackItem.m_Packs.Length > 0)
                    {
                        assetPack = zoneAssetPackItem.m_Packs[0].name;
                    }
                }
            }

            // Case 2: Ploppable Buildings (Schools, Services, Signatures) or Fallback
            // These buildings carry the Theme/AssetPack directly on themselves.
            if (theme == "No Theme")
            {
                var bThemeObj = buildingPrefab.GetComponent<ThemeObject>();
                if (bThemeObj != null && bThemeObj.m_Theme != null)
                    theme = bThemeObj.m_Theme.name;
            }

            // Check for AssetPack buffer on the building entity
            if (assetPack == "Default" && EntityManager.HasBuffer<AssetPackElement>(entity))
            {
                var packs = EntityManager.GetBuffer<AssetPackElement>(entity);
                if (packs.Length > 0)
                    assetPack = prefabSystem.GetPrefab<PrefabBase>(packs[0].m_Pack).name;
            }
        }

        /// <summary>
        /// Classifies the building type based on Components (for services) or Zone Properties (for growables).
        /// </summary>
        private string GetZoneBasedBuildingType(Entity entity, PrefabSystem prefabSystem)
        {
            // 1. Identify Service and Special Buildings by Component
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

            // 2. Identify Growables based on Zone Properties
            Entity zoneEntity = Entity.Null;
            if (EntityManager.TryGetComponent<SpawnableBuildingData>(entity, out var spawnData))
                zoneEntity = spawnData.m_ZonePrefab;
            else if (EntityManager.TryGetComponent<PlaceholderBuildingData>(entity, out var phData))
                zoneEntity = phData.m_ZonePrefab;

            if (zoneEntity != Entity.Null)
            {
                if (prefabSystem.TryGetPrefab<ZonePrefab>(zoneEntity, out var zonePrefab))
                {
                    var zoneData = EntityManager.GetComponentData<ZoneData>(zoneEntity);
                    var ambienceData = EntityManager.GetComponentData<GroupAmbienceData>(zoneEntity);

                    // We use the Zone Name to infer density (Low/Med/High) as it's the most reliable way.
                    string zoneName = zonePrefab.name.ToUpper();

                    // [Residential]
                    if (zoneData.m_AreaType == AreaType.Residential)
                    {
                        if (ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed) return "MIXED HOUSING";
                        if ((zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) != 0) return "ROW HOUSE";

                        if (zoneName.Contains("LOW")) return "LOW RESIDENTIAL";
                        if (zoneName.Contains("MEDIUM") || zoneName.Contains("MED")) return "MEDIUM RESIDENTIAL";
                        if (zoneName.Contains("HIGH")) return "HIGH RESIDENTIAL";
                        return "RESIDENTIAL";
                    }

                    // [Commercial]
                    if (zoneData.m_AreaType == AreaType.Commercial)
                    {
                        if (zoneName.Contains("LOW")) return "LOW COMMERCIAL";
                        if (zoneName.Contains("HIGH")) return "HIGH COMMERCIAL";
                        return "COMMERCIAL";
                    }

                    // [Industrial & Office]
                    if (zoneData.m_AreaType == AreaType.Industrial)
                    {
                        // Office is technically a subtype of Industrial in Zone properties
                        if (zonePrefab.m_Office)
                        {
                            if (zoneName.Contains("LOW")) return "LOW OFFICE";
                            if (zoneName.Contains("HIGH")) return "HIGH OFFICE";
                            return "OFFICE";
                        }

                        if (ambienceData.m_AmbienceType == GroupAmbienceType.Industrial) return "INDUSTRY";
                        if (EntityManager.HasComponent<ExtractorFacilityData>(entity)) return "EXTRACTOR";
                        return "INDUSTRY_SPECIALIZED";
                    }
                }
            }
            return "OTHER";
        }

        private void SaveFile(string path, StringBuilder content)
        {
            try { File.WriteAllText(path, content.ToString(), Encoding.UTF8); }
            catch (Exception ex) { Mod.log.Error($"Failed to save {path}: {ex.Message}"); }
        }
    }
}