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
    public partial class HospitalUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateHospitalJobQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Job Queries
            UpdateHospitalJobQuery UpdateHospitalJobQuery = new();
            m_UpdateHospitalJobQuery = GetEntityQuery(UpdateHospitalJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateHospitalJobQuery
            );
        }

        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            UpdateHospital();
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

        private void UpdateHospital()
        {

            UpdateHospitalJob updateHospitalJob = new UpdateHospitalJob
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(true),
                HospitalDataLookup = SystemAPI.GetComponentTypeHandle<HospitalData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                sqm_per_employee_hospital = Mod.m_Setting.hospital_sqm_per_worker,
                sqm_per_employee_patient = Mod.m_Setting.hospital_sqm_per_patient,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height
            };
            updateHospitalJob.ScheduleParallel(m_UpdateHospitalJobQuery, this.Dependency).Complete();
        }
    }
}
