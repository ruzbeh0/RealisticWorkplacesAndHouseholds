using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;

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

            // --- 1. Lookups ---
            var buildingDataLookup = GetComponentLookup<BuildingData>(true);
            var propertyLookup = GetComponentLookup<BuildingPropertyData>(true);
            var workplaceDataLookup = GetComponentLookup<WorkplaceData>(true);

            var schoolLookup = GetComponentLookup<SchoolData>(true);
            var hospitalLookup = GetComponentLookup<HospitalData>(true);
            var prisonLookup = GetComponentLookup<PrisonData>(true);

            var spawnableLookup = GetComponentLookup<SpawnableBuildingData>(true);
            var zoneDataLookup = GetComponentLookup<ZoneData>(true);
            var ambienceLookup = GetComponentLookup<GroupAmbienceData>(true);
            var signatureLookup = GetComponentLookup<Signature>(true);

            var assetPackBufferLookup = GetBufferLookup<AssetPackElement>(true);
            var geometryLookup = GetComponentLookup<ObjectGeometryData>(true);

            // --- 2. Headers ---
            string commonHeader = "Name,Type,Theme,AssetPack,Level,Height,Width,Length,Signature";

            var resCsv = new StringBuilder(commonHeader + ",Households,HouseholdsPerCell\n");
            var workCsv = new StringBuilder(commonHeader + ",Workers,WorkersPerCell\n");
            var schoolCsv = new StringBuilder(commonHeader + ",StudentCapacity,StudentCapacityPerCell\n");
            var hospitalCsv = new StringBuilder(commonHeader + ",PatientCapacity,PatientCapacityPerCell\n");
            var prisonCsv = new StringBuilder(commonHeader + ",PrisonerCapacity,PrisonerCapacityPerCell\n");

            int countRes = 0, countWork = 0, countSchool = 0, countHosp = 0, countPrison = 0;

            // --- 3. Iterate ---
            foreach (var entity in entities)
            {
                // Name
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

                // Dimensions
                int width = 0, length = 0;
                if (buildingDataLookup.TryGetComponent(entity, out var bData))
                {
                    width = bData.m_LotSize.x;
                    length = bData.m_LotSize.y;
                }

                float height = 0f;
                if (geometryLookup.TryGetComponent(entity, out var geom))
                {
                    height = geom.m_Size.y;
                }

                int level = 5;
                Entity zoneEntity = Entity.Null;

                // Spawnable Data (For Level and Zone Prefab)
                if (spawnableLookup.TryGetComponent(entity, out var sData))
                {
                    level = sData.m_Level;
                    zoneEntity = sData.m_ZonePrefab;
                }

                float area = (width * length);
                if (area == 0) area = 1f;

                // --- Category Logic ---
                string type = "OTHER";

                ZoneData zoneData = default;
                bool zoneDataExists = false;
                GroupAmbienceData ambienceData = default;
                bool ambienceDataExists = false;

                // 1. Check Service
                string serviceType = GetServiceCategory(entity);
                if (serviceType != null)
                {
                    type = serviceType;
                }
                else
                {
                    // 2. Check Zone (RCI)
                    if (zoneEntity != Entity.Null &&
                        zoneDataLookup.TryGetComponent(zoneEntity, out zoneData, out zoneDataExists) &&
                        ambienceLookup.TryGetComponent(zoneEntity, out ambienceData, out ambienceDataExists))
                    {
                        type = DetermineCategory(zoneData, ambienceData);
                    }
                    // 3. Workplace Uncategorized
                    else if (workplaceDataLookup.HasComponent(entity))
                    {
                        type = "WORKPLACE_UNCATEGORIZED";
                    }
                }

                // --- Asset Pack ---
                string theme = "No Theme";
                string assetPack = "Default";

                if (assetPackBufferLookup.TryGetBuffer(entity, out var packs) && packs.Length > 0)
                {
                    assetPack = GetPackName(packs[0].m_Pack);
                }
                else if (zoneEntity != Entity.Null && assetPackBufferLookup.TryGetBuffer(zoneEntity, out packs) && packs.Length > 0)
                {
                    assetPack = GetPackName(packs[0].m_Pack);
                }

                string commonLine = $"{name},{type},{theme},{assetPack},{level},{height:F1},{width},{length},{isSignature}";

                // --- 4. Append Data ---

                // Service Buildings
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

                // Residential
                if (propertyLookup.TryGetComponent(entity, out var prop))
                {
                    if (prop.m_ResidentialProperties > 0)
                    {
                        resCsv.AppendLine($"{commonLine},{prop.m_ResidentialProperties},{(prop.m_ResidentialProperties / area):F2}");
                        countRes++;
                    }
                }
                int workers = EntityManager.TryGetComponent<WorkplaceData>(entity, out var work) ? work.m_MaxWorkers : 0;
                // Workplace Debug: RICO workplaces print issue
                // When RICO's WorkPlace is print properly without using isRICOWorkplace, workers count is well defined & loaded.
                bool isRICOWorkplace = false;
                if (zoneEntity != Entity.Null &&
                    zoneDataExists &&
                    ambienceDataExists &&
                    ((zoneData.m_AreaType == AreaType.Residential && ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed) || zoneData.m_AreaType == AreaType.Commercial || zoneData.m_AreaType == AreaType.Industrial))
                {
                    isRICOWorkplace = true;
                }
                if (workers > 0 || isRICOWorkplace)
                {
                    workCsv.AppendLine($"{commonLine},{work.m_MaxWorkers},{(work.m_MaxWorkers / area):F2}");
                    countWork++;
                    if (isRICOWorkplace && workers <= 0)
                    {
                        Mod.log.Info($"[RWH Export] RICO Workplace Detected: {name} | ZoneType: {zoneData.m_AreaType} | Ambience: {ambienceData.m_AmbienceType} | Workers: {workers}");
                    }
                }
                if (workplaceDataLookup.TryGetComponent(entity, out var works))
                {
                    if (works.m_MaxWorkers > 0)
                    {
                        workCsv.AppendLine($"{commonLine},{works.m_MaxWorkers},{(works.m_MaxWorkers / area):F2}");
                        countWork++;
                    }
                }
            }

            Mod.log.Info($"[RWH Export] Summary: Res={countRes}, Work={countWork}, School={countSchool}, Hosp={countHosp}, Prison={countPrison}");

            // --- 5. Save ---
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

            return null;
        }
    }
}