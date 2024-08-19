using Game;
using Game.Simulation;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using RealisticWorkplacesAndHouseholds;

#nullable disable
namespace RWH.Systems
{
    public partial class RWHBuildingPollutionAddSystem : GameSystemBase
    {
        public static readonly int kUpdatesPerDay = 128;
        private SimulationSystem m_SimulationSystem;
        private GroundPollutionSystem m_GroundPollutionSystem;
        private AirPollutionSystem m_AirPollutionSystem;
        private NoisePollutionSystem m_NoisePollutionSystem;
        private CitySystem m_CitySystem;
        private EntityQuery m_PolluterQuery;
        private NativeArray<float> m_GroundWeightCache;
        private NativeArray<float> m_AirWeightCache;
        private NativeArray<float> m_NoiseWeightCache;
        private NativeArray<float> m_DistanceWeightCache;
        private NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem> m_GroundPollutionQueue;
        private NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem> m_AirPollutionQueue;
        private NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem> m_NoisePollutionQueue;
        private RWHBuildingPollutionAddSystem.TypeHandle __TypeHandle;
        private EntityQuery __query_985639355_0;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (16 * RWHBuildingPollutionAddSystem.kUpdatesPerDay);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_GroundPollutionSystem = this.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = this.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = this.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_GroundPollutionQueue = new NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_AirPollutionQueue = new NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_NoisePollutionQueue = new NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>((AllocatorManager.AllocatorHandle)Allocator.Persistent);
            this.m_PolluterQuery = this.GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Placeholder>());
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            if (this.m_GroundWeightCache.IsCreated)
            {
                this.m_GroundWeightCache.Dispose();
            }

            if (this.m_AirWeightCache.IsCreated)
            {
                this.m_AirWeightCache.Dispose();
            }
             
            if (this.m_NoiseWeightCache.IsCreated)
            {
                this.m_NoiseWeightCache.Dispose();
            }

            if (this.m_DistanceWeightCache.IsCreated)
            {
                this.m_DistanceWeightCache.Dispose();
            }

            this.m_GroundPollutionQueue.Dispose();
            this.m_AirPollutionQueue.Dispose();
            this.m_NoisePollutionQueue.Dispose();
            base.OnDestroy();
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            PollutionParameterData singleton = this.__query_985639355_0.GetSingleton<PollutionParameterData>();
            float num1 = math.max(math.max(singleton.m_GroundRadius, singleton.m_AirRadius), singleton.m_NoiseRadius);
            float num2 = num1 * num1;
            if (!this.m_GroundWeightCache.IsCreated)
            {
                int num3 = 3 + Mathf.CeilToInt(2f * singleton.m_GroundRadius * (float)GroundPollutionSystem.kTextureSize / (float)CellMapSystem<GroundPollution>.kMapSize);
                this.m_GroundWeightCache = new NativeArray<float>(num3 * num3, Allocator.Persistent);
                int num4 = 3 + Mathf.CeilToInt(2f * singleton.m_AirRadius * (float)AirPollutionSystem.kTextureSize / (float)CellMapSystem<AirPollution>.kMapSize);
                this.m_AirWeightCache = new NativeArray<float>(num4 * num4, Allocator.Persistent);
                int num5 = 3 + Mathf.CeilToInt(2f * singleton.m_NoiseRadius * (float)NoisePollutionSystem.kTextureSize / (float)CellMapSystem<NoisePollution>.kMapSize);
                this.m_NoiseWeightCache = new NativeArray<float>(num5 * num5, Allocator.Persistent);
                this.m_DistanceWeightCache = new NativeArray<float>(256, Allocator.Persistent);
                for (int index = 0; index < 256; ++index)
                {
                    this.m_DistanceWeightCache[index] = RWHBuildingPollutionAddSystem.GetWeight(math.sqrt((float)((double)num2 * (double)index / 256.0)), singleton.m_DistanceExponent);
                }
            }
            uint frameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref this.CheckedStateRef);

            JobHandle jobHandle1 = new RWHBuildingPollutionAddSystem.BuildingPolluteJob()
            {
                m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle,
                m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                m_DestroyedType = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle,
                m_AbandonedType = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle,
                m_ParkType = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle,
                m_BuildingEfficiencyType = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle,
                m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,
                m_InstalledUpgradeType = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle,
                m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup,
                m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup,
                m_SpawnableDatas = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup,
                m_PollutionDatas = this.__TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup,
                m_PollutionModifierDatas = this.__TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup,
                m_ZoneDatas = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup,
                m_Employees = this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup,
                m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup,
                m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_PollutionParameters = singleton,
                m_GroundPollutionQueue = this.m_GroundPollutionQueue.AsParallelWriter(),
                m_AirPollutionQueue = this.m_AirPollutionQueue.AsParallelWriter(),
                m_NoisePollutionQueue = this.m_NoisePollutionQueue.AsParallelWriter(),
                m_City = this.m_CitySystem.City,
                m_UpdateFrameIndex = frameWithInterval
            }.ScheduleParallel<RWHBuildingPollutionAddSystem.BuildingPolluteJob>(this.m_PolluterQuery, this.Dependency);
            JobHandle dependencies1;

            JobHandle jobHandle2 = new RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<GroundPollution>()
            {
                m_PollutionMap = this.m_GroundPollutionSystem.GetMap(false, out dependencies1),
                m_MapSize = CellMapSystem<GroundPollution>.kMapSize,
                m_TextureSize = GroundPollutionSystem.kTextureSize,
                m_PollutionParameters = singleton,
                m_MaxRadiusSq = num2,
                m_Radius = singleton.m_GroundRadius,
                m_PollutionQueue = this.m_GroundPollutionQueue,
                m_WeightCache = this.m_GroundWeightCache,
                m_DistanceWeightCache = this.m_DistanceWeightCache,
                m_Multiplier = singleton.m_GroundMultiplier
            }.Schedule<RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<GroundPollution>>(JobHandle.CombineDependencies(jobHandle1, dependencies1));
            // ISSUE: reference to a compiler-generated field
            this.m_GroundPollutionSystem.AddWriter(jobHandle2);
            JobHandle dependencies2;

            JobHandle jobHandle3 = new RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<AirPollution>()
            {
                m_PollutionMap = this.m_AirPollutionSystem.GetMap(false, out dependencies2),
                m_MapSize = CellMapSystem<AirPollution>.kMapSize,
                m_TextureSize = AirPollutionSystem.kTextureSize,
                m_PollutionParameters = singleton,
                m_MaxRadiusSq = num2,
                m_Radius = singleton.m_AirRadius,
                m_PollutionQueue = this.m_AirPollutionQueue,
                m_WeightCache = this.m_AirWeightCache,
                m_DistanceWeightCache = this.m_DistanceWeightCache,
                m_Multiplier = singleton.m_AirMultiplier
            }.Schedule<RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<AirPollution>>(JobHandle.CombineDependencies(dependencies2, jobHandle1));

            this.m_AirPollutionSystem.AddWriter(jobHandle3);
            JobHandle dependencies3;
            JobHandle jobHandle4 = new RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<NoisePollution>()
            {
                m_PollutionMap = this.m_NoisePollutionSystem.GetMap(false, out dependencies3),
                m_MapSize = CellMapSystem<NoisePollution>.kMapSize,
                m_TextureSize = NoisePollutionSystem.kTextureSize,
                m_PollutionParameters = singleton,
                m_MaxRadiusSq = num2,
                m_Radius = singleton.m_NoiseRadius,
                m_PollutionQueue = this.m_NoisePollutionQueue,
                m_WeightCache = this.m_NoiseWeightCache,
                m_DistanceWeightCache = this.m_DistanceWeightCache,
                m_Multiplier = singleton.m_NoiseMultiplier
            }.Schedule<RWHBuildingPollutionAddSystem.ApplyBuildingPollutionJob<NoisePollution>>(JobHandle.CombineDependencies(dependencies3, jobHandle1));

            this.m_NoisePollutionSystem.AddWriter(jobHandle4);
            this.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
        }

        private static float GetWeight(float distance, float exponent)
        {
            return 1f / math.max(20f, math.pow(distance, exponent));
        }

        public static PollutionData GetBuildingPollution(
          Entity prefab,
          bool destroyed,
          bool abandoned,
          bool isPark,
          float efficiency,
          DynamicBuffer<Renter> renters,
          DynamicBuffer<InstalledUpgrade> installedUpgrades,
          PollutionParameterData pollutionParameters,
          DynamicBuffer<CityModifier> cityModifiers,
          ref ComponentLookup<PrefabRef> prefabRefs,
          ref ComponentLookup<Game.Prefabs.BuildingData> buildingDatas,
          ref ComponentLookup<SpawnableBuildingData> spawnableDatas,
          ref ComponentLookup<PollutionData> pollutionDatas,
          ref ComponentLookup<PollutionModifierData> pollutionModifierDatas,
          ref ComponentLookup<ZoneData> zoneDatas,
          ref BufferLookup<Employee> employees,
          ref BufferLookup<HouseholdCitizen> householdCitizens,
          ref ComponentLookup<Citizen> citizens)
        {
            PollutionData componentData1;
            if (!(destroyed | abandoned))
            {
                if ((double)efficiency > 0.0 && pollutionDatas.TryGetComponent(prefab, out componentData1))
                {
                    if (installedUpgrades.IsCreated)
                        UpgradeUtils.CombineStats<PollutionData>(ref componentData1, installedUpgrades, ref prefabRefs, ref pollutionDatas);
                    SpawnableBuildingData componentData2;
                    if (componentData1.m_ScaleWithRenters && !isPark && renters.IsCreated)
                    {
                        int count;
                        int education;

                        RWHBuildingPollutionAddSystem.CountRenters(out count, out education, renters, ref employees, ref householdCitizens, ref citizens, false);

                        float num1 = spawnableDatas.TryGetComponent(prefab, out componentData2) ? (float)componentData2.m_Level : 5f;
                        float num2 = count > 0 ? (float)(5.0 * (double)count / ((double)num1 + 0.5 * (double)(education / count))) : 0.0f;
                        componentData1.m_GroundPollution *= num2;
                        componentData1.m_AirPollution *= num2;
                        componentData1.m_NoisePollution *= num2;
                    }
                    if (cityModifiers.IsCreated && spawnableDatas.TryGetComponent(prefab, out componentData2))
                    {
                        ZoneData zoneData = zoneDatas[componentData2.m_ZonePrefab];
                        if (zoneData.m_AreaType == AreaType.Industrial && (zoneData.m_ZoneFlags & ZoneFlags.Office) == (ZoneFlags)0)
                        {
                            CityUtils.ApplyModifier(ref componentData1.m_GroundPollution, cityModifiers, CityModifierType.IndustrialGroundPollution);
                            CityUtils.ApplyModifier(ref componentData1.m_AirPollution, cityModifiers, CityModifierType.IndustrialAirPollution);
                        }
                    }
                    if (installedUpgrades.IsCreated)
                    {
                        PollutionModifierData result = new PollutionModifierData();
                        UpgradeUtils.CombineStats<PollutionModifierData>(ref result, installedUpgrades, ref prefabRefs, ref pollutionModifierDatas);
                        componentData1.m_GroundPollution *= math.max(0.0f, 1f + result.m_GroundPollutionMultiplier);
                        componentData1.m_AirPollution *= math.max(0.0f, 1f + result.m_AirPollutionMultiplier);
                        componentData1.m_NoisePollution *= math.max(0.0f, 1f + result.m_NoisePollutionMultiplier);
                    }
                }
                else
                    componentData1 = new PollutionData();
            }
            else
            {
                Game.Prefabs.BuildingData buildingData = buildingDatas[prefab];
                componentData1 = new PollutionData()
                {
                    m_GroundPollution = 0.0f,
                    m_AirPollution = 0.0f,
                    m_NoisePollution = destroyed ? 0.0f : 5f * (float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y) * pollutionParameters.m_AbandonedNoisePollutionMultiplier
                };
            }
            if (abandoned | isPark && renters.IsCreated)
            {
                int count;

                RWHBuildingPollutionAddSystem.CountRenters(out count, out int _, renters, ref employees, ref householdCitizens, ref citizens, true);
                componentData1.m_NoisePollution += (float)(count * pollutionParameters.m_HomelessNoisePollution);
            }
            return componentData1;
        }

        private static void CountRenters(
          out int count,
          out int education,
          DynamicBuffer<Renter> renters,
          ref BufferLookup<Employee> employees,
          ref BufferLookup<HouseholdCitizen> householdCitizens,
          ref ComponentLookup<Citizen> citizens,
          bool ignoreEmployees)
        {
            count = 0;
            education = 0;
            foreach (Renter renter in renters)
            {
                DynamicBuffer<HouseholdCitizen> bufferData1;
                //if (householdCitizens.TryGetBuffer((Entity)renter, out bufferData1))
                //{
                //    foreach (HouseholdCitizen householdCitizen in bufferData1)
                //    {
                //        Citizen componentData;
                //        if (citizens.TryGetComponent((Entity)householdCitizen, out componentData))
                //        {
                //            education += componentData.GetEducationLevel();
                //            ++count;
                //        }
                //    }
                //}
                //else
                {
                    DynamicBuffer<Employee> bufferData2;
                    if (!ignoreEmployees && employees.TryGetBuffer((Entity)renter, out bufferData2))
                    {
                        foreach (Employee employee in bufferData2)
                        {
                            Citizen componentData;
                            if (citizens.TryGetComponent(employee.m_Worker, out componentData))
                            {
                                education += componentData.GetEducationLevel();
                                ++count;
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {

            this.__query_985639355_0 = state.GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[1]
              {
          ComponentType.ReadOnly<PollutionParameterData>()
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
        public RWHBuildingPollutionAddSystem()
        {
        }

        private struct PollutionItem
        {
            public int amount;
            public float2 position;
        }

        [BurstCompile]
        private struct ApplyBuildingPollutionJob<T> : IJob where T : struct, IPollution
        {
            public NativeArray<T> m_PollutionMap;
            public NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem> m_PollutionQueue;
            public int m_MapSize;
            public int m_TextureSize;
            public float m_MaxRadiusSq;
            public float m_Radius;
            public float m_Multiplier;
            public NativeArray<float> m_WeightCache;
            [ReadOnly]
            public NativeArray<float> m_DistanceWeightCache;
            public PollutionParameterData m_PollutionParameters;

            private float GetWeight(
              int2 cell,
              float2 position,
              float radiusSq,
              float offset,
              int cellSize)
            {
                float2 float2 = new float2((float)(-(double)offset + ((double)cell.x + 0.5) * (double)cellSize), (float)(-(double)offset + ((double)cell.y + 0.5) * (double)cellSize));
                float num1 = math.lengthsq(position - float2);
                if ((double)num1 >= (double)radiusSq)
                    return 0.0f;
                float num2 = (float)byte.MaxValue * num1 / this.m_MaxRadiusSq;
                int index = Mathf.FloorToInt(num2);

                return math.lerp(this.m_DistanceWeightCache[index], this.m_DistanceWeightCache[index + 1], math.frac(num2));
            }

            private void AddSingle(
              int pollution,
              int mapSize,
              int textureSize,
              float2 position,
              float radius,
              NativeArray<T> map)
            {
                int cellSize = mapSize / textureSize;
                float num1 = (float)mapSize / 2f;
                float radiusSq = radius * radius;
                int2 int2_1 = new int2(math.max(0, Mathf.FloorToInt((position.x + num1 - radius) / (float)cellSize)), math.max(0, Mathf.FloorToInt((position.y + num1 - radius) / (float)cellSize)));
                int2 int2_2 = new int2(math.min(textureSize - 1, Mathf.CeilToInt((position.x + num1 + radius) / (float)cellSize)), math.min(textureSize - 1, Mathf.CeilToInt((position.y + num1 + radius) / (float)cellSize)));
                float num2 = 0.0f;
                int index1 = 0;
                int2 cell;
                for (cell.x = int2_1.x; cell.x <= int2_2.x; ++cell.x)
                {
                    for (cell.y = int2_1.y; cell.y <= int2_2.y; ++cell.y)
                    {
                        float weight = this.GetWeight(cell, position, radiusSq, 0.5f * (float)mapSize, cellSize);
                        num2 += weight;
                        this.m_WeightCache[index1] = weight;
                        ++index1;
                    }
                }
                int index2 = 0;
                float num3 = (float)(1.0 / ((double)num2 * (double)RWHBuildingPollutionAddSystem.kUpdatesPerDay));
                for (cell.x = int2_1.x; cell.x <= int2_2.x; ++cell.x)
                {
                    int index3 = cell.x + textureSize * int2_1.y;
                    for (cell.y = int2_1.y; cell.y <= int2_2.y; ++cell.y)
                    {
                        float f = (float)pollution * num3 * this.m_WeightCache[index2];
                        ++index2;
                        if ((double)f > 0.20000000298023224)
                        {
                            int amount = Mathf.CeilToInt(f);
                            T obj = map[index3];
                            obj.Add((short)amount);
                            map[index3] = obj;
                        }
                        index3 += textureSize;
                    }
                }
            }

            public void Execute()
            {
                RWHBuildingPollutionAddSystem.PollutionItem pollutionItem;
                while (this.m_PollutionQueue.TryDequeue(out pollutionItem))
                {
                    this.AddSingle((int)((double)this.m_Multiplier * (double)pollutionItem.amount), this.m_MapSize, this.m_TextureSize, pollutionItem.position, this.m_Radius, this.m_PollutionMap);
                }
            }
        }

        [BurstCompile]
        private struct BuildingPolluteJob : IJobChunk
        {
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            [ReadOnly]
            public ComponentTypeHandle<Destroyed> m_DestroyedType;
            [ReadOnly]
            public ComponentTypeHandle<Abandoned> m_AbandonedType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;
            [ReadOnly]
            public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;
            [ReadOnly]
            public BufferTypeHandle<Renter> m_RenterType;
            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> m_BuildingDatas;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;
            [ReadOnly]
            public ComponentLookup<PollutionData> m_PollutionDatas;
            [ReadOnly]
            public ComponentLookup<PollutionModifierData> m_PollutionModifierDatas;
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneDatas;
            [ReadOnly]
            public BufferLookup<Employee> m_Employees;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            [ReadOnly]
            public PollutionParameterData m_PollutionParameters;
            public NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>.ParallelWriter m_GroundPollutionQueue;
            public NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>.ParallelWriter m_AirPollutionQueue;
            public NativeQueue<RWHBuildingPollutionAddSystem.PollutionItem>.ParallelWriter m_NoisePollutionQueue;
            public Entity m_City;
            public uint m_UpdateFrameIndex;

            public void Execute(
              in ArchetypeChunk chunk,
              int unfilteredChunkIndex,
              bool useEnabledMask,
              in v128 chunkEnabledMask)
            {
                if ((int)chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != (int)this.m_UpdateFrameIndex)
                    return;
                NativeArray<PrefabRef> nativeArray1 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabRefType);
                NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray<Game.Objects.Transform>(ref this.m_TransformType);
                bool flag1 = chunk.Has<Destroyed>(ref this.m_DestroyedType);
                bool flag2 = chunk.Has<Abandoned>(ref this.m_AbandonedType);
                bool flag3 = chunk.Has<Game.Buildings.Park>(ref this.m_ParkType);
                BufferAccessor<Efficiency> bufferAccessor1 = chunk.GetBufferAccessor<Efficiency>(ref this.m_BuildingEfficiencyType);
                BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor<InstalledUpgrade>(ref this.m_InstalledUpgradeType);
                DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];
                for (int index = 0; index < chunk.Count; ++index)
                {
                    Entity prefab = nativeArray1[index].m_Prefab;
                    float3 position = nativeArray2[index].m_Position;
                    float efficiency1 = Game.Buildings.BuildingUtils.GetEfficiency(bufferAccessor1, index);
                    DynamicBuffer<Renter> dynamicBuffer1 = bufferAccessor2.Length != 0 ? bufferAccessor2[index] : new DynamicBuffer<Renter>();
                    DynamicBuffer<InstalledUpgrade> dynamicBuffer2 = bufferAccessor3.Length != 0 ? bufferAccessor3[index] : new DynamicBuffer<InstalledUpgrade>();
                    int num1 = flag1 ? 1 : 0;
                    int num2 = flag2 ? 1 : 0;
                    int num3 = flag3 ? 1 : 0;
                    double efficiency2 = (double)efficiency1;
                    DynamicBuffer<Renter> renters = dynamicBuffer1;
                    DynamicBuffer<InstalledUpgrade> installedUpgrades = dynamicBuffer2;
                    PollutionParameterData pollutionParameters = this.m_PollutionParameters;
                    DynamicBuffer<CityModifier> cityModifiers = cityModifier;
                    ref ComponentLookup<PrefabRef> local1 = ref this.m_Prefabs;
                    ref ComponentLookup<Game.Prefabs.BuildingData> local2 = ref this.m_BuildingDatas;
                    ref ComponentLookup<SpawnableBuildingData> local3 = ref this.m_SpawnableDatas;
                    ref ComponentLookup<PollutionData> local4 = ref this.m_PollutionDatas;
                    ref ComponentLookup<PollutionModifierData> local5 = ref this.m_PollutionModifierDatas;
                    ref ComponentLookup<ZoneData> local6 = ref this.m_ZoneDatas;
                    ref BufferLookup<Employee> local7 = ref this.m_Employees;
                    ref BufferLookup<HouseholdCitizen> local8 = ref this.m_HouseholdCitizens;
                    ref ComponentLookup<Citizen> local9 = ref this.m_Citizens;

                    PollutionData buildingPollution = RWHBuildingPollutionAddSystem.GetBuildingPollution(prefab, num1 != 0, num2 != 0, num3 != 0, (float)efficiency2, renters, installedUpgrades, pollutionParameters, cityModifiers, ref local1, ref local2, ref local3, ref local4, ref local5, ref local6, ref local7, ref local8, ref local9);
                    if ((double)buildingPollution.m_GroundPollution > 0.0)
                    {
                        this.m_GroundPollutionQueue.Enqueue(new RWHBuildingPollutionAddSystem.PollutionItem()
                        {
                            amount = (int)buildingPollution.m_GroundPollution,
                            position = position.xz
                        });
                    }
                    if ((double)buildingPollution.m_AirPollution > 0.0)
                    {
                        this.m_AirPollutionQueue.Enqueue(new RWHBuildingPollutionAddSystem.PollutionItem()
                        {
                            amount = (int)buildingPollution.m_AirPollution,
                            position = position.xz
                        });
                    }
                    if ((double)buildingPollution.m_NoisePollution > 0.0)
                    {
                        this.m_NoisePollutionQueue.Enqueue(new RWHBuildingPollutionAddSystem.PollutionItem()
                        {
                            amount = (int)buildingPollution.m_NoisePollution,
                            position = position.xz
                        });
                    }
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

        private struct TypeHandle
        {
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(true);
                this.__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(true);
                this.__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(true);
                this.__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(true);
                this.__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(true);
                this.__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
                this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.BuildingData>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(true);
                this.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(true);
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
            }
        }
    }
}
