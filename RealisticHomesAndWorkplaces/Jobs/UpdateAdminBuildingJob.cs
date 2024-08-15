
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

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateAdminBuildingJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateAdminBuildingJobQuery()
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
                        ComponentType.ReadWrite<AdminBuildingData>(),
                        ComponentType.ReadOnly<SubMesh>()

                    ],
                    Any = 
                    [
                        
                    ],
                    None =
                    [
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                }
            ];
        }
    }

    [BurstCompile]
    public struct UpdateAdminBuildingJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<BuildingData> BuildingDataLookup;
        public ComponentTypeHandle<WorkplaceData> WorkplaceDataLookup;
        public ComponentTypeHandle<AdminBuildingData> AdminBuildingDataLookup;
        public BufferTypeHandle<SubMesh> subMeshHandle;
        public ComponentLookup<MeshData> meshDataLookup;
        public float sqm_per_employee_office;
        public float commercial_avg_floor_height;

        public UpdateAdminBuildingJob()
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
            var AdminBuildingDataArr = chunk.GetNativeArray(ref AdminBuildingDataLookup);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);
            
            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                AdminBuildingData AdminBuildingData = AdminBuildingDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                //Using same attributes as offices for admin buildings
                workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, sqm_per_employee_office, sqm_per_employee_office, false);

                workplaceDataArr[i] = workplaceData;
                AdminBuildingDataArr[i] = AdminBuildingData;
            }
        }
    }
}