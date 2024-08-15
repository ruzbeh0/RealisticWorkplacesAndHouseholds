using Game;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.SceneFlow;
using Unity.Jobs;
using Game.Common;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Mathematics;
using Unity.Burst;
using Game.Settings;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Jobs;
using Game.Buildings;
using Game.Companies;
using Unity.Collections;
using System.Runtime.InteropServices;
using Game.Areas;
using Game.Citizens;
using Game.Objects;
using Game.Simulation;
using Game.Triggers;
using static Colossal.IO.AssetDatabase.AssetDatabase;

namespace RealisticWorkplacesAndHouseholds
{
    [BurstCompile]
    public partial class UpdateWorkplaceSystem : GameSystemBase
    {
        private EntityQuery m_UpdateCommercialWorkplaceJobQuery;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateCommercialWorkplaceJobQuery updateCommercialWorkplaceJobQuery = new();
            m_UpdateCommercialWorkplaceJobQuery = GetEntityQuery(updateCommercialWorkplaceJobQuery.Query);

            this.RequireAnyForUpdate(m_UpdateCommercialWorkplaceJobQuery);

        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdateCommercialWorkplace();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateCommercialWorkplace()
        {
            UpdateWorkplaceJob updateZonableWorkplace = new UpdateWorkplaceJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PropertyRenterHandle = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                CompanyDataHandle = SystemAPI.GetComponentTypeHandle<CompanyData>(true),
                SpawnableBuildingDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                ZoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                WorkProviderHandle = SystemAPI.GetComponentTypeHandle<WorkProvider>(false),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PrefabSubMeshesLookup = SystemAPI.GetBufferLookup<SubMesh>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                BuildingPropertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(false),
                commercial_sqm_per_employee = Mod.m_Setting.commercial_sqm_per_worker,
                office_sqm_per_employee = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                industry_sqm_per_employee = Mod.m_Setting.industry_sqm_per_worker
            };
            this.Dependency = updateZonableWorkplace.ScheduleParallel(m_UpdateCommercialWorkplaceJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
