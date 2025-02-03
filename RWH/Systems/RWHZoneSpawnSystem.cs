
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Game.Simulation;
using Game;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds;

#nullable disable
namespace RWH.Systems
{
    //[CompilerGenerated]
    public partial class RWHZoneSpawnSystem : GameSystemBase
    {
        private ZoneSystem m_ZoneSystem;
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private GroundPollutionSystem m_PollutionSystem;
        private TerrainSystem m_TerrainSystem;
        private Game.Zones.SearchSystem m_SearchSystem;
        private ResourceSystem m_ResourceSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private EntityQuery m_LotQuery;
        private EntityQuery m_BuildingQuery;
        private EntityQuery m_ProcessQuery;
        private EntityQuery m_BuildingConfigurationQuery;
        private EntityArchetype m_DefinitionArchetype;
        private RWHZoneSpawnSystem.TypeHandle __TypeHandle;
        private EntityQuery __query_1944910156_0;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public override int GetUpdateOffset(SystemUpdatePhase phase) => 13;

        public bool debugFastSpawn { get; set; }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ZoneSystem = this.World.GetOrCreateSystemManaged<ZoneSystem>();
            this.m_ResidentialDemandSystem = this.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            this.m_CommercialDemandSystem = this.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            this.m_IndustrialDemandSystem = this.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            this.m_PollutionSystem = this.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_TerrainSystem = this.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_SearchSystem = this.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_LotQuery = this.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[4]
              {
          ComponentType.ReadOnly<Game.Zones.Block>(),
          ComponentType.ReadOnly<Owner>(),
          ComponentType.ReadOnly<CurvePosition>(),
          ComponentType.ReadOnly<VacantLot>()
              },
                Any = new ComponentType[0],
                None = new ComponentType[2]
              {
          ComponentType.ReadWrite<Temp>(),
          ComponentType.ReadWrite<Deleted>()
              }
            });
            this.m_BuildingQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Prefabs.BuildingData>(), ComponentType.ReadOnly<SpawnableBuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
            this.m_DefinitionArchetype = this.EntityManager.CreateArchetype(ComponentType.ReadWrite<CreationDefinition>(), ComponentType.ReadWrite<ObjectDefinition>(), ComponentType.ReadWrite<Updated>(), ComponentType.ReadWrite<Deleted>());
            this.m_ProcessQuery = this.GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
            this.m_BuildingConfigurationQuery = this.GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
            this.RequireForUpdate(this.m_LotQuery);
            this.RequireForUpdate(this.m_BuildingQuery);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            RandomSeed.Next().GetRandom(0);
            bool flag1 = this.debugFastSpawn || (this.m_ResidentialDemandSystem.buildingDemand.x + this.m_ResidentialDemandSystem.buildingDemand.y + this.m_ResidentialDemandSystem.buildingDemand.z) / 3 > 0;
            bool flag2 = this.debugFastSpawn || this.m_CommercialDemandSystem.buildingDemand > 0;
            bool flag3 = this.debugFastSpawn || (this.m_IndustrialDemandSystem.industrialBuildingDemand + this.m_IndustrialDemandSystem.officeBuildingDemand) / 2 > 0;
            bool flag4 = this.debugFastSpawn || this.m_IndustrialDemandSystem.storageBuildingDemand > 0;
            NativeQueue<RWHZoneSpawnSystem.SpawnLocation> nativeQueue1 = new NativeQueue<RWHZoneSpawnSystem.SpawnLocation>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<RWHZoneSpawnSystem.SpawnLocation> nativeQueue2 = new NativeQueue<RWHZoneSpawnSystem.SpawnLocation>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeQueue<RWHZoneSpawnSystem.SpawnLocation> nativeQueue3 = new NativeQueue<RWHZoneSpawnSystem.SpawnLocation>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_CurvePosition_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);
            JobHandle outJobHandle1;
            JobHandle deps1;
            JobHandle deps2;
            JobHandle deps3;
            JobHandle outJobHandle2;
            JobHandle dependencies1;

            RWHZoneSpawnSystem.EvaluateSpawnAreas jobData1 = new RWHZoneSpawnSystem.EvaluateSpawnAreas()
            {
                m_BuildingChunks = this.m_BuildingQuery.ToArchetypeChunkListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle1),
                m_ZonePrefabs = this.m_ZoneSystem.GetPrefabs(),
                m_Preferences = this.__query_1944910156_0.GetSingleton<ZonePreferenceData>(),
                m_SpawnResidential = flag1 ? 1 : 0,
                m_SpawnCommercial = flag2 ? 1 : 0,
                m_SpawnIndustrial = flag3 ? 1 : 0,
                m_SpawnStorage = flag4 ? 1 : 0,
                m_MinDemand = this.debugFastSpawn ? 0 : 1,
                m_ResidentialDemands = this.m_ResidentialDemandSystem.buildingDemand,
                m_CommercialBuildingDemands = this.m_CommercialDemandSystem.GetBuildingDemands(out deps1),
                m_IndustrialDemands = this.m_IndustrialDemandSystem.GetBuildingDemands(out deps2),
                m_StorageDemands = this.m_IndustrialDemandSystem.GetStorageBuildingDemands(out deps3),
                m_RandomSeed = RandomSeed.Next(),
                m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_BlockType = this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle,
                m_OwnerType = this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle,
                m_CurvePositionType = this.__TypeHandle.__Game_Zones_CurvePosition_RO_ComponentTypeHandle,
                m_VacantLotType = this.__TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle,
                m_BuildingDataType = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle,
                m_SpawnableBuildingType = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle,
                m_BuildingPropertyType = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle,
                m_ObjectGeometryType = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle,
                m_BuildingSpawnGroupType = this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle,
                m_WarehouseType = this.__TypeHandle.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle,
                m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup,
                m_BuildingPropertyExtraData = this.__TypeHandle.__Game_Prefabs_BuildingPropertyExtraData_RO_ComponentLookup,
                m_Availabilities = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup,
                m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup,
                m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup,
                m_Processes = this.m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle2),
                m_ProcessEstimates = this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup,
                m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs(),
                m_PollutionMap = this.m_PollutionSystem.GetMap(true, out dependencies1),
                m_Residential = nativeQueue1.AsParallelWriter(),
                m_Commercial = nativeQueue2.AsParallelWriter(),
                m_Industrial = nativeQueue3.AsParallelWriter()
            };
            this.__TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle dependencies2;

            RWHZoneSpawnSystem.SpawnBuildingJob jobData2 = new RWHZoneSpawnSystem.SpawnBuildingJob()
            {
                m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup,
                m_ValidAreaData = this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup,
                m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup,
                m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_PrefabBuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_PrefabPlaceableObjectData = this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup,
                m_PrefabSpawnableObjectData = this.__TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup,
                m_PrefabObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup,
                m_PrefabAreaGeometryData = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup,
                m_PrefabNetGeometryData = this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup,
                m_Cells = this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup,
                m_PrefabSubAreas = this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup,
                m_PrefabSubAreaNodes = this.__TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup,
                m_PrefabSubNets = this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup,
                m_PrefabPlaceholderElements = this.__TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup,
                m_DefinitionArchetype = this.m_DefinitionArchetype,
                m_RandomSeed = RandomSeed.Next(),
                m_LefthandTraffic = this.m_CityConfigurationSystem.leftHandTraffic,
                m_TerrainHeightData = this.m_TerrainSystem.GetHeightData(),
                m_ZoneSearchTree = this.m_SearchSystem.GetSearchTree(true, out dependencies2),
                m_BuildingConfigurationData = this.m_BuildingConfigurationQuery.GetSingleton<BuildingConfigurationData>(),
                m_Residential = nativeQueue1,
                m_Commercial = nativeQueue2,
                m_Industrial = nativeQueue3,
                m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
            };

            JobHandle jobHandle1 = jobData1.ScheduleParallel<RWHZoneSpawnSystem.EvaluateSpawnAreas>(this.m_LotQuery, JobUtils.CombineDependencies(outJobHandle1, deps1, deps2, deps3, dependencies1, this.Dependency, outJobHandle2));
            JobHandle dependsOn = JobHandle.CombineDependencies(jobHandle1, dependencies2);
            JobHandle jobHandle2 = jobData2.Schedule<RWHZoneSpawnSystem.SpawnBuildingJob>(3, 1, dependsOn);
            this.m_ResourceSystem.AddPrefabsReader(jobHandle1);
            this.m_PollutionSystem.AddReader(jobHandle1);
            this.m_CommercialDemandSystem.AddReader(jobHandle1);
            this.m_IndustrialDemandSystem.AddReader(jobHandle1);
            nativeQueue1.Dispose(jobHandle2);
            nativeQueue2.Dispose(jobHandle2);
            nativeQueue3.Dispose(jobHandle2);
            this.m_ZoneSystem.AddPrefabsReader(jobHandle1);
            this.m_TerrainSystem.AddCPUHeightReader(jobHandle2);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
            this.m_SearchSystem.AddSearchTreeReader(jobHandle2);
            this.Dependency = jobHandle2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            this.__query_1944910156_0 = state.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1]
              {
          ComponentType.ReadOnly<ZonePreferenceData>()
              },
                Any = new ComponentType[0],
                None = new ComponentType[0],
                Disabled = new ComponentType[0],
                Absent = new ComponentType[0],
                Options = EntityQueryOptions.IncludeSystems
            });
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public RWHZoneSpawnSystem()
        {
        }

        public struct SpawnLocation
        {
            public Entity m_Entity;
            public Entity m_Building;
            public int4 m_LotArea;
            public float m_Priority;
            public ZoneType m_ZoneType;
            public Game.Zones.AreaType m_AreaType;
            public LotFlags m_LotFlags;
        }

        [BurstCompile]
        public struct EvaluateSpawnAreas : IJobChunk
        {
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_BuildingChunks;
            [ReadOnly]
            public ZonePrefabs m_ZonePrefabs;
            [ReadOnly]
            public ZonePreferenceData m_Preferences;
            [ReadOnly]
            public int m_SpawnResidential;
            [ReadOnly]
            public int m_SpawnCommercial;
            [ReadOnly]
            public int m_SpawnIndustrial;
            [ReadOnly]
            public int m_SpawnStorage;
            [ReadOnly]
            public int m_MinDemand;
            public int3 m_ResidentialDemands;
            [ReadOnly]
            public NativeArray<int> m_CommercialBuildingDemands;
            [ReadOnly]
            public NativeArray<int> m_IndustrialDemands;
            [ReadOnly]
            public NativeArray<int> m_StorageDemands;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Zones.Block> m_BlockType;
            [ReadOnly]
            public ComponentTypeHandle<Owner> m_OwnerType;
            [ReadOnly]
            public ComponentTypeHandle<CurvePosition> m_CurvePositionType;
            [ReadOnly]
            public BufferTypeHandle<VacantLot> m_VacantLotType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Prefabs.BuildingData> m_BuildingDataType;
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;
            [ReadOnly]
            public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;
            [ReadOnly]
            public ComponentTypeHandle<WarehouseData> m_WarehouseType;
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyExtraData> m_BuildingPropertyExtraData;
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;
            [ReadOnly]
            public NativeList<IndustrialProcessData> m_Processes;
            [ReadOnly]
            public BufferLookup<ProcessEstimate> m_ProcessEstimates;
            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValues;
            [ReadOnly]
            public ComponentLookup<Game.Zones.Block> m_BlockData;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation>.ParallelWriter m_Residential;
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation>.ParallelWriter m_Commercial;
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation>.ParallelWriter m_Industrial;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
                RWHZoneSpawnSystem.SpawnLocation bestLocation1 = new RWHZoneSpawnSystem.SpawnLocation();
                RWHZoneSpawnSystem.SpawnLocation bestLocation2 = new RWHZoneSpawnSystem.SpawnLocation();
                RWHZoneSpawnSystem.SpawnLocation bestLocation3 = new RWHZoneSpawnSystem.SpawnLocation();
                NativeArray<Entity> nativeArray1 = chunk.GetNativeArray(this.m_EntityType);
                BufferAccessor<VacantLot> bufferAccessor = chunk.GetBufferAccessor<VacantLot>(ref this.m_VacantLotType);
                if (bufferAccessor.Length != 0)
                {
                    NativeArray<Owner> nativeArray2 = chunk.GetNativeArray<Owner>(ref this.m_OwnerType);
                    NativeArray<CurvePosition> nativeArray3 = chunk.GetNativeArray<CurvePosition>(ref this.m_CurvePositionType);
                    NativeArray<Game.Zones.Block> nativeArray4 = chunk.GetNativeArray<Game.Zones.Block>(ref this.m_BlockType);
                    for (int index1 = 0; index1 < nativeArray1.Length; ++index1)
                    {
                        Entity entity = nativeArray1[index1];
                        DynamicBuffer<VacantLot> dynamicBuffer = bufferAccessor[index1];
                        Owner owner = nativeArray2[index1];
                        CurvePosition curvePosition = nativeArray3[index1];
                        Game.Zones.Block block = nativeArray4[index1];
                        for (int index2 = 0; index2 < dynamicBuffer.Length; ++index2)
                        {
                            VacantLot lot = dynamicBuffer[index2];
                            ZoneData zoneData = this.m_ZoneData[this.m_ZonePrefabs[lot.m_Type]];
                            DynamicBuffer<ProcessEstimate> processEstimate = this.m_ProcessEstimates[this.m_ZonePrefabs[lot.m_Type]];
                            switch (zoneData.m_AreaType)
                            {
                                case Game.Zones.AreaType.Residential:
                                    if (this.m_SpawnResidential != 0)
                                    {
                                        float curvePos = this.CalculateCurvePos(curvePosition, lot, block);
                                        this.TryAddLot(ref bestLocation1, ref random, owner.m_Owner, curvePos, entity, lot.m_Area, lot.m_Flags, (int)lot.m_Height, zoneData, processEstimate, this.m_Processes);
                                        break;
                                    }
                                    break;
                                case Game.Zones.AreaType.Commercial:
                                    if (this.m_SpawnCommercial != 0)
                                    {
                                        float curvePos = this.CalculateCurvePos(curvePosition, lot, block);
                                        this.TryAddLot(ref bestLocation2, ref random, owner.m_Owner, curvePos, entity, lot.m_Area, lot.m_Flags, (int)lot.m_Height, zoneData, processEstimate, this.m_Processes);
                                        break;
                                    }
                                    break;
                                case Game.Zones.AreaType.Industrial:
                                    if (this.m_SpawnIndustrial != 0 || this.m_SpawnStorage != 0)
                                    {
                                        float curvePos = this.CalculateCurvePos(curvePosition, lot, block);
                                        this.TryAddLot(ref bestLocation3, ref random, owner.m_Owner, curvePos, entity, lot.m_Area, lot.m_Flags, (int)lot.m_Height, zoneData, processEstimate, this.m_Processes, this.m_SpawnIndustrial != 0, this.m_SpawnStorage != 0);
                                        break;
                                    }
                                    break;
                            }
                        }
                    }
                }
                if ((double)bestLocation1.m_Priority != 0.0)
                {
                    this.m_Residential.Enqueue(bestLocation1);
                }
                if ((double)bestLocation2.m_Priority != 0.0)
                {
                    this.m_Commercial.Enqueue(bestLocation2);
                }
                if ((double)bestLocation3.m_Priority == 0.0)
                    return;
                this.m_Industrial.Enqueue(bestLocation3);
            }

            private float CalculateCurvePos(CurvePosition curvePosition, VacantLot lot, Game.Zones.Block block)
            {
                float s = math.saturate((float)(lot.m_Area.x + lot.m_Area.y) * 0.5f / (float)block.m_Size.x);
                return math.lerp(curvePosition.m_CurvePosition.x, curvePosition.m_CurvePosition.y, s);
            }

            private void TryAddLot(
              ref RWHZoneSpawnSystem.SpawnLocation bestLocation,
              ref Random random,
              Entity road,
              float curvePos,
              Entity entity,
              int4 area,
              LotFlags flags,
              int height,
              ZoneData zoneData,
              DynamicBuffer<ProcessEstimate> estimates,
              NativeList<IndustrialProcessData> processes,
              bool normal = true,
              bool storage = false)
            {
                if (!this.m_Availabilities.HasBuffer(road))
                    return;
                if ((zoneData.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == (ZoneFlags)0)
                    flags &= ~LotFlags.CornerLeft;
                if ((zoneData.m_ZoneFlags & ZoneFlags.SupportRightCorner) == (ZoneFlags)0)
                    flags &= ~LotFlags.CornerRight;
                RWHZoneSpawnSystem.SpawnLocation location = new RWHZoneSpawnSystem.SpawnLocation();
                location.m_Entity = entity;
                location.m_LotArea = area;
                location.m_ZoneType = zoneData.m_ZoneType;
                location.m_AreaType = zoneData.m_AreaType;
                location.m_LotFlags = flags;
                bool office = zoneData.m_AreaType == Game.Zones.AreaType.Industrial && estimates.Length == 0;
                DynamicBuffer<ResourceAvailability> availability = this.m_Availabilities[road];
                if (!this.m_BlockData.HasComponent(location.m_Entity))
                    return;
                float3 position = ZoneUtils.GetPosition(this.m_BlockData[location.m_Entity], location.m_LotArea.xz, location.m_LotArea.yw);
                bool extractor = false;
                float pollution = (float)GroundPollutionSystem.GetPollution(position, this.m_PollutionMap).m_Pollution;
                float landValue = this.m_LandValues[road].m_LandValue;
                float maxHeight = (float)height - position.y;
                if (!this.SelectBuilding(ref location, ref random, availability, zoneData, curvePos, pollution, landValue, maxHeight, estimates, processes, normal, storage, extractor, office) || (double)location.m_Priority <= (double)bestLocation.m_Priority)
                    return;
                bestLocation = location;
            }

            private bool SelectBuilding(
              ref RWHZoneSpawnSystem.SpawnLocation location,
              ref Random random,
              DynamicBuffer<ResourceAvailability> availabilities,
              ZoneData zoneData,
              float curvePos,
              float pollution,
              float landValue,
              float maxHeight,
              DynamicBuffer<ProcessEstimate> estimates,
              NativeList<IndustrialProcessData> processes,
              bool normal = true,
              bool storage = false,
              bool extractor = false,
              bool office = false)
            {
                int2 int2_1 = location.m_LotArea.yw - location.m_LotArea.xz;
                Game.Prefabs.BuildingData buildingData1 = new Game.Prefabs.BuildingData();
                bool2 bool2_1 = new bool2((location.m_LotFlags & LotFlags.CornerLeft) != 0, (location.m_LotFlags & LotFlags.CornerRight) != 0);
                bool flag = (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == (ZoneFlags)0;
                for (int index1 = 0; index1 < this.m_BuildingChunks.Length; ++index1)
                {
                    ArchetypeChunk buildingChunk = this.m_BuildingChunks[index1];
                    if (buildingChunk.GetSharedComponent<BuildingSpawnGroupData>(this.m_BuildingSpawnGroupType).m_ZoneType.Equals(location.m_ZoneType))
                    {
                        bool storage1 = buildingChunk.Has<WarehouseData>(ref this.m_WarehouseType);
                        if (!(storage1 & !storage | !storage1 & !normal))
                        {
                            NativeArray<Entity> nativeArray1 = buildingChunk.GetNativeArray(this.m_EntityType);
                            NativeArray<Game.Prefabs.BuildingData> nativeArray2 = buildingChunk.GetNativeArray<Game.Prefabs.BuildingData>(ref this.m_BuildingDataType);
                            NativeArray<SpawnableBuildingData> nativeArray3 = buildingChunk.GetNativeArray<SpawnableBuildingData>(ref this.m_SpawnableBuildingType);
                            NativeArray<BuildingPropertyData> nativeArray4 = buildingChunk.GetNativeArray<BuildingPropertyData>(ref this.m_BuildingPropertyType);
                            NativeArray<ObjectGeometryData> nativeArray5 = buildingChunk.GetNativeArray<ObjectGeometryData>(ref this.m_ObjectGeometryType);
                            for (int index2 = 0; index2 < nativeArray3.Length; ++index2)
                            {
                                if (nativeArray3[index2].m_Level == (byte)1)
                                {
                                    Game.Prefabs.BuildingData buildingData2 = nativeArray2[index2];
                                    int2 lotSize = buildingData2.m_LotSize;
                                    bool2 bool2_2 = new bool2((buildingData2.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) > (Game.Prefabs.BuildingFlags)0, (buildingData2.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) > (Game.Prefabs.BuildingFlags)0);
                                    float y = nativeArray5[index2].m_Size.y;
                                    if (math.all(lotSize <= int2_1) && (double)y <= (double)maxHeight)
                                    {
                                        BuildingPropertyData propertyData = nativeArray4[index2];

                                        float factor = 1f;

                                        int demandAndAvailability = this.EvaluateDemandAndAvailability(location.m_AreaType, propertyData, lotSize.x * lotSize.y, factor, storage1);
                                        if (demandAndAvailability >= this.m_MinDemand | extractor)
                                        {
                                            int2 int2_2 = math.select(int2_1 - lotSize, (int2)0, lotSize == int2_1 - 1);
                                            float num1 = ((float)(lotSize.x * lotSize.y) * random.NextFloat(1f, 1.05f) + (float)(int2_2.x * lotSize.y) * random.NextFloat(0.95f, 1f) + (float)(int2_1.x * int2_2.y) * random.NextFloat(0.55f, 0.6f)) / (float)(int2_1.x * int2_1.y) * (float)(demandAndAvailability + 1) * math.csum(math.select((float2)0.01f, (float2)0.5f, bool2_1 == bool2_2));
                                            if (!extractor)
                                            {
                                                float num2 = landValue;
                                                float num3;
                                                if (location.m_AreaType == Game.Zones.AreaType.Residential)
                                                {
                                                    num3 = propertyData.m_ResidentialProperties == 1 ? 2f : (float)propertyData.CountProperties();
                                                    lotSize.x = math.select(lotSize.x, int2_1.x, lotSize.x == int2_1.x - 1 & flag);
                                                    num2 *= (float)(lotSize.x * int2_1.y);
                                                }
                                                else
                                                    num3 = propertyData.m_SpaceMultiplier;

                                                float score = ZoneEvaluationUtils.GetScore(location.m_AreaType, office, availabilities, curvePos, ref this.m_Preferences, storage1, storage1 ? this.m_StorageDemands : this.m_IndustrialDemands, propertyData, pollution, num2 / num3, estimates, processes, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
                                                //Mod.log.Info($"Score:{score},Space:{propertyData.m_SpaceMultiplier}");
                                                float num4 = math.select(score, math.max(0.0f, score) + 1f, this.m_MinDemand == 0);
                                                num1 *= num4;
                                            }
                                            if ((double)num1 > (double)location.m_Priority)
                                            {
                                                location.m_Building = nativeArray1[index2];
                                                buildingData1 = buildingData2;
                                                location.m_Priority = num1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!(location.m_Building != Entity.Null))
                    return false;
                if ((buildingData1.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) == (Game.Prefabs.BuildingFlags)0 && ((buildingData1.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != (Game.Prefabs.BuildingFlags)0 || random.NextBool()))
                {
                    location.m_LotArea.x = location.m_LotArea.y - buildingData1.m_LotSize.x;
                    location.m_LotArea.w = location.m_LotArea.z + buildingData1.m_LotSize.y;
                }
                else
                {
                    location.m_LotArea.yw = location.m_LotArea.xz + buildingData1.m_LotSize;
                }
                return true;
            }

            private int EvaluateDemandAndAvailability(
              Game.Zones.AreaType areaType,
              BuildingPropertyData propertyData,
              int size,
              float factor,
              bool storage = false
              )
            {
                switch (areaType)
                {
                    case Game.Zones.AreaType.Residential:
                        if (propertyData.m_ResidentialProperties == 1)
                        {
                            return this.m_ResidentialDemands.x;
                        }
                        if(m_ResidentialDemands.y > 0 && m_ResidentialDemands.x == 0 && m_ResidentialDemands.z == 0)
                        {
                            factor /= propertyData.m_SpaceMultiplier * 2;
                        }
                        double x = (double)propertyData.m_SpaceMultiplier * factor * propertyData.m_ResidentialProperties / (double)((float)size);
                        //Mod.log.Info($"m_ResidentialProperties: {propertyData.m_ResidentialProperties}, m_SpaceMultiplier:{propertyData.m_SpaceMultiplier}, x: {x}, m_ResidentialDemands: {m_ResidentialDemands}");
                        return x < 1.0 ? this.m_ResidentialDemands.y : this.m_ResidentialDemands.z;
                    case Game.Zones.AreaType.Commercial:
                        int demandAndAvailability1 = 0;
                        ResourceIterator iterator1 = ResourceIterator.GetIterator();
                        while (iterator1.Next())
                        {
                            if ((propertyData.m_AllowedSold & iterator1.resource) != Resource.NoResource)
                            {
                                demandAndAvailability1 += this.m_CommercialBuildingDemands[EconomyUtils.GetResourceIndex(iterator1.resource)];
                            }
                        }
                        return demandAndAvailability1;
                    case Game.Zones.AreaType.Industrial:
                        int demandAndAvailability2 = 0;
                        ResourceIterator iterator2 = ResourceIterator.GetIterator();
                        while (iterator2.Next())
                        {
                            if (storage)
                            {
                                if ((propertyData.m_AllowedStored & iterator2.resource) != Resource.NoResource)
                                {
                                    demandAndAvailability2 += this.m_StorageDemands[EconomyUtils.GetResourceIndex(iterator2.resource)];
                                }
                            }
                            else if ((propertyData.m_AllowedManufactured & iterator2.resource) != Resource.NoResource)
                            {
                                demandAndAvailability2 += this.m_IndustrialDemands[EconomyUtils.GetResourceIndex(iterator2.resource)];
                            }
                        }
                        return demandAndAvailability2;
                    default:
                        return 0;
                }
            }

            void IJobChunk.Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        [BurstCompile]
        public struct SpawnBuildingJob : IJobParallelFor
        {
            [ReadOnly]
            public ComponentLookup<Game.Zones.Block> m_BlockData;
            [ReadOnly]
            public ComponentLookup<ValidArea> m_ValidAreaData;
            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_PrefabBuildingData;
            [ReadOnly]
            public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;
            [ReadOnly]
            public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;
            [ReadOnly]
            public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;
            [ReadOnly]
            public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;
            [ReadOnly]
            public BufferLookup<Cell> m_Cells;
            [ReadOnly]
            public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;
            [ReadOnly]
            public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;
            [ReadOnly]
            public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;
            [ReadOnly]
            public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;
            [ReadOnly]
            public EntityArchetype m_DefinitionArchetype;
            [ReadOnly]
            public RandomSeed m_RandomSeed;
            [ReadOnly]
            public bool m_LefthandTraffic;
            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;
            [ReadOnly]
            public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;
            [NativeDisableParallelForRestriction]
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation> m_Residential;
            [NativeDisableParallelForRestriction]
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation> m_Commercial;
            [NativeDisableParallelForRestriction]
            public NativeQueue<RWHZoneSpawnSystem.SpawnLocation> m_Industrial;
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(int index)
            {
                RWHZoneSpawnSystem.SpawnLocation location;
                switch (index)
                {
                    case 0:
                        if (!this.SelectLocation(this.m_Residential, out location))
                            return;
                        break;
                    case 1:
                        if (!this.SelectLocation(this.m_Commercial, out location))
                            return;
                        break;
                    case 2:
                        if (!this.SelectLocation(this.m_Industrial, out location))
                            return;
                        break;
                    default:
                        return;
                }
                Random random = this.m_RandomSeed.GetRandom(index);
                this.Spawn(index, location, ref random);
            }

            private bool SelectLocation(
              NativeQueue<RWHZoneSpawnSystem.SpawnLocation> queue,
              out RWHZoneSpawnSystem.SpawnLocation location)
            {
                location = new RWHZoneSpawnSystem.SpawnLocation();
                RWHZoneSpawnSystem.SpawnLocation spawnLocation;
                while (queue.TryDequeue(out spawnLocation))
                {
                    if ((double)spawnLocation.m_Priority > (double)location.m_Priority)
                        location = spawnLocation;
                }
                return (double)location.m_Priority != 0.0;
            }

            private void Spawn(int jobIndex, RWHZoneSpawnSystem.SpawnLocation location, ref Random random)
            {
                Game.Prefabs.BuildingData prefabBuildingData = this.m_PrefabBuildingData[location.m_Building];
                ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[location.m_Building];
                PlaceableObjectData placeableObjectData = new PlaceableObjectData();
                if (this.m_PrefabPlaceableObjectData.HasComponent(location.m_Building))
                {
                    placeableObjectData = this.m_PrefabPlaceableObjectData[location.m_Building];
                }
                CreationDefinition component1 = new CreationDefinition();
                component1.m_Prefab = location.m_Building;
                component1.m_Flags |= CreationFlags.Permanent | CreationFlags.Construction;
                component1.m_RandomSeed = random.NextInt();
                Transform transform1 = new Transform();
                if (this.m_BlockData.HasComponent(location.m_Entity))
                {
                    Game.Zones.Block block = this.m_BlockData[location.m_Entity];
                    transform1.m_Position = ZoneUtils.GetPosition(block, location.m_LotArea.xz, location.m_LotArea.yw);
                    transform1.m_Rotation = ZoneUtils.GetRotation(block);
                }
                else
                {
                    if (this.m_TransformData.HasComponent(location.m_Entity))
                    {
                        component1.m_Attached = location.m_Entity;
                        component1.m_Flags |= CreationFlags.Attach;
                        Transform transform2 = this.m_TransformData[location.m_Entity];
                        Game.Prefabs.BuildingData buildingData = this.m_PrefabBuildingData[this.m_PrefabRefData[location.m_Entity].m_Prefab];
                        transform1.m_Position = transform2.m_Position;
                        transform1.m_Rotation = transform2.m_Rotation;
                        float z = (float)(buildingData.m_LotSize.y - prefabBuildingData.m_LotSize.y) * 4f;
                        transform1.m_Position += math.rotate(transform1.m_Rotation, new float3(0.0f, 0.0f, z));
                    }
                }
                float3 frontPosition = Game.Buildings.BuildingUtils.CalculateFrontPosition(transform1, prefabBuildingData.m_LotSize.y);
                transform1.m_Position.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, frontPosition);
                if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == Game.Objects.PlacementFlags.None)
                    transform1.m_Position.y += placeableObjectData.m_PlacementOffset.y;
                float maxHeight = this.GetMaxHeight(transform1, prefabBuildingData);
                transform1.m_Position.y = math.min(transform1.m_Position.y, (float)((double)maxHeight - (double)objectGeometryData.m_Size.y - 0.10000000149011612));
                ObjectDefinition component2 = new ObjectDefinition();
                component2.m_ParentMesh = -1;
                component2.m_Position = transform1.m_Position;
                component2.m_Rotation = transform1.m_Rotation;
                component2.m_LocalPosition = transform1.m_Position;
                component2.m_LocalRotation = transform1.m_Rotation;

                Entity entity = this.m_CommandBuffer.CreateEntity(jobIndex, this.m_DefinitionArchetype);
                this.m_CommandBuffer.SetComponent<CreationDefinition>(jobIndex, entity, component1);
                this.m_CommandBuffer.SetComponent<ObjectDefinition>(jobIndex, entity, component2);
                OwnerDefinition ownerDefinition = new OwnerDefinition();
                ownerDefinition.m_Prefab = location.m_Building;
                ownerDefinition.m_Position = component2.m_Position;
                ownerDefinition.m_Rotation = component2.m_Rotation;
                if (this.m_PrefabSubAreas.HasBuffer(location.m_Building))
                {
                    this.Spawn(jobIndex, ownerDefinition, this.m_PrefabSubAreas[location.m_Building], this.m_PrefabSubAreaNodes[location.m_Building], prefabBuildingData, ref random);
                }
                if (!this.m_PrefabSubNets.HasBuffer(location.m_Building))
                    return;

                this.Spawn(jobIndex, ownerDefinition, this.m_PrefabSubNets[location.m_Building], ref random);
            }

            private float GetMaxHeight(Transform transform, Game.Prefabs.BuildingData prefabBuildingData)
            {
                float2 xz1 = math.rotate(transform.m_Rotation, new float3(8f, 0.0f, 0.0f)).xz;
                float2 xz2 = math.rotate(transform.m_Rotation, new float3(0.0f, 0.0f, 8f)).xz;
                float2 x1 = xz1 * (float)((double)prefabBuildingData.m_LotSize.x * 0.5 - 0.5);
                float2 x2 = xz2 * (float)((double)prefabBuildingData.m_LotSize.y * 0.5 - 0.5);
                float2 float2 = math.abs(x2) + math.abs(x1);

                RWHZoneSpawnSystem.SpawnBuildingJob.Iterator iterator = new RWHZoneSpawnSystem.SpawnBuildingJob.Iterator()
                {
                    m_Bounds = new Bounds2(transform.m_Position.xz - float2, transform.m_Position.xz + float2),
                    m_LotSize = prefabBuildingData.m_LotSize,
                    m_StartPosition = transform.m_Position.xz + x2 + x1,
                    m_Right = xz1,
                    m_Forward = xz2,
                    m_MaxHeight = int.MaxValue,
                    m_BlockData = this.m_BlockData,
                    m_ValidAreaData = this.m_ValidAreaData,
                    m_Cells = this.m_Cells
                };
                this.m_ZoneSearchTree.Iterate<RWHZoneSpawnSystem.SpawnBuildingJob.Iterator>(ref iterator);
                return (float)iterator.m_MaxHeight;
            }

            private void Spawn(
              int jobIndex,
              OwnerDefinition ownerDefinition,
              DynamicBuffer<Game.Prefabs.SubArea> subAreas,
              DynamicBuffer<SubAreaNode> subAreaNodes,
              Game.Prefabs.BuildingData prefabBuildingData,
              ref Random random)
            {
                NativeParallelHashMap<Entity, int> selectedSpawnables = new NativeParallelHashMap<Entity, int>();
                bool flag = false;
                for (int index1 = 0; index1 < subAreas.Length; ++index1)
                {
                    Game.Prefabs.SubArea subArea = subAreas[index1];
                    // ISSUE: reference to a compiler-generated field
                    AreaGeometryData areaGeometryData = this.m_PrefabAreaGeometryData[subArea.m_Prefab];
                    if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
                    {
                        if (!flag)
                        {
                            subArea.m_Prefab = this.m_BuildingConfigurationData.m_ConstructionSurface;
                            flag = true;
                        }
                        else
                            continue;
                    }
                    DynamicBuffer<PlaceholderObjectElement> bufferData;
                    int seed;
                    if (this.m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out bufferData))
                    {
                        if (!selectedSpawnables.IsCreated)
                            selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, (AllocatorManager.AllocatorHandle)Allocator.Temp);
                        if (!AreaUtils.SelectAreaPrefab(bufferData, this.m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
                            continue;
                    }
                    else
                        seed = random.NextInt();
                    Entity entity = this.m_CommandBuffer.CreateEntity(jobIndex);
                    CreationDefinition component = new CreationDefinition();
                    component.m_Prefab = subArea.m_Prefab;
                    component.m_RandomSeed = seed;
                    component.m_Flags |= CreationFlags.Permanent;
                    this.m_CommandBuffer.AddComponent<CreationDefinition>(jobIndex, entity, component);
                    this.m_CommandBuffer.AddComponent<Updated>(jobIndex, entity, new Updated());
                    this.m_CommandBuffer.AddComponent<OwnerDefinition>(jobIndex, entity, ownerDefinition);
                    DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, entity);
                    if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
                    {
                        Quad3 corners = Game.Buildings.BuildingUtils.CalculateCorners(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation), prefabBuildingData.m_LotSize);
                        dynamicBuffer.ResizeUninitialized(5);
                        dynamicBuffer[0] = new Game.Areas.Node(corners.a, float.MinValue);
                        dynamicBuffer[1] = new Game.Areas.Node(corners.b, float.MinValue);
                        dynamicBuffer[2] = new Game.Areas.Node(corners.c, float.MinValue);
                        dynamicBuffer[3] = new Game.Areas.Node(corners.d, float.MinValue);
                        dynamicBuffer[4] = new Game.Areas.Node(corners.a, float.MinValue);
                    }
                    else
                    {
                        dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
                        int index2 = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
                        int index3 = 0;
                        for (int x = subArea.m_NodeRange.x; x <= subArea.m_NodeRange.y; ++x)
                        {
                            float3 position = subAreaNodes[index2].m_Position;
                            float3 world = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, position);
                            int parentMesh = subAreaNodes[index2].m_ParentMesh;
                            float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
                            dynamicBuffer[index3] = new Game.Areas.Node(world, elevation);
                            ++index3;
                            if (++index2 == subArea.m_NodeRange.y)
                                index2 = subArea.m_NodeRange.x;
                        }
                    }
                }
                if (!selectedSpawnables.IsCreated)
                    return;
                selectedSpawnables.Dispose();
            }

            private void Spawn(
              int jobIndex,
              OwnerDefinition ownerDefinition,
              DynamicBuffer<Game.Prefabs.SubNet> subNets,
              ref Random random)
            {
                NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, (AllocatorManager.AllocatorHandle)Allocator.Temp);
                for (int index = 0; index < subNets.Length; ++index)
                {
                    Game.Prefabs.SubNet subNet = subNets[index];
                    float4 float4;
                    if (subNet.m_NodeIndex.x >= 0)
                    {
                        while (nodePositions.Length <= subNet.m_NodeIndex.x)
                        {
                            ref NativeList<float4> local1 = ref nodePositions;
                            float4 = new float4();
                            ref float4 local2 = ref float4;
                            local1.Add(in local2);
                        }
                        nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
                    }
                    if (subNet.m_NodeIndex.y >= 0)
                    {
                        while (nodePositions.Length <= subNet.m_NodeIndex.y)
                        {
                            ref NativeList<float4> local3 = ref nodePositions;
                            float4 = new float4();
                            ref float4 local4 = ref float4;
                            local3.Add(in local4);
                        }
                        nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
                    }
                }
                for (int index = 0; index < nodePositions.Length; ++index)
                    nodePositions[index] /= math.max(1f, nodePositions[index].w);
                for (int index = 0; index < subNets.Length; ++index)
                {
                    Game.Prefabs.SubNet subNet = NetUtils.GetSubNet(subNets, index, this.m_LefthandTraffic, ref this.m_PrefabNetGeometryData);
                    this.CreateSubNet(jobIndex, subNet.m_Prefab, subNet.m_Curve, subNet.m_NodeIndex, subNet.m_ParentMesh, subNet.m_Upgrades, nodePositions, ownerDefinition, ref random);
                }
                nodePositions.Dispose();
            }

            private void CreateSubNet(
              int jobIndex,
              Entity netPrefab,
              Bezier4x3 curve,
              int2 nodeIndex,
              int2 parentMesh,
              CompositionFlags upgrades,
              NativeList<float4> nodePositions,
              OwnerDefinition ownerDefinition,
              ref Random random)
            {
                Entity entity = this.m_CommandBuffer.CreateEntity(jobIndex);
                CreationDefinition component1 = new CreationDefinition();
                component1.m_Prefab = netPrefab;
                component1.m_RandomSeed = random.NextInt();
                component1.m_Flags |= CreationFlags.Permanent;
                this.m_CommandBuffer.AddComponent<CreationDefinition>(jobIndex, entity, component1);
                this.m_CommandBuffer.AddComponent<Updated>(jobIndex, entity, new Updated());
                this.m_CommandBuffer.AddComponent<OwnerDefinition>(jobIndex, entity, ownerDefinition);
                NetCourse component2 = new NetCourse()
                {
                    m_Curve = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, curve)
                };
                component2.m_StartPosition.m_Position = component2.m_Curve.a;
                component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), ownerDefinition.m_Rotation);
                component2.m_StartPosition.m_CourseDelta = 0.0f;
                component2.m_StartPosition.m_Elevation = (float2)curve.a.y;
                component2.m_StartPosition.m_ParentMesh = parentMesh.x;
                if (nodeIndex.x >= 0)
                    component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.x].xyz);
                component2.m_EndPosition.m_Position = component2.m_Curve.d;
                component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), ownerDefinition.m_Rotation);
                component2.m_EndPosition.m_CourseDelta = 1f;
                component2.m_EndPosition.m_Elevation = (float2)curve.d.y;
                component2.m_EndPosition.m_ParentMesh = parentMesh.y;
                if (nodeIndex.y >= 0)
                    component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.y].xyz);
                component2.m_Length = MathUtils.Length(component2.m_Curve);
                component2.m_FixedIndex = -1;
                component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
                component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
                if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
                {
                    component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
                    component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
                }
                this.m_CommandBuffer.AddComponent<NetCourse>(jobIndex, entity, component2);
                if (!(upgrades != new CompositionFlags()))
                    return;
                Upgraded component3 = new Upgraded()
                {
                    m_Flags = upgrades
                };
                this.m_CommandBuffer.AddComponent<Upgraded>(jobIndex, entity, component3);
            }

            private struct Iterator :
              INativeQuadTreeIterator<Entity, Bounds2>,
              IUnsafeQuadTreeIterator<Entity, Bounds2>
            {
                public Bounds2 m_Bounds;
                public int2 m_LotSize;
                public float2 m_StartPosition;
                public float2 m_Right;
                public float2 m_Forward;
                public int m_MaxHeight;
                public ComponentLookup<Game.Zones.Block> m_BlockData;
                public ComponentLookup<ValidArea> m_ValidAreaData;
                public BufferLookup<Cell> m_Cells;

                public bool Intersect(Bounds2 bounds) => MathUtils.Intersect(bounds, this.m_Bounds);

                public void Iterate(Bounds2 bounds, Entity blockEntity)
                {
                    if (!MathUtils.Intersect(bounds, this.m_Bounds))
                        return;
                    ValidArea validArea = this.m_ValidAreaData[blockEntity];
                    if (validArea.m_Area.y <= validArea.m_Area.x)
                        return;
                    Game.Zones.Block block = this.m_BlockData[blockEntity];
                    DynamicBuffer<Cell> cell1 = this.m_Cells[blockEntity];
                    float2 startPosition = this.m_StartPosition;
                    int2 int2;
                    for (int2.y = 0; int2.y < this.m_LotSize.y; ++int2.y)
                    {
                        float2 position = startPosition;
                        for (int2.x = 0; int2.x < this.m_LotSize.x; ++int2.x)
                        {
                            int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
                            if (math.all(cellIndex >= validArea.m_Area.xz & cellIndex < validArea.m_Area.yw))
                            {
                                int index = cellIndex.y * block.m_Size.x + cellIndex.x;
                                Cell cell2 = cell1[index];
                                if ((cell2.m_State & CellFlags.Visible) != CellFlags.None)
                                {
                                    this.m_MaxHeight = math.min(this.m_MaxHeight, (int)cell2.m_Height);
                                }
                            }
                            position -= this.m_Right;
                        }
                        startPosition -= this.m_Forward;
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Zones.Block> __Game_Zones_Block_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CurvePosition> __Game_Zones_CurvePosition_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<VacantLot> __Game_Zones_VacantLot_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;
            public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<WarehouseData> __Game_Prefabs_WarehouseData_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<BuildingPropertyExtraData> __Game_Prefabs_BuildingPropertyExtraData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Zones.Block> __Game_Zones_Block_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<ProcessEstimate> __Game_Zones_ProcessEstimate_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Zones.Block>(true);
                this.__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(true);
                this.__Game_Zones_CurvePosition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurvePosition>(true);
                this.__Game_Zones_VacantLot_RO_BufferTypeHandle = state.GetBufferTypeHandle<VacantLot>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Prefabs.BuildingData>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(true);
                this.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
                this.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WarehouseData>(true);
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                this.__Game_Prefabs_BuildingPropertyExtraData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyExtraData>(true);
                this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
                this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(true);
                this.__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Game.Zones.Block>(true);
                this.__Game_Zones_ProcessEstimate_RO_BufferLookup = state.GetBufferLookup<ProcessEstimate>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                this.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(true);
                this.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(true);
                this.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(true);
                this.__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(true);
                this.__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(true);
                this.__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(true);
                this.__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(true);
                this.__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(true);
                this.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(true);
            }
        }
    }
}
