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
    public partial class PowerPlantUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdatePowerPlantJobQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Job Queries
            UpdatePowerPlantJobQuery UpdatePowerPlantJobQuery = new();
            m_UpdatePowerPlantJobQuery = GetEntityQuery(UpdatePowerPlantJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdatePowerPlantJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            UpdatePowerPlant();
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

        private void UpdatePowerPlant()
        {

            UpdatePowerPlantJob updatePowerPlantJob = new UpdatePowerPlantJob
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(true),
                PowerPlantDataLookup = SystemAPI.GetComponentTypeHandle<PowerPlantData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_industry = Mod.m_Setting.industry_sqm_per_worker,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height
            };
            updatePowerPlantJob.ScheduleParallel(m_UpdatePowerPlantJobQuery, this.Dependency).Complete();
        }
    }
}
