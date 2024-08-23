
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using RealisticWorkplacesAndHouseholds;
using Game.Companies;
using Game.Buildings;
using RealisticWorkplacesAndHouseholds.Components;
using UnityEngine;
using Game.Citizens;
using Game.Economy;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateCityServicesWorkplaceJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateCityServicesWorkplaceJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadWrite<WorkProvider>(),
                    ],
                    Any = [
                        ComponentType.ReadOnly<Game.Buildings.ElectricityProducer>(),
                        ComponentType.ReadWrite<Game.Buildings.School>(),
                        ComponentType.ReadWrite<Game.Buildings.Hospital>(),
                        ComponentType.ReadOnly<Game.Buildings.PoliceStation>(),
                        ComponentType.ReadOnly<Game.Buildings.Prison>(),
                        ComponentType.ReadOnly<Game.Buildings.FireStation>(),
                        ComponentType.ReadOnly<Game.Buildings.CargoTransportStation>(),
                        ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),
                        ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(),
                        ComponentType.ReadOnly<Game.Buildings.MaintenanceDepot>(),
                        ComponentType.ReadOnly<Game.Buildings.PostFacility>(),
                        ComponentType.ReadOnly<Game.Buildings.AdminBuilding>(),
                        ComponentType.ReadOnly<Game.Buildings.WelfareOffice>(),
                        ComponentType.ReadOnly<Game.Buildings.ResearchFacility>(),
                        ComponentType.ReadOnly<Game.Buildings.TransportStation>(),
                        ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(),
                    ],
                    None =
                    [

                    ],
                },
            ];
        }
    }

    [BurstCompile]
    public struct UpdateCityServicesWorkproviderJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        public ComponentLookup<ServiceCompanyData> ServiceCompanyDataLookup;
        public ComponentTypeHandle<WorkProvider> WorkProviderHandle;
        public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
        public ComponentLookup<WorkplaceData> WorkplaceDataLookup;
        [ReadOnly]
        public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> PrefabRefHandle;
        [ReadOnly]
        public BufferLookup<SubMesh> PrefabSubMeshesLookup;
        [ReadOnly]
        public ComponentLookup<MeshData> meshDataLookup;
        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.TransportDepot> TransportDepotLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.CargoTransportStation> CargoTransportStationLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.MaintenanceDepot> MaintenanceDepotLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.GarbageFacility> GarbageFacilityLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.ResearchFacility> ResearchFacilityLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.PostFacility> PostFacilityLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.FireStation> FireStationLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.PoliceStation> PoliceStationLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.WelfareOffice> WelfareOfficeLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.AdminBuilding> AdminBuildingLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.Hospital> HospitalLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.School> SchoolLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.Prison> PrisonLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.ElectricityProducer> ElectricityProducerLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.TransportStation> TransportStationLookup;
        [ReadOnly]
        public ComponentLookup<Game.Buildings.TelecomFacility> TelecomFacilityLookup;
        [ReadOnly]
        public ComponentLookup<SchoolData> SchoolDataLookup;
        [ReadOnly]
        public float studentPerTeacher;
        [ReadOnly]
        public float sqm_per_student;
        [ReadOnly]
        public float support_staff;
        [ReadOnly]
        public float sqm_per_student_university_factor;
        [ReadOnly]
        public float sqm_per_student_college_factor;
        [ReadOnly]
        public float sqm_per_employee_office;
        [ReadOnly]
        public float commercial_avg_floor_height;
        [ReadOnly]
        public int office_sqm_per_elevator;
        [ReadOnly]
        public float sqm_per_employee_police;
        [ReadOnly]
        public float sqm_per_employee_fire;
        [ReadOnly]
        public float sqm_per_patient_hospital;
        [ReadOnly]
        public float sqm_per_employee_hospital;
        [ReadOnly]
        public float industry_avg_floor_height;
        [ReadOnly]
        public float sqm_per_employee_transit;
        [ReadOnly]
        public float commercial_sqm_per_employee;
        [ReadOnly]
        public float industry_sqm_per_employee;
        [ReadOnly]
        public float postoffice_sqm_per_employee;
        [ReadOnly]
        public float powerplant_sqm_per_employee;
        [ReadOnly]
        public float non_usable_space_pct;
        [ReadOnly]
        public float global_reduction;
        [ReadOnly]
        public float prison_non_usable_area;
        [ReadOnly]
        public float prison_officers_prisoner_ratio;
        [ReadOnly]
        public float prison_sqm_per_prisoner;
        [ReadOnly]
        public float depot_sqm_per_worker;
        [ReadOnly]
        public float garbage_sqm_per_worker;
        [ReadOnly]
        public float transit_sqm_per_worker;
        [ReadOnly]
        public float airport_sqm_per_worker;

        public UpdateCityServicesWorkproviderJob()
        {

        }

        public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var workProviderArr = chunk.GetNativeArray(ref WorkProviderHandle);
            var prefabRefArr = chunk.GetNativeArray(ref PrefabRefHandle);

            for (int i = 0; i < workProviderArr.Length; i++)
            {
                var entity = entities[i];
                WorkProvider workProvider = workProviderArr[i];
                PrefabRef prefab1 = prefabRefArr[i];
                WorkplaceData workplaceData;

                if (WorkplaceDataLookup.TryGetComponent(prefab1, out workplaceData))
                {
                    if (PrefabSubMeshesLookup.TryGetBuffer(prefab1, out var subMeshes))
                    {
                        var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                        var size = ObjectUtils.GetSize(dimensions);
                        float width = size.x;
                        float length = size.z;
                        float height = size.y;

                        int original_workers = workProvider.m_MaxWorkers;
                        int workers = original_workers;

                        if (TransportDepotLookup.HasComponent(entity) || MaintenanceDepotLookup.HasComponent(entity) || CargoTransportStationLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.depotWorkers(width, length, height, industry_avg_floor_height, depot_sqm_per_worker);
                        }
                        if (GarbageFacilityLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.garbageWorkers(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker);
                        }
                        if (TelecomFacilityLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.telecomWorkers(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker, original_workers);
                        }
                        if (TransportStationLookup.HasComponent(entity))
                        {
                            if (TransportStationLookup.TryGetComponent(entity, out var transportStation))
                            {
                                if (transportStation.m_CarRefuelTypes != Game.Vehicles.EnergyTypes.None)
                                {
                                    workers = BuildingUtils.publicTransportationWorkers(width, length, height, industry_avg_floor_height, transit_sqm_per_worker, non_usable_space_pct, office_sqm_per_elevator, original_workers);
                                }
                                else
                                {
                                    workers = BuildingUtils.airportWorkers(width, length, height, industry_avg_floor_height, airport_sqm_per_worker, non_usable_space_pct, office_sqm_per_elevator, original_workers);
                                }
                            }
                        }
                        if (ElectricityProducerLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.powerPlantWorkers(width, length, height, industry_avg_floor_height, powerplant_sqm_per_employee);
                        }
                        if (HospitalLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.hospitalWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_hospital, office_sqm_per_elevator);
                        }
                        if (PrisonLookup.HasComponent(entity))
                        {
                            int new_capacity = BuildingUtils.prisonCapacity(width, length, height, commercial_avg_floor_height, prison_sqm_per_prisoner, prison_non_usable_area, office_sqm_per_elevator);
                            workers = BuildingUtils.prisonWorkers(new_capacity, prison_officers_prisoner_ratio);
                        }
                        if (WelfareOfficeLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.welfareOfficeWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, office_sqm_per_elevator, non_usable_space_pct);
                        }
                        if (AdminBuildingLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.adminBuildingsWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, office_sqm_per_elevator, non_usable_space_pct);
                        }
                        if (ResearchFacilityLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.researchFacilityWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, non_usable_space_pct, sqm_per_student_university_factor, office_sqm_per_elevator);
                        }
                        if (PostFacilityLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.postFacilitiesWorkers(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee, industry_sqm_per_employee, office_sqm_per_elevator);
                        }
                        else if (PoliceStationLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.policeStationWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_police);
                        }
                        else if (FireStationLookup.HasComponent(entity))
                        {
                            workers = BuildingUtils.fireStationWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_fire);  
                        }
                        else if (SchoolLookup.HasComponent(entity))
                        {
                            if (SchoolDataLookup.TryGetComponent(prefab1, out SchoolData schoolData))
                            {
                                int new_capacity = BuildingUtils.schoolCapacity(width, length, height, commercial_avg_floor_height, sqm_per_student, schoolData.m_EducationLevel, sqm_per_student_college_factor, sqm_per_student_university_factor);

                                workers = BuildingUtils.schoolWorkers(new_capacity, studentPerTeacher, support_staff);
                            }
                        }

                        workers = (int)(workers * (1f - global_reduction));

                        workProvider.m_MaxWorkers = workers;
                        workProviderArr[i] = workProvider;
                    }
                }
            }
        }
    }
}