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
    public partial class FireStationUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateFireStationJobQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Job Queries
            UpdateFireStationJobQuery UpdateFireStationJobQuery = new();
            m_UpdateFireStationJobQuery = GetEntityQuery(UpdateFireStationJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateFireStationJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            UpdateFireStation();
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

        private void UpdateFireStation()
        {

            UpdateFireStationJob updateFireStationJob = new UpdateFireStationJob
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(true),
                FireStationDataLookup = SystemAPI.GetComponentTypeHandle<FireStationData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_police = Mod.m_Setting.police_fire_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height
            };
            updateFireStationJob.ScheduleParallel(m_UpdateFireStationJobQuery, this.Dependency).Complete();
        }
    }
}
