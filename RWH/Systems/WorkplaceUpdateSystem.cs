using Game;
using Unity.Entities;
using UnityEngine.Scripting;
using Unity.Jobs;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using RealisticWorkplacesAndHouseholds.Jobs;
using Game.Buildings;
using Game.Companies;
using RealisticWorkplacesAndHouseholds.Components;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class WorkplaceUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWorkplaceJobQuery1;
        private EntityQuery m_UpdateWorkplaceJobQuery2;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateWorkplaceJobQuery updateWorkplaceJobQuery = new();
            m_UpdateWorkplaceJobQuery1 = GetEntityQuery(updateWorkplaceJobQuery.Query);
            //RemoveRealisticWorkplaceJobQuery removeWorkplaceJobQuery = new();
            //m_UpdateWorkplaceJobQuery2 = GetEntityQuery(removeWorkplaceJobQuery.Query);
            //
            //this.RequireAnyForUpdate([m_UpdateWorkplaceJobQuery1, m_UpdateWorkplaceJobQuery2]);
            this.RequireAnyForUpdate(m_UpdateWorkplaceJobQuery1);

        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            UpdateWorkplace(true);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if(!m_UpdateWorkplaceJobQuery1.IsEmptyIgnoreFilter)
            {
                UpdateWorkplace(false);
            } 
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateWorkplace(bool reset)
        {
            UpdateWorkplaceJob updateZonableWorkplace = new UpdateWorkplaceJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
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
                reset = reset,
                commercial_sqm_per_employee = Mod.m_Setting.commercial_sqm_per_worker,
                office_sqm_per_employee = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                industry_sqm_per_employee = Mod.m_Setting.industry_sqm_per_worker,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm,
                commercial_self_service_gas = Mod.m_Setting.commercial_self_service_gas,
                non_usable_space_pct = Mod.m_Setting.office_non_usable_space/100f,
                commercial_sqm_per_worker_restaurants = Mod.m_Setting.commercial_sqm_per_worker_restaurants,
                commercial_sqm_per_worker_supermarket = Mod.m_Setting.commercial_sqm_per_worker_supermarket,
                global_reduction = Mod.m_Setting.results_reduction/100f
            };
            this.Dependency = updateZonableWorkplace.ScheduleParallel(m_UpdateWorkplaceJobQuery1, this.Dependency);
            
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
