
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
    public struct UpdatePowerPlantJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdatePowerPlantJobQuery()
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
                        ComponentType.ReadWrite<PowerPlantData>(),
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
    public struct UpdatePowerPlantJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<BuildingData> BuildingDataLookup;
        public ComponentTypeHandle<WorkplaceData> WorkplaceDataLookup;
        public ComponentTypeHandle<PowerPlantData> PowerPlantDataLookup;
        public BufferTypeHandle<SubMesh> subMeshHandle;
        public ComponentLookup<MeshData> meshDataLookup;
        public float sqm_per_employee_industry;
        public float industry_avg_floor_height;
        public const int POWER_PLANT_FLOOR_LIMIT = 2; //Use a floor limit for power plant since they tend to be very tall but it's mostly the chimmenies

        public UpdatePowerPlantJob()
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
            var PowerPlantDataArr = chunk.GetNativeArray(ref PowerPlantDataLookup);
            var subMeshBufferAccessor = chunk.GetBufferAccessor(ref subMeshHandle);
            
            for (int i = 0; i < workplaceDataArr.Length; i++)
            {
                Entity entity = entities[i];
                WorkplaceData workplaceData = workplaceDataArr[i];
                BuildingData buildingData = buildingDataArr[i];
                PowerPlantData PowerPlantData = PowerPlantDataArr[i];
                DynamicBuffer<SubMesh> subMeshes = subMeshBufferAccessor[i];

                var dimensions = BuildingUtils.GetBuildingDimensions(subMeshes, meshDataLookup);
                var size = ObjectUtils.GetSize(dimensions);
                float width = size.x;
                float length = size.z;
                float height = size.y;

                workplaceData.m_MaxWorkers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, sqm_per_employee_industry, false, POWER_PLANT_FLOOR_LIMIT);

                workplaceDataArr[i] = workplaceData;
                PowerPlantDataArr[i] = PowerPlantData;
            }
        }
    }
}