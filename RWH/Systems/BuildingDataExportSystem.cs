using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    /// <summary>
    /// System responsible for exporting all Building Prefabs (Assets) to CSV files.
    /// This includes metrics like Dimensions, Height, and Capacities.
    /// </summary>
    public partial class BuildingDataExportSystem : GameSystemBase
    {
        private EntityQuery m_PrefabQuery;
        private PrefabSystem m_PrefabSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Query to find all valid Building Prefabs with relevant data components.
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
                    ComponentType.ReadOnly<PrisonData>()
                },
                None = new[] {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });
        }

        protected override void OnUpdate()
        {
            // Listen for the export trigger from Mod Settings.
            if (Mod.m_Setting.export_data_requested)
            {
                Mod.log.Info("[RWH Export] Export requested via Settings.");
                ExportDataToCSV();
                Mod.m_Setting.export_data_requested = false;
            }
        }

        /// <summary>
        /// Gathers data from prefabs and writes to CSV files in the user's My Documents folder.
        /// </summary>
        public void ExportDataToCSV()
        {
            Mod.log.Info("[RWH Export] Starting Prefab Export...");

            var entities = m_PrefabQuery.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"[RWH Export] Found {entities.Length} prefabs to process.");

            // --- 1. Initialize Component Lookups ---

            // Physical & Geometric Data
            var buildingDataLookup = GetComponentLookup<BuildingData>(true);
            var geometryLookup = GetComponentLookup<ObjectGeometryData>(true); // Used for Height

            // Gameplay Data
            var propertyLookup = GetComponentLookup<BuildingPropertyData>(true);
            var workplaceDataLookup = GetComponentLookup<WorkplaceData>(true);

            // Service Data
            var schoolLookup = GetComponentLookup<SchoolData>(true);
            var hospitalLookup = GetComponentLookup<HospitalData>(true);
            var prisonLookup = GetComponentLookup<PrisonData>(true);

            // Categorization & Metadata
            var spawnableLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);
            var ambienceLookup = GetComponentLookup<GroupAmbienceData>(true);
            var signatureLookup = GetComponentLookup<Signature>(true);
            var assetPackBufferLookup = GetBufferLookup<AssetPackElement>(true);

            // --- 2. Initialize CSV StringBuilders ---

            // Header includes 'Height' column
            string commonHeader = "Name,Type,Theme,AssetPack,Level,Height,Width,Length,Signature";

            // SpaceMultiplier removed from Residential CSV Header
            var resCsv = new StringBuilder(commonHeader + ",Households,HouseholdsPerCell\n");
            var workCsv = new StringBuilder(commonHeader + ",Workers,WorkersPerCell\n");
            var schoolCsv = new StringBuilder(commonHeader + ",StudentCapacity,StudentCapacityPerCell\n");
            var hospitalCsv = new StringBuilder(commonHeader + ",PatientCapacity,PatientCapacityPerCell\n");
            var prisonCsv = new StringBuilder(commonHeader + ",PrisonerCapacity,PrisonerCapacityPerCell\n");

            int countRes = 0, countWork = 0, countSchool = 0, countHosp = 0, countPrison = 0;

            // --- 3. Iterate Prefabs ---
            foreach (var entity in entities)
            {
                // Get Prefab Name
                string name = "";
                if (m_PrefabSystem.GetPrefab<PrefabBase>(entity) is PrefabBase prefabBase)
                {
                    name = prefabBase.name;
                }
                else
                {
                    name = EntityManager.GetName(entity);
                }

                bool isSignature = signatureLookup.HasComponent(entity);

                // Get Dimensions (Width, Length)
                int width = 0, length = 0;
                if (buildingDataLookup.TryGetComponent(entity, out var bData))
                {
                    width = bData.m_LotSize.x;
                    length = bData.m_LotSize.y;
                }

                // Get Height using ObjectGeometryData
                float height = 0f;
                if (geometryLookup.TryGetComponent(entity, out var geom))
                {
                    height = geom.m_Size.y;
                }

                // Get Level
                int level = 1;
                if (spawnableLookup.TryGetComponent(entity, out var sData))
                {
                    level = sData.m_Level;
                }

                // Calculate Base Area
                float area = (width * length);
                if (area == 0) area = 1f;

                // --- Determine Category, Theme, and Asset Pack ---
                string type = "OTHER";
                string theme = "No Theme";
                string assetPack = "Default";

                Entity zoneEntity = Entity.Null;
                if (sData.m_ZonePrefab != Entity.Null) zoneEntity = sData.m_ZonePrefab;

                // Determine Building Type
                if (zoneEntity != Entity.Null &&
                    zoneDataLookup.TryGetComponent(zoneEntity, out var zoneData) &&
                    ambienceLookup.TryGetComponent(zoneEntity, out var ambienceData))
                {
                    type = DetermineCategory(zoneData, ambienceData);
                }

                // Determine Asset Pack from Buffer
                if (assetPackBufferLookup.TryGetBuffer(entity, out var packs) && packs.Length > 0)
                {
                    assetPack = GetPackName(packs[0].m_Pack);
                }
                else if (zoneEntity != Entity.Null && assetPackBufferLookup.TryGetBuffer(zoneEntity, out packs) && packs.Length > 0)
                {
                    assetPack = GetPackName(packs[0].m_Pack);
                }

                // Construct common data line with Height included
                string commonLine = $"{name},{type},{theme},{assetPack},{level},{height:F1},{width},{length},{isSignature}";

                // --- 4. Append to Specific CSV ---

                // School Data
                if (schoolLookup.TryGetComponent(entity, out var school))
                {
                    schoolCsv.AppendLine($"{commonLine},{school.m_StudentCapacity},{(school.m_StudentCapacity / area):F2}");
                    countSchool++; continue;
                }
                // Hospital Data
                if (hospitalLookup.TryGetComponent(entity, out var hospital))
                {
                    hospitalCsv.AppendLine($"{commonLine},{hospital.m_PatientCapacity},{(hospital.m_PatientCapacity / area):F2}");
                    countHosp++; continue;
                }
                // Prison Data
                if (prisonLookup.TryGetComponent(entity, out var prison))
                {
                    prisonCsv.AppendLine($"{commonLine},{prison.m_PrisonerCapacity},{(prison.m_PrisonerCapacity / area):F2}");
                    countPrison++; continue;
                }
                // Residential Data
                if (propertyLookup.TryGetComponent(entity, out var prop))
                {
                    if (prop.m_ResidentialProperties > 0)
                    {
                        // Removed SpaceMultiplier from the output line
                        resCsv.AppendLine($"{commonLine},{prop.m_ResidentialProperties},{(prop.m_ResidentialProperties / area):F2}");
                        countRes++;
                    }
                }
                // Workplace Data
                if (workplaceDataLookup.TryGetComponent(entity, out var work))
                {
                    if (work.m_MaxWorkers > 0)
                    {
                        workCsv.AppendLine($"{commonLine},{work.m_MaxWorkers},{(work.m_MaxWorkers / area):F2}");
                        countWork++;
                    }
                }
            }

            Mod.log.Info($"[RWH Export] Summary: Res={countRes}, Work={countWork}, School={countSchool}, Hosp={countHosp}, Prison={countPrison}");

            // --- 5. Save Files ---
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            Mod.log.Info($"[RWH Export] Saving to: {basePath}");

            try
            {
                File.WriteAllText(Path.Combine(basePath, "RWH_Residential.csv"), resCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Workplaces.csv"), workCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Schools.csv"), schoolCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Hospitals.csv"), hospitalCsv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(basePath, "RWH_Prisons.csv"), prisonCsv.ToString(), Encoding.UTF8);
                Mod.log.Info("[RWH Export] Success!");
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"[RWH Export] File Write Error: {ex.Message}");
            }

            entities.Dispose();
        }

        /// <summary>
        /// Categorizes the building based on Zone AreaType and Ambience.
        /// </summary>
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
        /// Retrieves the Asset Pack name via PrefabSystem.
        /// </summary>
        private string GetPackName(Entity packEntity)
        {
            if (m_PrefabSystem.GetPrefab<PrefabBase>(packEntity) is PrefabBase packPrefab)
            {
                return packPrefab.name;
            }
            return EntityManager.GetName(packEntity);
        }
    }
}