using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class HouseholdUpdateSystem : GameSystemBase
    {
        private EntityQuery m_UpdateHouseholdJobQuery;
        private EntityQuery m_ResetHouseholdJobQuery;
        private EntityQuery m_AssetPackQuery;
        private PrefabSystem _prefabSystem;
        private NativeParallelHashMap<Entity, FixedString64Bytes> _prefabNames;
        private NativeParallelHashMap<Entity, float3> _assetPackFactors;

        private bool m_TriggerInitialHouseholdUpdate = false;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Normal query — excludes already processed buildings
            UpdateHouseholdJobQuery updateQuery = new();
            m_UpdateHouseholdJobQuery = GetEntityQuery(updateQuery.Query);

            // Reset query — includes all buildings
            EntityQueryDesc resetQuery = new()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<BuildingData>(),
                    ComponentType.ReadOnly<BuildingPropertyData>(),
                    ComponentType.ReadOnly<GroupAmbienceData>(),
                    ComponentType.ReadWrite<SpawnableBuildingData>(),
                    ComponentType.ReadWrite<SubMesh>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_ResetHouseholdJobQuery = GetEntityQuery(resetQuery);

            EntityQueryDesc query = new()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<AssetPackData>(),
                    ComponentType.ReadOnly<PrefabData>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            };
            m_AssetPackQuery = GetEntityQuery(query);

            this.RequireForUpdate(m_UpdateHouseholdJobQuery);

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            _prefabNames = new NativeParallelHashMap<Entity, FixedString64Bytes>(1024, Allocator.Persistent);
            _assetPackFactors = new NativeParallelHashMap<Entity, float3>(1024, Allocator.Persistent);
        }

        private float3 GetFactorsForPackName(string name)
        {
            // default: no change
            float low = 1f;
            float rowhomes = 1f;
            float medHigh = 1f;

            // normalize for simple comparisons
            string n = name.ToLowerInvariant();
            int index = -1;

            // NOTE: adjust the substring checks if your prefab names differ.
            try
            {
                if (n.Contains("uk"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.UK, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("de"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.Germany, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("nl"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.Netherlands, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("fr"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.France, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("jp"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.Japan, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("cn"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.China, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("ee"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.EasterEurope, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("ussw"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.USSW, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("usne"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.USNE, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("mediterraneanheritage"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.Mediterranean, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("dragongate"))
                {
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.DragonGate, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
                else if (n.Contains("skyscrapers"))
                {
                    Mod.log.Info($"Detected Skyscrapers Pack in asset pack name '{n}'");
                    Mod.m_Setting.packIndexLookup.TryGetValue((int)Setting.PacksEnum.Skyscrapers, out index);
                    low = Mod.m_Setting.pack_low_density_factor_[index];
                    rowhomes = Mod.m_Setting.pack_row_homes_factor_[index];
                    medHigh = Mod.m_Setting.pack_medhigh_density_factor_[index];
                }
            }
            catch (Exception e)
            {
                Mod.log.Error($"Error processing asset pack name '{n}', index:{index}: {e}");
            }
            
            return new float3(low, rowhomes, medHigh);
        }


        private void BuildPrefabMaps(EntityQuery query)
        {
            _prefabNames.Clear();
            _assetPackFactors.Clear();

            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity pack = entities[i];
                FixedString64Bytes fs = _prefabSystem.GetPrefabName(pack);
                _prefabNames.TryAdd(pack, fs);

                // Optional: log once to discover actual names
                // Mod.log.Info($"Asset pack prefab: {fs}");

                float3 factors = GetFactorsForPackName(fs.ToString());
                _assetPackFactors.TryAdd(pack, factors);

                Mod.log.Info(
                    $"Asset Pack:{fs.ToString()} - Factors: LowDensity={factors.x}, RowHomes={factors.y}, MedHigh={factors.z}");
            }
        }


        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game && purpose == Colossal.Serialization.Entities.Purpose.LoadGame)
            {
                UpdateHouseholds(reset: true);
                m_TriggerInitialHouseholdUpdate = false;
            } else
            {
                m_TriggerInitialHouseholdUpdate = true;
            }  
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if(_prefabNames.IsEmpty)
            {
                Mod.log.Info("Building Prefab Name Map");
                BuildPrefabMaps(query: m_AssetPackQuery);
            }
            //Mod.log.Info($"OnUpdate: {m_TriggerInitialHouseholdUpdate}");
            if (m_TriggerInitialHouseholdUpdate)
            {
                UpdateHouseholds(reset: true);
                m_TriggerInitialHouseholdUpdate = false;
            }
            else
            {
                UpdateHouseholds(reset: false);
            }
        }

        private void UpdateHouseholds(bool reset)
        {
            var query = reset ? m_ResetHouseholdJobQuery : m_UpdateHouseholdJobQuery;

            if (reset)
                query.ResetFilter();
            else
                query.SetChangedVersionFilter(typeof(BuildingPropertyData));

            var commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            UpdateHouseholdJob job = new UpdateHouseholdJob
            {
                ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                BuildingDataHandle = SystemAPI.GetComponentTypeHandle<BuildingData>(true),
                meshDataLookup = SystemAPI.GetComponentLookup<MeshData>(true),
                SubMeshHandle = SystemAPI.GetBufferTypeHandle<SubMesh>(true),
                SpawnableBuildingHandle = SystemAPI.GetComponentTypeHandle<SpawnableBuildingData>(true),
                AssetPackElementBufferLookup = SystemAPI.GetBufferLookup<AssetPackElement>(true),
                AssetPackDataLookup = SystemAPI.GetComponentLookup<AssetPackData>(true),
                PrefabDataLookup = SystemAPI.GetComponentLookup<PrefabData>(true),
                BuildingPropertyDataHandle = SystemAPI.GetComponentTypeHandle<BuildingPropertyData>(false),
                ZoneDataLookup = SystemAPI.GetComponentLookup<ZoneData>(true),
                GroupAmbienceDataHandle = SystemAPI.GetComponentTypeHandle<GroupAmbienceData>(true),
                RealisticHouseholdDataLookup = SystemAPI.GetComponentLookup<RealisticHouseholdData>(true),
                sqm_per_apartment = Mod.m_Setting.residential_sqm_per_apartment,
                residential_avg_floor_height = Mod.m_Setting.residential_avg_floor_height,
                enable_rh_apt_per_floor = Mod.m_Setting.disable_row_homes_apt_per_floor,
                rowhome_apt_per_floor = Mod.m_Setting.rowhomes_apt_per_floor,
                rowhome_basement = Mod.m_Setting.rowhomes_basement,
                units_per_elevator = Mod.m_Setting.residential_units_per_elevator,
                single_family = Mod.m_Setting.single_household_low_density,
                luxury_highrise_less_apt = !Mod.m_Setting.disable_high_level_less_apt,
                lv4_increase = Mod.m_Setting.residential_l4_reduction / 100f,
                lv5_increase = Mod.m_Setting.residential_l5_reduction / 100f,
                hallway_pct = Mod.m_Setting.residential_hallway_space / 100f,
                global_reduction = Mod.m_Setting.results_reduction / 100f,
                sqm_per_apartment_lowdensity = Mod.m_Setting.residential_lowdensity_sqm_per_apartment,
                UffLookup = SystemAPI.GetComponentLookup<RealisticWorkplacesAndHouseholds.Components.UsableFootprintFactor>(true),
                RowHomeLikeLookup = SystemAPI.GetComponentLookup<RowHomeLike>(true),
                PrefabNameMap = _prefabNames,
                AssetPackFactorsLookup = _assetPackFactors,
                reset = reset,
            };

            this.Dependency = job.ScheduleParallel(query, this.Dependency);
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_prefabNames.IsCreated)
                _prefabNames.Dispose();
            if (_assetPackFactors.IsCreated)                 
                _assetPackFactors.Dispose();
        }
    }
}
