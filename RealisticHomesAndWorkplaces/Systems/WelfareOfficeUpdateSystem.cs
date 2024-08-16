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
    public partial class WelfareOfficeUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWelfareOfficeJobQuery;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateWelfareOfficeJobQuery UpdateWelfareOfficeJobQuery = new();
            m_UpdateWelfareOfficeJobQuery = GetEntityQuery(UpdateWelfareOfficeJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateWelfareOfficeJobQuery
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
            UpdateWelfareOffice();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateWelfareOffice()
        {

            UpdateWelfareOfficeJob updateWelfareOfficeJob = new UpdateWelfareOfficeJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(false),
                WelfareOfficeDataLookup = SystemAPI.GetComponentTypeHandle<WelfareOfficeData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_office = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm
            };
            this.Dependency = updateWelfareOfficeJob.ScheduleParallel(m_UpdateWelfareOfficeJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
