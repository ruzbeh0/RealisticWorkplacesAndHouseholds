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

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class PublicTransportStationUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdatePublicTransportStationJobQuery;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdatePublicTransportStationJobQuery UpdatePublicTransportStationJobQuery = new();
            m_UpdatePublicTransportStationJobQuery = GetEntityQuery(UpdatePublicTransportStationJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdatePublicTransportStationJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdatePublicTransportStation();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdatePublicTransportStation()
        {

            UpdatePublicTransportStationJob updatePublicTransportStationJob = new UpdatePublicTransportStationJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(false),
                PublicTransportStationDataLookup = SystemAPI.GetComponentTypeHandle<PublicTransportStationData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_transit = Mod.m_Setting.transit_station_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm
            };
            this.Dependency = updatePublicTransportStationJob.ScheduleParallel(m_UpdatePublicTransportStationJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
