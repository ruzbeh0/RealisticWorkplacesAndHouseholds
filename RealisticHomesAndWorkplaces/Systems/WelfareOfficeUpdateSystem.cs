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
    public partial class WelfareOfficeUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateWelfareOfficeJobQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

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

            UpdateWelfareOffice();
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

        private void UpdateWelfareOffice()
        {

            UpdateWelfareOfficeJob updateWelfareOfficeJob = new UpdateWelfareOfficeJob
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(false),
                WelfareOfficeDataLookup = SystemAPI.GetComponentTypeHandle<WelfareOfficeData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_office = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height
            };
            updateWelfareOfficeJob.ScheduleParallel(m_UpdateWelfareOfficeJobQuery, this.Dependency).Complete();
        }
    }
}
