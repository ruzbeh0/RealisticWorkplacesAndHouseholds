using Colossal.Entities;
using Game;
using Game.Simulation;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities.Internal;
using RealisticWorkplacesAndHouseholds;

#nullable disable

namespace RWH.Systems
{
    //[CompilerGenerated]
    public partial class RWHHouseholdSpawnSystem : GameSystemBase
    {
        private EntityQuery m_HouseholdPrefabQuery;
        private EntityQuery m_OutsideConnectionQuery;
        private EntityQuery m_DemandParameterQuery;
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private ResidentialVacancySystem m_ResidentialVacancySystem;
        private EndFrameBarrier m_EndFrameBarrier;
        private SimulationSystem m_SimulationSystem;
        private CountStudyPositionsSystem m_CountStudyPositionsSystem;
        private CitySystem m_CitySystem;
        private RWHHouseholdSpawnSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ResidentialDemandSystem = this.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            this.m_ResidentialVacancySystem = this.World.GetOrCreateSystemManaged<ResidentialVacancySystem>();
            this.m_EndFrameBarrier = this.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_SimulationSystem = this.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_CountStudyPositionsSystem = this.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
            this.m_HouseholdPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Prefabs.ArchetypeData>(), ComponentType.ReadOnly<Game.Prefabs.HouseholdData>(), ComponentType.Exclude<DynamicHousehold>());
            this.m_OutsideConnectionQuery = this.GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            this.m_DemandParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            this.RequireForUpdate(this.m_HouseholdPrefabQuery);
            this.RequireForUpdate(this.m_OutsideConnectionQuery);

            Mod.log.Info("RWHHouseholdSpawnSystem: OnCreate");
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            JobHandle jobHandle = this.Dependency;
            int householdDemand = this.m_ResidentialDemandSystem.householdDemand;
            //Mod.log.Info($"RWHHouseholdSpawnSystem: householdDemand={householdDemand}, vacancy_rate={m_ResidentialVacancySystem.vacancy_rate}, residential_vacancy_rate={Mod.m_Setting.residential_vacancy_rate}");
            if (householdDemand > 0 || m_ResidentialVacancySystem.vacancy_rate > Mod.m_Setting.residential_vacancy_rate/100f)
            {
                if (householdDemand <= 0)
                {
                    // targetVacancy and actualVacancy in [0..1]
                    float target = math.saturate(Mod.m_Setting.residential_vacancy_rate / 100f);
                    float actual = math.max(0.001f, m_ResidentialVacancySystem.vacancy_rate);

                    // spawn proportional to how far below target we are
                    float ratio = math.saturate((target - actual) / target); // 0..1
                    householdDemand = (int)math.round(100f * ratio);
                }

                

                JobHandle deps1;
                NativeArray<int> densityDemandFactors1 = this.m_ResidentialDemandSystem.GetLowDensityDemandFactors(out deps1);
                JobHandle deps2;
                NativeArray<int> densityDemandFactors2 = this.m_ResidentialDemandSystem.GetMediumDensityDemandFactors(out deps2);
                JobHandle deps3;
                NativeArray<int> densityDemandFactors3 = this.m_ResidentialDemandSystem.GetHighDensityDemandFactors(out deps3);

                JobHandle outJobHandle1;
                JobHandle outJobHandle2;
                JobHandle outJobHandle3;
                JobHandle deps4;

                jobHandle = new RWHHouseholdSpawnSystem.SpawnHouseholdJob()
                {
                    m_PrefabEntities = this.m_HouseholdPrefabQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle1),
                    m_Archetypes = this.m_HouseholdPrefabQuery.ToComponentDataListAsync<Game.Prefabs.ArchetypeData>((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle2),
                    m_OutsideConnectionEntities = this.m_OutsideConnectionQuery.ToEntityListAsync((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle3),
                    m_HouseholdDatas = InternalCompilerInterface.GetComponentLookup<Game.Prefabs.HouseholdData>(ref this.__TypeHandle.__Game_Prefabs_HouseholdData_RO_ComponentLookup, ref this.CheckedStateRef),
                    m_Dynamics = InternalCompilerInterface.GetComponentLookup<DynamicHousehold>(ref this.__TypeHandle.__Game_Prefabs_DynamicHousehold_RO_ComponentLookup, ref this.CheckedStateRef),
                    m_Populations = InternalCompilerInterface.GetComponentLookup<Population>(ref this.__TypeHandle.__Game_City_Population_RO_ComponentLookup, ref this.CheckedStateRef),
                    m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup<OutsideConnectionData>(ref this.__TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref this.CheckedStateRef),
                    m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref this.CheckedStateRef),
                    m_DemandParameterData = this.m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                    m_LowFactors = densityDemandFactors1,
                    m_MedFactors = densityDemandFactors2,
                    m_HiFactors = densityDemandFactors3,
                    m_StudyPositions = this.m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out deps4),
                    m_City = this.m_CitySystem.City,
                    m_Demand = 4*householdDemand,
                    m_Random = RandomSeed.Next().GetRandom((int)this.m_SimulationSystem.frameIndex),
                    hh_speed_rate = Mod.m_Setting.hh_spawn_speed_rate,
                    m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer()
                }.Schedule<RWHHouseholdSpawnSystem.SpawnHouseholdJob>(JobUtils.CombineDependencies(outJobHandle1, outJobHandle2, jobHandle, outJobHandle3, deps1, deps2, deps3, deps4));

                this.m_ResidentialDemandSystem.AddReader(jobHandle);
                this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            }
            this.Dependency = jobHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            new EntityQueryBuilder((AllocatorManager.AllocatorHandle)Allocator.Temp).Dispose();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public RWHHouseholdSpawnSystem()
        {
        }

        [BurstCompile]
        private struct SpawnHouseholdJob : IJob
        {
            [ReadOnly]
            public NativeList<Entity> m_PrefabEntities;
            [ReadOnly]
            public NativeList<Game.Prefabs.ArchetypeData> m_Archetypes;
            [ReadOnly]
            public NativeList<Entity> m_OutsideConnectionEntities;
            [ReadOnly]
            public ComponentLookup<Population> m_Populations;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.HouseholdData> m_HouseholdDatas;
            [ReadOnly]
            public ComponentLookup<DynamicHousehold> m_Dynamics;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefs;
            [ReadOnly]
            public DemandParameterData m_DemandParameterData;
            public Entity m_City;
            public EntityCommandBuffer m_CommandBuffer;
            public int m_Demand;
            public Unity.Mathematics.Random m_Random;
            [ReadOnly]
            public NativeArray<int> m_LowFactors;
            [ReadOnly]
            public NativeArray<int> m_MedFactors;
            [ReadOnly]
            public NativeArray<int> m_HiFactors;
            [ReadOnly]
            public NativeArray<int> m_StudyPositions;
            public float hh_speed_rate;

            private bool IsValidStudyPrefab(Entity householdPrefab)
            {
                Game.Prefabs.HouseholdData householdData = this.m_HouseholdDatas[householdPrefab];
                if ((this.m_StudyPositions[1] + this.m_StudyPositions[2] <= 0 ? 0 : (this.m_Random.NextBool() ? 1 : 0)) == 0 && this.m_StudyPositions[3] + this.m_StudyPositions[4] > 0)
                    return householdData.m_StudentCount > 0;
                return this.m_StudyPositions[1] + this.m_StudyPositions[2] > 0 && householdData.m_ChildCount > 0;
            }

            public void Execute()
            {
                int max1 = Mathf.RoundToInt(300f / math.clamp((hh_speed_rate) * math.log((float)(1.0 + 1.0 / 1000.0 * (double)this.m_Populations[this.m_City].m_Population)), 0.5f, 20f));
                //int max1 = 600;
                int num1 = this.m_Random.NextInt(max1);
                int num2 = 0;
                for (; num1 < this.m_Demand; num1 = this.m_Random.NextInt(max1))
                {
                    ++num2;
                    this.m_Demand -= num1;
                }
                if (num2 == 0)
                    return;
                //num2 = num2 * 2;
                int y1 = this.m_LowFactors[6] + this.m_MedFactors[6] + this.m_HiFactors[6];
                int y2 = this.m_LowFactors[12] + this.m_MedFactors[12] + this.m_HiFactors[12];
                int num3 = math.max(0, y1);
                int num4 = math.max(0, y2);
                float num5 = (float)num4 / (float)(num4 + num3);

                //Mod.log.Info($"SpawnHouseholdJob: num2={num2}, num3={num3}, num4={num4}, num5={num5}, max1={max1}");
                for (int index1 = 0; index1 < num2; ++index1)
                {
                    int max2 = 0;
                    bool flag = (double)this.m_Random.NextFloat() < (double)num5;
                    for (int index2 = 0; index2 < this.m_PrefabEntities.Length; ++index2)
                    {
                        if (this.IsValidStudyPrefab(this.m_PrefabEntities[index2]) == flag)
                        {
                            max2 += this.m_HouseholdDatas[this.m_PrefabEntities[index2]].m_Weight;
                        }
                    }
                    int num6 = this.m_Random.NextInt(max2);
                    int index3 = 0;
                    for (int index4 = 0; index4 < this.m_PrefabEntities.Length; ++index4)
                    {
                        if (this.IsValidStudyPrefab(this.m_PrefabEntities[index4]) == flag)
                        {
                            num6 -= this.m_HouseholdDatas[this.m_PrefabEntities[index4]].m_Weight;
                        }
                        if (num6 < 0)
                        {
                            index3 = index4;
                            break;
                        }
                    }
                    Entity prefabEntity = this.m_PrefabEntities[index3];
                    Entity entity = this.m_CommandBuffer.CreateEntity(this.m_Archetypes[index3].m_Archetype);
                    this.m_CommandBuffer.SetComponent<PrefabRef>(entity, new PrefabRef()
                    {
                        m_Prefab = prefabEntity
                    });
                    Entity result;

                    if (this.m_OutsideConnectionEntities.Length > 0 && Game.Buildings.BuildingUtils.GetRandomOutsideConnectionByParameters(ref this.m_OutsideConnectionEntities, ref this.m_OutsideConnectionDatas, ref this.m_PrefabRefs, this.m_Random, this.m_DemandParameterData.m_CitizenOCSpawnParameters, out result))
                    {
                        CurrentBuilding component = new CurrentBuilding()
                        {
                            m_CurrentBuilding = result
                        };
                        this.m_CommandBuffer.AddComponent<CurrentBuilding>(entity, component);
                        //Mod.log.Info($"SpawnHouseholdJob: Spawning household with prefab {prefabEntity} in building {result}");
                    }
                    else
                    {
                        this.m_CommandBuffer.AddComponent<Deleted>(entity, new Deleted());
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.HouseholdData> __Game_Prefabs_HouseholdData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<DynamicHousehold> __Game_Prefabs_DynamicHousehold_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Prefabs_HouseholdData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.HouseholdData>(true);
                this.__Game_Prefabs_DynamicHousehold_RO_ComponentLookup = state.GetComponentLookup<DynamicHousehold>(true);
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
                this.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
            }
        }
    }
}
