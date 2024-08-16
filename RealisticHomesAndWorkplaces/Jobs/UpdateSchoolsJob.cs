
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
    public struct UpdateSchoolsJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateSchoolsJobQuery()
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
                        ComponentType.ReadWrite<SchoolData>(),
                        ComponentType.ReadOnly<SubMesh>()

                    ],
                    Any = 
                    [
                        
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
    public struct UpdateSchoolsJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly]
        public ComponentTypeHandle<BuildingData> BuildingDataLookup;
        public ComponentTypeHandle<WorkplaceData> WorkplaceDataLookup;
        public ComponentTypeHandle<SchoolData> SchoolDataLookup;
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
        public float commercial_avg_floor_height;

        public UpdateSchoolsJob()
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
            var schoolDataArr = chunk.GetNativeArray(ref SchoolDataLookup);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);
            
            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                SchoolData schoolData = schoolDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

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

                int new_capacity = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_student_t, false, 0);
                //Mod.log.Info($"Level:{level}, Previous Students:{schoolData.m_StudentCapacity} New:{new_capacity}");
                schoolData.m_StudentCapacity = new_capacity;

                float teacherAmount = schoolData.m_StudentCapacity / studentPerTeacher;

                //Support staff adjuster
                float supportStaffAdjuster = support_staff;
                //Mod.log.Info($"Previous Teachers:{workplaceData.m_MaxWorkers} New:{(int)(teacherAmount * (1f + supportStaffAdjuster))}");
                workplaceData.m_MaxWorkers = (int)(teacherAmount / (1f - supportStaffAdjuster));
                
                workplaceDataArr[i] = workplaceData;
                schoolDataArr[i] = schoolData;
                RealisticWorkplaceData realisticWorkplaceData = new();
                realisticWorkplaceData.max_workers = workplaceData.m_MaxWorkers;
                ecb.AddComponent(unfilteredChunkIndex, entity, realisticWorkplaceData);

            }
        }
    }
}