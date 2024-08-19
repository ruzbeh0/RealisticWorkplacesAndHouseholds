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
    public partial class CityServicesUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateCityServicesJobQuery;

        ModificationEndBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();

            // Job Queries
            UpdateCityServicesJobQuery UpdateCityServicesJobQuery = new();
            m_UpdateCityServicesJobQuery = GetEntityQuery(UpdateCityServicesJobQuery.Query);

            RequireAnyForUpdate(
                m_UpdateCityServicesJobQuery
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
            //UpdateCityServices();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            UpdateCityServices();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateCityServices()
        {

            UpdateCityServicesJob updateCityServicesJob = new UpdateCityServicesJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataLookup = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                WorkplaceDataLookup = SystemAPI.GetComponentTypeHandle<WorkplaceData>(false),
                SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(false),
                WelfareOfficeDataLookup = SystemAPI.GetComponentLookup<WelfareOfficeData>(true),
                FireStationDataLookup = SystemAPI.GetComponentLookup<FireStationData>(true),
                PrisonDataLookup = SystemAPI.GetComponentLookup<PrisonData>(false),
                ResearchFacilityDataLookup = SystemAPI.GetComponentLookup<ResearchFacilityData>(false),
                PostFacilityDataLookup = SystemAPI.GetComponentLookup<PostFacilityData>(false),
                PowerPlantDataLookup = SystemAPI.GetComponentLookup<PowerPlantData>(false),
                HospitalDataLookup = SystemAPI.GetComponentLookup<HospitalData>(false),
                PoliceStationDataLookup = SystemAPI.GetComponentLookup<PoliceStationData>(false),
                MaintenanceDepotDataLookup = SystemAPI.GetComponentLookup<MaintenanceDepotData>(false),
                AdminBuildingDataLookup = SystemAPI.GetComponentLookup<AdminBuildingData>(false),
                PublicTransportStationDataLookup = SystemAPI.GetComponentLookup<PublicTransportStationData>(false),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                subMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                studentPerTeacher = Mod.m_Setting.students_per_teacher,
                sqm_per_student = Mod.m_Setting.sqm_per_student,
                support_staff = Mod.m_Setting.support_staff / 100f,
                sqm_per_student_college_factor = Mod.m_Setting.sqm_college_adjuster,
                sqm_per_student_university_factor = Mod.m_Setting.sqm_univ_adjuster,
                sqm_per_employee_office = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm,
                sqm_per_employee_police = Mod.m_Setting.police_fire_sqm_per_worker,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                industry_sqm_per_employee = Mod.m_Setting.industry_sqm_per_worker,
                commercial_sqm_per_employee = Mod.m_Setting.commercial_sqm_per_worker,
                postoffice_sqm_per_employee = Mod.m_Setting.postoffice_sqm_per_worker,
                sqm_per_employee_hospital = Mod.m_Setting.hospital_sqm_per_worker,
                sqm_per_patient_hospital = Mod.m_Setting.hospital_sqm_per_patient,
                powerplant_sqm_per_employee = Mod.m_Setting.powerplant_sqm_per_worker,
                sqm_per_employee_transit = 1, //TODO
                non_usable_space_pct = Mod.m_Setting.office_non_usable_space/100f,
                global_reduction = Mod.m_Setting.results_reduction/100f
            };
            this.Dependency = updateCityServicesJob.ScheduleParallel(m_UpdateCityServicesJobQuery, this.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
