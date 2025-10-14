using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class CityServicesWorkproviderUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateCityServicesWorkplaceJobQuery1;

        EndFrameBarrier m_EndFrameBarrier;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // Job Queries
            UpdateCityServicesWorkplaceJobQuery UpdateCityServicesWorkplaceJobQuery = new();
            m_UpdateCityServicesWorkplaceJobQuery1 = GetEntityQuery(UpdateCityServicesWorkplaceJobQuery.Query);

            this.RequireAnyForUpdate(m_UpdateCityServicesWorkplaceJobQuery1);

        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            //Mod.log.Info("City Services Work Provider calculations loaded");
            UpdateCityServicesWorkplace();
        }

        [Preserve]
        protected override void OnUpdate()
        {

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        private void UpdateCityServicesWorkplace()
        {
            UpdateCityServicesWorkproviderJob updateCityServicesJob = new UpdateCityServicesWorkproviderJob
            {
                ecb = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                ServiceCompanyDataLookup = SystemAPI.GetComponentLookup<ServiceCompanyData>(false),
                IndustrialProcessDataLookup = SystemAPI.GetComponentLookup<IndustrialProcessData>(false),
                WorkplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(false),
                SpawnableBuildingDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                WorkProviderHandle = SystemAPI.GetComponentTypeHandle<WorkProvider>(false),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                PrefabSubMeshesLookup = SystemAPI.GetBufferLookup<SubMesh>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                TransportDepotLookup = SystemAPI.GetComponentLookup<Game.Buildings.TransportDepot>(true),
                CargoTransportStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.CargoTransportStation>(true),
                MaintenanceDepotLookup = SystemAPI.GetComponentLookup<Game.Buildings.MaintenanceDepot>(true),
                GarbageFacilityLookup = SystemAPI.GetComponentLookup<Game.Buildings.GarbageFacility>(true),
                AdminBuildingLookup = SystemAPI.GetComponentLookup<Game.Buildings.AdminBuilding>(true),
                FireStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.FireStation>(true),
                PoliceStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.PoliceStation>(true),
                PostFacilityLookup = SystemAPI.GetComponentLookup<Game.Buildings.PostFacility>(true),
                ResearchFacilityLookup = SystemAPI.GetComponentLookup<Game.Buildings.ResearchFacility>(true),
                WelfareOfficeLookup = SystemAPI.GetComponentLookup<Game.Buildings.WelfareOffice>(true),
                HospitalLookup = SystemAPI.GetComponentLookup<Game.Buildings.Hospital>(true),
                PrisonLookup = SystemAPI.GetComponentLookup<Game.Buildings.Prison>(true),
                ElectricityProducerLookup = SystemAPI.GetComponentLookup<Game.Buildings.ElectricityProducer>(true),
                TransportStationLookup = SystemAPI.GetComponentLookup<Game.Buildings.TransportStation>(true),   
                SchoolLookup = SystemAPI.GetComponentLookup<Game.Buildings.School>(true),
                TelecomFacilityLookup = SystemAPI.GetComponentLookup<Game.Buildings.TelecomFacility>(true), 
                SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(true),
                ParkLookup = SystemAPI.GetComponentLookup<Game.Buildings.Park>(true),
                UffLookup = SystemAPI.GetComponentLookup<UsableFootprintFactor>(true),
                park_sqm_per_worker = Mod.m_Setting.park_sqm_per_worker,
                studentPerTeacher = Mod.m_Setting.students_per_teacher,
                sqm_per_student = Mod.m_Setting.sqm_per_student,
                support_staff = Mod.m_Setting.support_staff / 100f,
                sqm_per_student_college_factor = Mod.m_Setting.sqm_college_adjuster,
                sqm_per_student_university_factor = Mod.m_Setting.sqm_univ_adjuster,
                sqm_per_employee_office = Mod.m_Setting.office_sqm_per_worker,
                commercial_avg_floor_height = Mod.m_Setting.commercial_avg_floor_height,
                office_sqm_per_elevator = Mod.m_Setting.office_elevators_per_sqm,
                sqm_per_employee_police = Mod.m_Setting.police_sqm_per_worker,
                industry_avg_floor_height = Mod.m_Setting.industry_avg_floor_height,
                industry_sqm_per_employee = Mod.m_Setting.industry_sqm_per_worker,
                commercial_sqm_per_employee = Mod.m_Setting.commercial_sqm_per_worker,
                postoffice_sqm_per_employee = Mod.m_Setting.postoffice_sqm_per_worker,
                sqm_per_employee_hospital = Mod.m_Setting.hospital_sqm_per_worker,
                sqm_per_patient_hospital = Mod.m_Setting.hospital_sqm_per_patient,
                powerplant_sqm_per_employee = Mod.m_Setting.powerplant_sqm_per_worker,
                sqm_per_employee_clinic = Mod.m_Setting.clinic_sqm_per_worker,
                sqm_per_patient_clinic = Mod.m_Setting.clinic_sqm_per_patient,
                sqm_per_employee_transit = 1, //TODO
                non_usable_space_pct = Mod.m_Setting.office_non_usable_space / 100f,
                prison_non_usable_area = Mod.m_Setting.prison_non_usable_space,
                prison_officers_prisoner_ratio = Mod.m_Setting.prisoners_per_officer,
                prison_sqm_per_prisoner = Mod.m_Setting.prison_sqm_per_prisoner,
                sqm_per_employee_fire = Mod.m_Setting.fire_sqm_per_worker,
                depot_sqm_per_worker = Mod.m_Setting.depot_sqm_per_worker,
                garbage_sqm_per_worker = Mod.m_Setting.garbage_sqm_per_worker,
                transit_sqm_per_worker = Mod.m_Setting.transit_station_sqm_per_worker,
                airport_sqm_per_worker = Mod.m_Setting.airport_sqm_per_worker,
                admin_sqm_per_worker = Mod.m_Setting.admin_sqm_per_worker,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
                disable_powerplant = Mod.m_Setting.disable_powerplant,
                disable_school = Mod.m_Setting.disable_school,
                disable_police = Mod.m_Setting.disable_police,
                disable_fire = Mod.m_Setting.disable_fire,
                disable_postoffice = Mod.m_Setting.disable_postoffice,
                disable_garbage = Mod.m_Setting.disable_garbage,
                disable_depot = Mod.m_Setting.disable_garbage,
                disable_hospital = Mod.m_Setting.disable_hospital,
                disable_park = Mod.m_Setting.disable_park,
                disable_transport = Mod.m_Setting.disable_transit,
                disable_airport = Mod.m_Setting.disable_airport,
                disable_admin = Mod.m_Setting.disable_admin
            };
            this.Dependency = updateCityServicesJob.ScheduleParallel(m_UpdateCityServicesWorkplaceJobQuery1, this.Dependency);
            
            m_EndFrameBarrier.AddJobHandleForProducer(this.Dependency);
        }
    }
}
