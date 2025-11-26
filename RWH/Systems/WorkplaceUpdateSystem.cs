using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class WorkplaceUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWorkplaceJobQuery;
        private EntityQuery m_ResetWorkplaceJobQuery;

        private bool m_TriggerInitialWorkplaceUpdate = false;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Query: excludes already updated entities
            UpdateWorkplaceJobQuery standardQuery = new();
            m_UpdateWorkplaceJobQuery = GetEntityQuery(standardQuery.Query);

            // Query: allows all entities (including already updated ones) for reset
            EntityQueryDesc resetQuery = new()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<CompanyData>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadWrite<WorkProvider>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_ResetWorkplaceJobQuery = GetEntityQuery(resetQuery);

            this.RequireForUpdate(m_UpdateWorkplaceJobQuery);
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_TriggerInitialWorkplaceUpdate = true;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            //Mod.log.Info($"OnUpdate, Reset:{m_TriggerInitialWorkplaceUpdate}");
            if (m_TriggerInitialWorkplaceUpdate)
            {
                UpdateWorkplace(true); // Reset mode
                m_TriggerInitialWorkplaceUpdate = false;
            }
            else
            {
                UpdateWorkplace(false); // Standard update
            }
        }

        private void UpdateWorkplace(bool reset)
        {
            var activeQuery = reset ? m_ResetWorkplaceJobQuery : m_UpdateWorkplaceJobQuery;

            if (reset)
            {
                activeQuery.ResetFilter(); // include all entities
            }
            else
            {
                activeQuery.SetChangedVersionFilter(typeof(WorkProvider));
            }

            var commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            UpdateWorkplaceJob updateZonableWorkplace = new UpdateWorkplaceJob
            {
                ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                PropertyRenterHandle = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                CompanyDataHandle = SystemAPI.GetComponentTypeHandle<CompanyData>(true),
                ServiceCompanyDataLookup = SystemAPI.GetComponentLookup<ServiceCompanyData>(false),
                IndustrialProcessDataLookup = SystemAPI.GetComponentLookup<IndustrialProcessData>(false),
                WorkplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(false),
                SpawnableBuildingDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                ZoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                WorkProviderHandle = SystemAPI.GetComponentTypeHandle<WorkProvider>(false),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PrefabSubMeshesLookup = SystemAPI.GetBufferLookup<SubMesh>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                BuildingPropertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(false),
                ExtractorCompanyDataLookup = SystemAPI.GetComponentLookup<ExtractorCompanyData>(true),
                RealisticWorkplaceDataLookup = SystemAPI.GetComponentLookup<RealisticWorkplaceData>(true),
                UffLookup = SystemAPI.GetComponentLookup<RealisticWorkplacesAndHouseholds.Components.UsableFootprintFactor>(true),
                reset = reset,
                commercial_sqm_per_employee = Mod.m_Setting.commercial_sqm_per_worker,
                office_sqm_per_employee = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                industry_sqm_per_employee = Mod.m_Setting.industry_sqm_per_worker,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm,
                commercial_self_service_gas = Mod.m_Setting.commercial_self_service_gas,
                non_usable_space_pct = Mod.m_Setting.office_non_usable_space / 100f,
                commercial_sqm_per_worker_restaurants = Mod.m_Setting.commercial_sqm_per_worker_restaurants,
                commercial_sqm_per_worker_supermarket = Mod.m_Setting.commercial_sqm_per_worker_supermarket,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
                industry_base_threshold = Mod.m_Setting.industry_area_base,
                office_height_threshold = Mod.m_Setting.office_height_base,
                commercial_sqm_per_worker_rec_entertainment = Mod.m_Setting.commercial_sqm_per_worker_rec_entertainment,
            };

            this.Dependency = updateZonableWorkplace.ScheduleParallel(activeQuery, this.Dependency);
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
