
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
        [ReadOnly]
        public float non_usable_space_pct;
        [ReadOnly]
        public float global_reduction;

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
            //Mod.log.Info($"Length: {workplaceDataArr.Length}");
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

                int oldworkers = workplaceData.m_MaxWorkers;
                int workers = oldworkers;

                if (PublicTransportStationDataLookup.HasComponent(entity))
                {
                    //Apply reduction factor because stations have a lot of empty space
                    workers = math.max(1, (int)(reduction_factor * BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_transit, office_sqm_per_elevator)));
                }
                if (PowerPlantDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"PP");
                    //Apply floor limit because power plants have huge chimney 
                    //Smooth the employees per sqm for bigger powerplants
                    float base_area = 120 * 120;
                    float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                    workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, powerplant_sqm_per_employee*area_factor, 0, 0, POWER_PLANT_FLOOR_LIMIT);

                }
                if (HospitalDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"H");
                    if (HospitalDataLookup.TryGetComponent(entity, out HospitalData hospitalData))
                    {
                        //Smooth the employees per sqm for bigger hospitals
                        float base_area = 50 * 50;
                        float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                        int new_capacity = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_patient_hospital*area_factor, office_sqm_per_elevator);
                        hospitalData.m_PatientCapacity = (int)(new_capacity * (1f - global_reduction));

                        workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area_factor*sqm_per_employee_hospital, office_sqm_per_elevator);
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
                if (WelfareOfficeDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"W");
                    //Using same attributes as offices for admin buildings
                    //Smooth the employees per sqm for bigger buildings
                    float base_area = 50 * 50;
                    float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                    float area = sqm_per_employee_office * (1 + non_usable_space_pct);
                    workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area* area_factor, office_sqm_per_elevator);
                }
                if (AdminBuildingDataLookup.HasComponent(entity))
                {
                    //Using double sqm per employee as office for admin buildings
                    //Smooth the employees per sqm for bigger buildings
                    float base_area = 50 * 50;
                    float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                    float area = 2*sqm_per_employee_office * (1 + non_usable_space_pct);
                    workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area * area_factor, office_sqm_per_elevator);
                }
                if (ResearchFacilityDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"R");
                    float area = sqm_per_employee_office * (1 + non_usable_space_pct);
                    //Using university factor because they also do research
                    workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area * sqm_per_student_university_factor, office_sqm_per_elevator);
                }
                if (PostFacilityDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"PF");
                    workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee, office_sqm_per_elevator);
                    //post sorting facility
                    if(workers > 500)
                    {
                        float base_area = 60 * 60;
                        float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                        //Removing one floor for trucks and mail storage
                        workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, industry_sqm_per_employee * area_factor, 1, 0);

                    }
                }
                else if(FireStationDataLookup.HasComponent(entity) || PoliceStationDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"FSPS");
                    //Skipping lobby because usually in fire stations the ground floor is the fire truck garage
                    workers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_police, 1, 0);

                }
                else if (SchoolDataLookup.HasComponent(entity))
                {
                    //Mod.log.Info($"S");
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
                        schoolData.m_StudentCapacity = (int)(new_capacity * (1f - global_reduction));

                        float teacherAmount = schoolData.m_StudentCapacity / studentPerTeacher;

                        //Support staff adjuster
                        float supportStaffAdjuster = support_staff;
                        //Mod.log.Info($"Previous Teachers:{workplaceData.m_MaxWorkers} New:{(int)(teacherAmount * (1f + supportStaffAdjuster))}");
                        workers = (int)(teacherAmount / (1f - supportStaffAdjuster));

                        ecb.SetComponent(unfilteredChunkIndex, entity, schoolData);
                    }
                }

                //Apply global reduction factor
                workplaceData.m_MaxWorkers = (int)(workers * (1f - global_reduction));

                workplaceDataArr[i] = workplaceData;
                RealisticWorkplaceData realisticWorkplaceData = new();
                realisticWorkplaceData.max_workers = workplaceData.m_MaxWorkers;
                ecb.AddComponent(unfilteredChunkIndex, entity, realisticWorkplaceData);
            }
        }
    }
}