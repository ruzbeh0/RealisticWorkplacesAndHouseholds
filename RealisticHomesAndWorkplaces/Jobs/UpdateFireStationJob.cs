
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
    public struct UpdateFireStationJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateFireStationJobQuery()
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
                        ComponentType.ReadWrite<FireStationData>(),
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
    public struct UpdateFireStationJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<BuildingData> BuildingDataLookup;
        public ComponentTypeHandle<WorkplaceData> WorkplaceDataLookup;
        public ComponentTypeHandle<FireStationData> FireStationDataLookup;
        public BufferTypeHandle<SubMesh> subMeshHandle;
        public ComponentLookup<MeshData> meshDataLookup;
        public float sqm_per_employee_police;
        public float commercial_avg_floor_height;
        public UpdateFireStationJob()
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
            var FireStationDataArr = chunk.GetNativeArray(ref FireStationDataLookup);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);
            
            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                FireStationData FireStationData = FireStationDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                //Skipping lobby because usually in fire stations the ground floor is the fire truck garage
                workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_police, true);

                workplaceDataArr[i] = workplaceData;
                FireStationDataArr[i] = FireStationData;
            }
        }
    }
}