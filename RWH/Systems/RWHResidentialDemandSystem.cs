// Decompiled with JetBrains decompiler
// Type: Game.Simulation.RWHResidentialDemandSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 407A0A0B-0B48-4732-BD57-8ACD1ADF3D7C
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Companies;
using Game.Debug;
using Game.Prefabs;
using Game.Reflection;
using Game.Triggers;
using Game;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Colossal.PSI.Common;
using RealisticWorkplacesAndHouseholds;

#nullable disable
namespace RWH.Systems
{
    //[CompilerGenerated]
    public partial class RWHResidentialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
    {
        public static readonly int kMaxFactorEffect = 15;
        public static readonly int kMaxMovingInHouseholdAmount = 500;
        private TaxSystem m_TaxSystem;
        private CountStudyPositionsSystem m_CountStudyPositionsSystem;
        private CountWorkplacesSystem m_CountWorkplacesSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountResidentialPropertySystem m_CountResidentialPropertySystem;
        private CitySystem m_CitySystem;
        private TriggerSystem m_TriggerSystem;
        private EntityQuery m_DemandParameterGroup;
        private EntityQuery m_UnlockedZoneQuery;
        [DebugWatchValue(color = "#27ae60")]
        private NativeValue<int> m_HouseholdDemand;
        [DebugWatchValue(color = "#117a65")]
        private NativeValue<int3> m_BuildingDemand;
        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_LowDemandFactors;
        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_MediumDemandFactors;
        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_HighDemandFactors;
        [DebugWatchDeps]
        private JobHandle m_WriteDependencies;
        private JobHandle m_ReadDependencies;
        private int m_LastHouseholdDemand;
        private int3 m_LastBuildingDemand;
        private RWHResidentialDemandSystem.TypeHandle __TypeHandle;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        public override int GetUpdateOffset(SystemUpdatePhase phase) => 10;

        public int householdDemand => this.m_LastHouseholdDemand;

        public int3 buildingDemand => this.m_LastBuildingDemand;

        public NativeArray<int> GetLowDensityDemandFactors(out JobHandle deps)
        {
            // ISSUE: reference to a compiler-generated field
            deps = this.m_WriteDependencies;
            // ISSUE: reference to a compiler-generated field
            return this.m_LowDemandFactors;
        }

        public NativeArray<int> GetMediumDensityDemandFactors(out JobHandle deps)
        {
            // ISSUE: reference to a compiler-generated field
            deps = this.m_WriteDependencies;
            // ISSUE: reference to a compiler-generated field
            return this.m_MediumDemandFactors;
        }

        public NativeArray<int> GetHighDensityDemandFactors(out JobHandle deps)
        {
            // ISSUE: reference to a compiler-generated field
            deps = this.m_WriteDependencies;
            // ISSUE: reference to a compiler-generated field
            return this.m_HighDemandFactors;
        }

        public void AddReader(JobHandle reader)
        {
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_ReadDependencies = JobHandle.CombineDependencies(this.m_ReadDependencies, reader);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            // ISSUE: reference to a compiler-generated field
            this.m_DemandParameterGroup = this.GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            // ISSUE: reference to a compiler-generated field
            this.m_UnlockedZoneQuery = this.GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<ZonePropertiesData>(), ComponentType.Exclude<Locked>());
            // ISSUE: reference to a compiler-generated field
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_TaxSystem = this.World.GetOrCreateSystemManaged<TaxSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_CountStudyPositionsSystem = this.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_CountWorkplacesSystem = this.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_CountHouseholdDataSystem = this.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_CountResidentialPropertySystem = this.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_TriggerSystem = this.World.GetOrCreateSystemManaged<TriggerSystem>();
            // ISSUE: reference to a compiler-generated field
            this.m_HouseholdDemand = new NativeValue<int>(Allocator.Persistent);
            // ISSUE: reference to a compiler-generated field
            this.m_BuildingDemand = new NativeValue<int3>(Allocator.Persistent);
            // ISSUE: reference to a compiler-generated field
            this.m_LowDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            // ISSUE: reference to a compiler-generated field
            this.m_MediumDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            // ISSUE: reference to a compiler-generated field
            this.m_HighDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnDestroy()
        {
            // ISSUE: reference to a compiler-generated field
            this.m_HouseholdDemand.Dispose();
            // ISSUE: reference to a compiler-generated field
            this.m_BuildingDemand.Dispose();
            // ISSUE: reference to a compiler-generated field
            this.m_LowDemandFactors.Dispose();
            // ISSUE: reference to a compiler-generated field
            this.m_MediumDemandFactors.Dispose();
            // ISSUE: reference to a compiler-generated field
            this.m_HighDemandFactors.Dispose();
            base.OnDestroy();
        }

        public void SetDefaults(Colossal.Serialization.Entities.Context context)
        {
            // ISSUE: reference to a compiler-generated field
            this.m_HouseholdDemand.value = 0;
            // ISSUE: reference to a compiler-generated field
            this.m_BuildingDemand.value = new int3();
            // ISSUE: reference to a compiler-generated field
            this.m_LowDemandFactors.Fill<int>(0);
            // ISSUE: reference to a compiler-generated field
            this.m_MediumDemandFactors.Fill<int>(0);
            // ISSUE: reference to a compiler-generated field
            this.m_HighDemandFactors.Fill<int>(0);
            // ISSUE: reference to a compiler-generated field
            this.m_LastHouseholdDemand = 0;
            // ISSUE: reference to a compiler-generated field
            this.m_LastBuildingDemand = new int3();
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_HouseholdDemand.value);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_BuildingDemand.value);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_LowDemandFactors.Length);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_LowDemandFactors);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_MediumDemandFactors);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_HighDemandFactors);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_LastHouseholdDemand);
            // ISSUE: reference to a compiler-generated field
            writer.Write(this.m_LastBuildingDemand);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            int num1;
            reader.Read(out num1);
            // ISSUE: reference to a compiler-generated field
            this.m_HouseholdDemand.value = num1;
            if (reader.context.version < Version.residentialDemandSplit)
            {
                int num2;
                reader.Read(out num2);
                // ISSUE: reference to a compiler-generated field
                this.m_BuildingDemand.value = new int3(num2 / 3, num2 / 3, num2 / 3);
            }
            else
            {
                int3 int3;
                reader.Read(out int3);
                // ISSUE: reference to a compiler-generated field
                this.m_BuildingDemand.value = int3;
            }
            if (reader.context.version < Version.demandFactorCountSerialization)
            {
                NativeArray<int> src = new NativeArray<int>(13, Allocator.Temp);
                reader.Read(src);
                // ISSUE: reference to a compiler-generated field
                CollectionUtils.CopySafe<int>(src, this.m_LowDemandFactors);
                src.Dispose();
            }
            else
            {
                int length;
                reader.Read(out length);
                // ISSUE: reference to a compiler-generated field
                if (length == this.m_LowDemandFactors.Length)
                {
                    // ISSUE: reference to a compiler-generated field
                    reader.Read(this.m_LowDemandFactors);
                    // ISSUE: reference to a compiler-generated field
                    reader.Read(this.m_MediumDemandFactors);
                    // ISSUE: reference to a compiler-generated field
                    reader.Read(this.m_HighDemandFactors);
                }
                else
                {
                    NativeArray<int> src = new NativeArray<int>(length, Allocator.Temp);
                    reader.Read(src);
                    // ISSUE: reference to a compiler-generated field
                    CollectionUtils.CopySafe<int>(src, this.m_LowDemandFactors);
                    reader.Read(src);
                    // ISSUE: reference to a compiler-generated field
                    CollectionUtils.CopySafe<int>(src, this.m_MediumDemandFactors);
                    reader.Read(src);
                    // ISSUE: reference to a compiler-generated field
                    CollectionUtils.CopySafe<int>(src, this.m_HighDemandFactors);
                    src.Dispose();
                }
            }
            // ISSUE: reference to a compiler-generated field
            reader.Read(out this.m_LastHouseholdDemand);
            if (reader.context.version < Version.residentialDemandSplit)
            {
                int num3;
                reader.Read(out num3);
                // ISSUE: reference to a compiler-generated field
                this.m_LastBuildingDemand = new int3(num3 / 3, num3 / 3, num3 / 3);
            }
            else
            {
                // ISSUE: reference to a compiler-generated field
                reader.Read(out this.m_LastBuildingDemand);
            }
        }

        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            // ISSUE: reference to a compiler-generated field
            if (this.m_DemandParameterGroup.IsEmptyIgnoreFilter)
                return;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_LastHouseholdDemand = this.m_HouseholdDemand.value;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.m_LastBuildingDemand = this.m_BuildingDemand.value;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            this.__TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            JobHandle outJobHandle;
            JobHandle deps;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            // ISSUE: object of a compiler-generated type is created
            // ISSUE: variable of a compiler-generated type
            RWHResidentialDemandSystem.UpdateResidentialDemandJob jobData = new RWHResidentialDemandSystem.UpdateResidentialDemandJob()
            {
                m_UnlockedZones = this.m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_Populations = this.__TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_DemandParameters = this.m_DemandParameterGroup.ToComponentDataListAsync<DemandParameterData>((AllocatorManager.AllocatorHandle)this.World.UpdateAllocator.ToAllocator, out outJobHandle),
                m_StudyPositions = this.m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out deps),
                m_FreeWorkplaces = this.m_CountWorkplacesSystem.GetFreeWorkplaces(),
                m_TotalWorkplaces = this.m_CountWorkplacesSystem.GetTotalWorkplaces(),
                m_HouseholdCountData = this.m_CountHouseholdDataSystem.GetHouseholdCountData(),
                m_ResidentialPropertyData = this.m_CountResidentialPropertySystem.GetResidentialPropertyData(),
                m_TaxRates = this.m_TaxSystem.GetTaxRates(),
                m_City = this.m_CitySystem.City,
                m_HouseholdDemand = this.m_HouseholdDemand,
                m_BuildingDemand = this.m_BuildingDemand,
                m_LowDemandFactors = this.m_LowDemandFactors,
                m_MediumDemandFactors = this.m_MediumDemandFactors,
                m_HighDemandFactors = this.m_HighDemandFactors,
                m_UnemploymentRate = this.m_CountHouseholdDataSystem.UnemploymentRate,
                m_TriggerQueue = this.m_TriggerSystem.CreateActionBuffer()
            };
            // ISSUE: reference to a compiler-generated field
            this.Dependency = jobData.Schedule<RWHResidentialDemandSystem.UpdateResidentialDemandJob>(JobUtils.CombineDependencies(this.Dependency, this.m_ReadDependencies, outJobHandle, deps));
            // ISSUE: reference to a compiler-generated field
            this.m_WriteDependencies = this.Dependency;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_CountStudyPositionsSystem.AddReader(this.Dependency);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_TaxSystem.AddReader(this.Dependency);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.m_TriggerSystem.AddActionBufferWriter(this.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            // ISSUE: reference to a compiler-generated method
            this.__AssignQueries(ref this.CheckedStateRef);
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public RWHResidentialDemandSystem()
        {
        }

        //[BurstCompile]
        private struct UpdateResidentialDemandJob : IJob
        {
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<ZonePropertiesData> m_UnlockedZones;
            [ReadOnly]
            public ComponentLookup<Population> m_Populations;
            [ReadOnly]
            public NativeList<DemandParameterData> m_DemandParameters;
            [ReadOnly]
            public NativeArray<int> m_StudyPositions;
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            [ReadOnly]
            public float m_UnemploymentRate;
            public Entity m_City;
            public NativeValue<int> m_HouseholdDemand;
            public NativeValue<int3> m_BuildingDemand;
            public NativeArray<int> m_LowDemandFactors;
            public NativeArray<int> m_MediumDemandFactors;
            public NativeArray<int> m_HighDemandFactors;
            public CountHouseholdDataSystem.HouseholdData m_HouseholdCountData;
            public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;
            public Workplaces m_FreeWorkplaces;
            public Workplaces m_TotalWorkplaces;
            public NativeQueue<TriggerAction> m_TriggerQueue;

            public void Execute()
            {
                bool3 c = new bool3();
                // ISSUE: reference to a compiler-generated field
                for (int index = 0; index < this.m_UnlockedZones.Length; ++index)
                {
                    // ISSUE: reference to a compiler-generated field
                    if ((double)this.m_UnlockedZones[index].m_ResidentialProperties > 0.0)
                    {
                        // ISSUE: reference to a compiler-generated field
                        // ISSUE: reference to a compiler-generated field
                        float num = this.m_UnlockedZones[index].m_ResidentialProperties / this.m_UnlockedZones[index].m_SpaceMultiplier;
                        // ISSUE: reference to a compiler-generated field
                        if (!this.m_UnlockedZones[index].m_ScaleResidentials)
                            c.x = true;
                        else if ((double)num < 1.0)
                            c.y = true;
                        else
                            c.z = true;
                    }
                }
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                int3 freeProperties = this.m_ResidentialPropertyData.m_FreeProperties;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                int3 totalProperties = this.m_ResidentialPropertyData.m_TotalProperties;
                // ISSUE: reference to a compiler-generated field
                DemandParameterData demandParameter = this.m_DemandParameters[0];
                int num1 = 0;
                for (int index = 1; index <= 4; ++index)
                {
                    // ISSUE: reference to a compiler-generated field
                    num1 += this.m_StudyPositions[index];
                }
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                Population population = this.m_Populations[this.m_City];
                float num2 = 20f - math.smoothstep(0.0f, 20f, (float)population.m_Population / 20000f);
                int num3 = math.max(demandParameter.m_MinimumHappiness, population.m_AverageHappiness);
                float num4 = 0.0f;
                for (int jobLevel = 0; jobLevel < 5; ++jobLevel)
                {
                    // ISSUE: reference to a compiler-generated field
                    // ISSUE: reference to a compiler-generated method
                    num4 += (float)-(TaxSystem.GetResidentialTaxRate(jobLevel, this.m_TaxRates) - 10);
                }
                float f1 = demandParameter.m_TaxEffect * (num4 / 5f);
                float f2 = demandParameter.m_HappinessEffect * (float)(num3 - demandParameter.m_NeutralHappiness);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                float num5 = math.min((float)(-(double)demandParameter.m_HomelessEffect * (100.0 * (double)this.m_HouseholdCountData.m_HomelessHouseholdCount / (1.0 + (double)this.m_HouseholdCountData.m_MovedInHouseholdCount) - (double)demandParameter.m_NeutralHomelessness)), (float)RWHResidentialDemandSystem.kMaxFactorEffect);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                float f3 = math.clamp(demandParameter.m_HomelessEffect * ((float)(100.0 * (double)this.m_HouseholdCountData.m_HomelessHouseholdCount / (1.0 + (double)this.m_HouseholdCountData.m_MovedInHouseholdCount)) - demandParameter.m_NeutralHomelessness), 0.0f, (float)RWHResidentialDemandSystem.kMaxFactorEffect);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                float num6 = math.clamp(demandParameter.m_AvailableWorkplaceEffect * ((float)this.m_FreeWorkplaces.SimpleWorkplacesCount - (float)((double)this.m_TotalWorkplaces.SimpleWorkplacesCount * (double)demandParameter.m_NeutralAvailableWorkplacePercentage / 100.0)), 0.0f, 40f);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                float y = math.clamp(demandParameter.m_AvailableWorkplaceEffect * ((float)this.m_FreeWorkplaces.ComplexWorkplacesCount - (float)((double)this.m_TotalWorkplaces.ComplexWorkplacesCount * (double)demandParameter.m_NeutralAvailableWorkplacePercentage / 100.0)), 0.0f, 20f);
                float f4 = demandParameter.m_StudentEffect * math.clamp((float)num1 / 200f, 0.0f, 20f);
                // ISSUE: reference to a compiler-generated field
                float f5 = demandParameter.m_NeutralUnemployment - this.m_UnemploymentRate;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                this.m_HouseholdDemand.value = this.m_HouseholdCountData.m_MovingInHouseholdCount <= RWHResidentialDemandSystem.kMaxMovingInHouseholdAmount ? math.min(200, (int)((double)num2 + (double)f2 + (double)num5 + (double)f1 + (double)f5 + (double)f4 + (double)math.max(num6, y))) : 0;
                int num7 = Mathf.RoundToInt(100f * (float)(demandParameter.m_FreeResidentialRequirement.x - freeProperties.x) / (float)demandParameter.m_FreeResidentialRequirement.x);
                int num8 = Mathf.RoundToInt(100f * (float)(demandParameter.m_FreeResidentialRequirement.y - freeProperties.y) / (float)demandParameter.m_FreeResidentialRequirement.y);
                int num9 = Mathf.RoundToInt(100f * (float)(demandParameter.m_FreeResidentialRequirement.z - freeProperties.z) / (float)demandParameter.m_FreeResidentialRequirement.z);
                // ISSUE: reference to a compiler-generated field
                RealisticWorkplacesAndHouseholds.Mod.log.Info($"m_FreeResidentialRequirement:{demandParameter.m_FreeResidentialRequirement},freeProperties:{freeProperties},num2:{num2},num3:{num3},num4:{num4},f1:{f1},f2:{f2},f3:{f3},num6:{num6},y:{y},f4:{f4},f5:{f5},num7:{num7},num8:{num8},num9:{num9}");
                this.m_LowDemandFactors[7] = Mathf.RoundToInt(f2);
                // ISSUE: reference to a compiler-generated field
                this.m_LowDemandFactors[6] = Mathf.RoundToInt(num6) / 2;
                // ISSUE: reference to a compiler-generated field
                this.m_LowDemandFactors[5] = Mathf.RoundToInt(f5);
                // ISSUE: reference to a compiler-generated field
                this.m_LowDemandFactors[11] = Mathf.RoundToInt(f1);
                // ISSUE: reference to a compiler-generated field
                this.m_LowDemandFactors[13] = num7;
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[7] = Mathf.RoundToInt(f2);
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[6] = Mathf.RoundToInt(num6);
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[5] = Mathf.RoundToInt(f5);
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[11] = Mathf.RoundToInt(f1);
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[12] = Mathf.RoundToInt(f4);
                // ISSUE: reference to a compiler-generated field
                this.m_MediumDemandFactors[13] = num8;
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[7] = Mathf.RoundToInt(f2);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[8] = Mathf.RoundToInt(f3);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[6] = Mathf.RoundToInt(num6);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[5] = Mathf.RoundToInt(f5);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[11] = Mathf.RoundToInt(f1);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[12] = Mathf.RoundToInt(f4);
                // ISSUE: reference to a compiler-generated field
                this.m_HighDemandFactors[13] = num9;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                int num10 = this.m_LowDemandFactors[13] >= 0 ? this.m_LowDemandFactors[7] + this.m_LowDemandFactors[11] + this.m_LowDemandFactors[6] + this.m_LowDemandFactors[5] + this.m_LowDemandFactors[13] : 0;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                int num11 = this.m_MediumDemandFactors[13] >= 0 ? this.m_MediumDemandFactors[7] + this.m_MediumDemandFactors[11] + this.m_MediumDemandFactors[6] + this.m_MediumDemandFactors[12] + this.m_MediumDemandFactors[5] + this.m_MediumDemandFactors[13] : 0;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                int num12 = this.m_HighDemandFactors[13] >= 0 ? this.m_HighDemandFactors[7] + this.m_HighDemandFactors[8] + this.m_HighDemandFactors[11] + this.m_HighDemandFactors[6] + this.m_HighDemandFactors[12] + this.m_HighDemandFactors[5] + this.m_HighDemandFactors[13] : 0;
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                this.m_BuildingDemand.value = new int3(math.clamp(this.m_HouseholdDemand.value / 2 + num7 + num10, 0, 100), math.clamp(this.m_HouseholdDemand.value / 2 + num8 + num11, 0, 100), math.clamp(this.m_HouseholdDemand.value / 2 + num9 + num12, 0, 100));
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                this.m_BuildingDemand.value = math.select(new int3(), this.m_BuildingDemand.value, c);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                this.m_TriggerQueue.Enqueue(new TriggerAction(TriggerType.ResidentialDemand, Entity.Null, totalProperties.x + totalProperties.y + totalProperties.z > 100 ? (float)(this.m_BuildingDemand.value.x + this.m_BuildingDemand.value.y + this.m_BuildingDemand.value.z) / 100f : 0.0f));
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                // ISSUE: reference to a compiler-generated field
                this.__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
            }
        }
    }
}
