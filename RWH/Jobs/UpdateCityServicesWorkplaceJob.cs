﻿
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
using RealisticWorkplacesAndHouseholds.Components;
using Game.Areas;
using System;

//Obsolete - This job only works for new city services buildings. I created another one that works for all
namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateCityServicesJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateCityServicesJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabData>(),
                        ComponentType.ReadWrite<WorkplaceData>(),
                        ComponentType.ReadOnly<BuildingData>(),
                        //ComponentType.ReadOnly<ServiceObjectData>(),
                        ComponentType.ReadOnly<SubMesh>()

                    ],
                    Any =
                    [
                        ComponentType.ReadOnly<PowerPlantData>(),
                        ComponentType.ReadWrite<SchoolData>(),
                        ComponentType.ReadWrite<HospitalData>(),
                        ComponentType.ReadOnly<PoliceStationData>(),
                        ComponentType.ReadOnly<PrisonData>(),
                        ComponentType.ReadOnly<FireStationData>(),
                        ComponentType.ReadOnly<CargoTransportStationData>(),
                        ComponentType.ReadOnly<TransportDepotData>(),
                        ComponentType.ReadOnly<GarbageFacilityData>(),
                        //ComponentType.ReadOnly<DeathcareFacilityData>(),
                        ComponentType.ReadOnly<TransportStationData>(),
                        ComponentType.ReadOnly<MaintenanceDepotData>(),
                        ComponentType.ReadOnly<PostFacilityData>(),
                        ComponentType.ReadOnly<AdminBuildingData>(),
                        ComponentType.ReadOnly<WelfareOfficeData>(),
                        ComponentType.ReadOnly<ResearchFacilityData>(),
                        ComponentType.ReadOnly<TelecomFacilityData>(),
                        //ComponentType.ReadOnly<ParkData>(),
                    ],
                    None =
                    [
                        //ComponentType.Exclude<RealisticWorkplaceData>(),
                        //ComponentType.Exclude<Deleted>(),
                        //ComponentType.Exclude<Temp>()
                    ],
                }
            ];
        }
    }

    [BurstCompile]
    public struct UpdateCityServicesWorkplaceJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public ComponentTypeHandle<PrefabData> PrefabDataLookup;
        [ReadOnly]
        public ComponentTypeHandle<BuildingData> BuildingDataLookup;
        public ComponentTypeHandle<WorkplaceData> WorkplaceDataLookup;
        [ReadOnly]
        public ComponentLookup<WelfareOfficeData> WelfareOfficeDataLookup;
        public ComponentLookup<SchoolData> SchoolDataLookup;
        [ReadOnly]
        public ComponentLookup<AdminBuildingData> AdminBuildingDataLookup;
        public ComponentLookup<HospitalData> HospitalDataLookup;
        [ReadOnly]
        public ComponentLookup<PoliceStationData> PoliceStationDataLookup;
        [ReadOnly]
        public ComponentLookup<FireStationData> FireStationDataLookup;
        [ReadOnly]
        public ComponentLookup<PowerPlantData> PowerPlantDataLookup;
        [ReadOnly]
        public ComponentLookup<TransportStationData> TransportStationDataLookup;
        [ReadOnly]
        public ComponentLookup<ResearchFacilityData> ResearchFacilityDataLookup;
        [ReadOnly]
        public ComponentLookup<PostFacilityData> PostFacilityDataLookup;
        [ReadOnly]
        public ComponentLookup<PrisonData> PrisonDataLookup;
        [ReadOnly]
        public ComponentLookup<MaintenanceDepotData> MaintenanceDepotDataLookup;
        [ReadOnly]
        public ComponentLookup<TransportDepotData> TransportDepotDataLookup;
        [ReadOnly]
        public ComponentLookup<GarbageFacilityData> GarbageFacilityDataLookup;
        [ReadOnly]
        public ComponentLookup<CargoTransportStationData> CargoTransportStationDataLookup;
        [ReadOnly]
        public ComponentLookup<TelecomFacilityData> TelecomFacilityDataLookup;
        [ReadOnly]
        public BufferTypeHandle<SubMesh> subMeshHandle;
        [ReadOnly]
        public ComponentLookup<MeshData> meshDataLookup;
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
        [ReadOnly]
        public bool more_electricity;

        public UpdateCityServicesWorkplaceJob()
        {
        }

        public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
            ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
            var buildingDataArr = chunk.GetNativeArray(ref BuildingDataLookup);
            var workplaceDataArr = chunk.GetNativeArray(ref WorkplaceDataLookup);
            var prefabDataArr = chunk.GetNativeArray(ref PrefabDataLookup);

            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);
            
            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];
                PrefabData prefabData = prefabDataArr[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                int oldworkers = workplaceData.m_MaxWorkers;
                int workers = oldworkers;

                if (MaintenanceDepotDataLookup.HasComponent(entity) || TransportDepotDataLookup.HasComponent(entity) || CargoTransportStationDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.depotWorkers(width, length, height, industry_avg_floor_height, depot_sqm_per_worker);
                }
                if (GarbageFacilityDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.garbageWorkers(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker);
                }
                if (TelecomFacilityDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.telecomWorkers(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker, oldworkers);
                }
                if (TransportStationDataLookup.HasComponent(entity))
                {
                    if (TransportStationDataLookup.TryGetComponent(entity, out var transportStation))
                    {
                        if(transportStation.m_AircraftRefuelTypes != Game.Vehicles.EnergyTypes.None)
                        {
                            //Airports
                            workers = BuildingUtils.airportWorkers(width, length, height, industry_avg_floor_height, airport_sqm_per_worker, non_usable_space_pct, office_sqm_per_elevator, oldworkers);
                        } else
                        {
                            workers = BuildingUtils.publicTransportationWorkers(width, length, height, industry_avg_floor_height, transit_sqm_per_worker, non_usable_space_pct, office_sqm_per_elevator, oldworkers); 
                        }                       
                    } 
                }
                if (PowerPlantDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.powerPlantWorkers(width, length, height, industry_avg_floor_height, powerplant_sqm_per_employee);

                    if(more_electricity && PowerPlantDataLookup.TryGetComponent(entity, out var powerPlant))
                    {
                        float factor = 1f;
                        if(workers > 0 && oldworkers > 0)
                        {
                            factor = (int)(workers * (1f - global_reduction)) / oldworkers;
                        }
                        if(factor < 1f)
                        {
                            factor = 1f;
                        }
                        powerPlant.m_ElectricityProduction = (int)(powerPlant.m_ElectricityProduction*factor);
                        ecb.SetComponent(unfilteredChunkIndex, entity, powerPlant);
                    }
                }
                if (HospitalDataLookup.HasComponent(entity))
                {
                    if (HospitalDataLookup.TryGetComponent(entity, out HospitalData hospitalData))
                    {
                        int new_capacity = workers = BuildingUtils.hospitalWorkers(width, length, height, commercial_avg_floor_height, sqm_per_patient_hospital, office_sqm_per_elevator);
                        hospitalData.m_PatientCapacity = (int)(new_capacity * (1f - global_reduction));

                        workers = BuildingUtils.hospitalWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_hospital, office_sqm_per_elevator);
                        ecb.SetComponent(unfilteredChunkIndex, entity, hospitalData);
                    }        
                }
                if (PrisonDataLookup.HasComponent(entity))
                {
                    if (PrisonDataLookup.TryGetComponent(entity, out PrisonData prisonData))
                    {
                        int new_capacity = BuildingUtils.prisonCapacity(width, length, height, commercial_avg_floor_height, prison_sqm_per_prisoner, prison_non_usable_area, office_sqm_per_elevator);
                        prisonData.m_PrisonerCapacity = (int)(new_capacity * (1f - global_reduction));

                        workers = BuildingUtils.prisonWorkers(new_capacity, prison_officers_prisoner_ratio);

                        ecb.SetComponent(unfilteredChunkIndex, entity, prisonData);
                    }               
                }
                if (WelfareOfficeDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.welfareOfficeWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, office_sqm_per_elevator, non_usable_space_pct);
                }
                if (AdminBuildingDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.adminBuildingsWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, office_sqm_per_elevator, non_usable_space_pct);
                }
                if (ResearchFacilityDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.researchFacilityWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, non_usable_space_pct, sqm_per_student_university_factor, office_sqm_per_elevator);
                }
                if (PostFacilityDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.postFacilitiesWorkers(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee, industry_sqm_per_employee, office_sqm_per_elevator);
                }
                else if(PoliceStationDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.policeStationWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_police);
                }
                else if (FireStationDataLookup.HasComponent(entity))
                {
                    workers = BuildingUtils.fireStationWorkers(width, length, height, commercial_avg_floor_height, sqm_per_employee_fire);
                }
                else if (SchoolDataLookup.HasComponent(entity))
                {
                    if (SchoolDataLookup.TryGetComponent(entity, out SchoolData schoolData))
                    {
                        int new_capacity = BuildingUtils.schoolCapacity(width, length, height, commercial_avg_floor_height, sqm_per_student, schoolData.m_EducationLevel, sqm_per_student_college_factor, sqm_per_student_university_factor);
                        schoolData.m_StudentCapacity = (int)(new_capacity * (1f - global_reduction));
                        workers = BuildingUtils.schoolWorkers(new_capacity, studentPerTeacher, support_staff);

                        ecb.SetComponent(unfilteredChunkIndex, entity, schoolData);
                    }
                }

                //Apply global reduction factor
                workplaceData.m_MaxWorkers = (int)(workers * (1f - global_reduction));

                workplaceDataArr[i] = workplaceData;
            }
        }
    }
}