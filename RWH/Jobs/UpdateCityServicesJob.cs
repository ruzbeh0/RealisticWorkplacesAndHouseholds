
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
                        ComponentType.ReadWrite<WorkplaceData>(),
                        ComponentType.ReadOnly<BuildingData>(),
                        ComponentType.ReadOnly<ServiceObjectData>(),
                        ComponentType.ReadOnly<SubMesh>()

                    ],
                    Any =
                    [
                        ComponentType.ReadOnly<PowerPlantData>(),
                        ComponentType.ReadWrite<SchoolData>(),
                        ComponentType.ReadWrite<HospitalData>(),
                        ComponentType.ReadOnly<PoliceStationData>(),
                        //ComponentType.ReadOnly<PrisonData>(),
                        ComponentType.ReadOnly<FireStationData>(),
                        //ComponentType.ReadOnly<CargoTransportStationData>(),
                        //ComponentType.ReadOnly<TransportDepotData>(),
                        //ComponentType.ReadOnly<GarbageFacilityData>(),
                        //ComponentType.ReadOnly<DeathcareFacilityData>(),
                        //ComponentType.ReadOnly<PublicTransportStationData>(),
                        //ComponentType.ReadOnly<MaintenanceDepotData>(),
                        ComponentType.ReadOnly<PostFacilityData>(),
                        ComponentType.ReadOnly<AdminBuildingData>(),
                        ComponentType.ReadOnly<WelfareOfficeData>(),
                        //ComponentType.ReadOnly<ResearchFacilityData>(),
                        //ComponentType.ReadOnly<TelecomFacilityData>(),
                        //ComponentType.ReadOnly<ParkData>(),
                    ],
                    None =
                    [
                        ComponentType.Exclude<RealisticWorkplaceData>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                }
            ];
        }
    }

    [BurstCompile]
    public struct UpdateCityServicesJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

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
        public ComponentLookup<PublicTransportStationData> PublicTransportStationDataLookup;
        [ReadOnly]
        public ComponentLookup<ResearchFacilityData> ResearchFacilityDataLookup;
        [ReadOnly]
        public ComponentLookup<PostFacilityData> PostFacilityDataLookup;
        [ReadOnly]
        public ComponentLookup<PrisonData> PrisonDataLookup;
        [ReadOnly]
        public ComponentLookup<MaintenanceDepotData> MaintenanceDepotDataLookup;
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

        public const int POWER_PLANT_FLOOR_LIMIT = 2; //Use a floor limit for power plant since they tend to be very tall but it's mostly the chimmenies

        const float reduction_factor = 0.85f;

        public UpdateCityServicesJob()
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
            //var CityServicesDataArr = chunk.GetNativeArray(ref CityServicesDataLookup);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);

            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                if (PublicTransportStationDataLookup.HasComponent(entity))
                {
                    //Apply reduction factor because stations have a lot of empty space
                    workplaceData.m_MaxWorkers = math.max(1, (int)(reduction_factor * BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_transit, office_sqm_per_elevator)));
                }
                if (PowerPlantDataLookup.HasComponent(entity))
                {
                    //Apply floor limit because power plants have huge chimney 
                    workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, powerplant_sqm_per_employee, 0, 0, POWER_PLANT_FLOOR_LIMIT);

                }
                if (HospitalDataLookup.HasComponent(entity))
                {
                    if (HospitalDataLookup.TryGetComponent(entity, out HospitalData hospitalData))
                    {
                        int new_capacity = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_patient_hospital, office_sqm_per_elevator);
                        hospitalData.m_PatientCapacity = new_capacity;

                        workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_hospital, office_sqm_per_elevator);
                        //Mod.log.Info($"new_capacity: {new_capacity}, sqm_per_patient_hospital: {sqm_per_patient_hospital}, workplaceData.m_MaxWorkers {workplaceData.m_MaxWorkers}");

                        ecb.SetComponent(unfilteredChunkIndex, entity, hospitalData);
                    }
                        
                }
                //if (PrisonDataLookup.HasComponent(entity))
                //{
                //    if (PrisonDataLookup.TryGetComponent(entity, out PrisonData prisonData))
                //    {
                //        int new_capacity = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_patient_hospital, false, office_sqm_per_elevator);
                //        prisonData.m_PatientCapacity = new_capacity;
                //
                //        workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_hospital, false, office_sqm_per_elevator);
                //        //Mod.log.Info($"new_capacity: {new_capacity}, sqm_per_patient_hospital: {sqm_per_patient_hospital}, workplaceData.m_MaxWorkers {workplaceData.m_MaxWorkers}");
                //
                //        ecb.SetComponent(unfilteredChunkIndex, entity, hospitalData);
                //    }
                //
                //}
                if (AdminBuildingDataLookup.HasComponent(entity) || WelfareOfficeDataLookup.HasComponent(entity))
                {
                    //Using same attributes as offices for admin buildings
                    workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_office, office_sqm_per_elevator);
                }
                if (ResearchFacilityDataLookup.HasComponent(entity))
                {
                    //Using university factor because they also do research
                    workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_office * sqm_per_student_university_factor, office_sqm_per_elevator);
                }
                if (PostFacilityDataLookup.HasComponent(entity))
                {
                    workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee, office_sqm_per_elevator);
                    //TODO post sorting facility
                    if(workplaceData.m_MaxWorkers > 500)
                    {
                        continue;
                    }
                }
                else if(FireStationDataLookup.HasComponent(entity) || PoliceStationDataLookup.HasComponent(entity))
                {
                    //Skipping lobby because usually in fire stations the ground floor is the fire truck garage
                    workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_police, 1, 0);

                }
                else if (SchoolDataLookup.HasComponent(entity))
                {
                    if (SchoolDataLookup.TryGetComponent(entity, out SchoolData schoolData))
                    {
                        int level = schoolData.m_EducationLevel;
                        float sqm_per_student_t = sqm_per_student;
                        //Average number of students per teachers
                        //College
                        if (level == 3)
                        {
                            sqm_per_student_t *= sqm_per_student_college_factor;
                        }
                        else
                        {
                            //University
                            if (level == 4)
                            {
                                sqm_per_student_t *= sqm_per_student_university_factor;
                            }
                        }

                        int new_capacity = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_student_t, 0);
                        //Mod.log.Info($"Level:{level}, Previous Students:{schoolData.m_StudentCapacity} New:{new_capacity}");
                        schoolData.m_StudentCapacity = new_capacity;

                        float teacherAmount = schoolData.m_StudentCapacity / studentPerTeacher;

                        //Support staff adjuster
                        float supportStaffAdjuster = support_staff;
                        //Mod.log.Info($"Previous Teachers:{workplaceData.m_MaxWorkers} New:{(int)(teacherAmount * (1f + supportStaffAdjuster))}");
                        workplaceData.m_MaxWorkers = (int)(teacherAmount / (1f - supportStaffAdjuster));

                        ecb.SetComponent(unfilteredChunkIndex, entity, schoolData);
                    }
                }

                workplaceDataArr[i] = workplaceData;
                RealisticWorkplaceData realisticWorkplaceData = new();
                realisticWorkplaceData.max_workers = workplaceData.m_MaxWorkers;
                ecb.AddComponent(unfilteredChunkIndex, entity, realisticWorkplaceData);
            }
        }
    }
}