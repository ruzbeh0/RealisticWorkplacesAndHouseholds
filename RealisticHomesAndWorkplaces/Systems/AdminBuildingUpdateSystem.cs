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

namespace RealisticWorkplacesAndHouseholds
{
    [BurstCompile]
    public partial class AdminBuildingUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateAdminBuildingJobQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Job Queries
            UpdateAdminBuildingJobQuery UpdateAdminBuildingJobQuery = new();
            m_UpdateAdminBuildingJobQuery = GetEntityQuery(UpdateAdminBuildingJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateAdminBuildingJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            UpdateAdminBuilding();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        [Preserve]
        protected override void OnUpdate()
        {

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateAdminBuilding()
        {

            UpdateAdminBuildingJob updateAdminBuildingJob = new UpdateAdminBuildingJob
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(true),
                AdminBuildingDataLookup = SystemAPI.GetComponentTypeHandle<AdminBuildingData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_office = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height
            };
            updateAdminBuildingJob.ScheduleParallel(m_UpdateAdminBuildingJobQuery, this.Dependency).Complete();
        }
    }
}
