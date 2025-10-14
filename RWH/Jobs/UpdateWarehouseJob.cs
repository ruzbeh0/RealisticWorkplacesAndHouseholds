
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Mono.Cecil;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Components;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;

namespace RealisticWorkplacesAndHouseholds.Jobs
{
    public struct UpdateWarehouseJobQuery
    {
        public EntityQueryDesc[] Query;

        public UpdateWarehouseJobQuery()
        {
            Query =
            [
                new()
                {
                    All =
                    [
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<CompanyData>(),
                        ComponentType.ReadOnly<PropertyRenter>(),
                        ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                    ],
                    Any = [
               
                    ],
                    None =
                    [
                        ComponentType.Exclude<WorkProvider>(),
                        ComponentType.Exclude<RealisticWorkplaceData>(),
                        ComponentType.Exclude<Deleted>(),
                        ComponentType.Exclude<Temp>()
                    ],
                },
            ];
        }
    }

    //[BurstCompile]
    public struct UpdateWarehouseJob : IJobChunk
    {
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter ecb;

        [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefHandle;
        [ReadOnly] public ComponentLookup<WorkplaceData> WorkplaceDataLookup;

        public UpdateWarehouseJob()
        {

        }

        public void Execute(in ArchetypeChunk chunk,
    int unfilteredChunkIndex,
    bool useEnabledMask,
    in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var prefabRefArr = chunk.GetNativeArray(ref PrefabRefHandle);

            Mod.log.Info($"length:{entities.Length}");

            for (int i = 0; i < entities.Length; i++)
            {
                var companyEntity = entities[i];
                var prefabEntity = prefabRefArr[i].m_Prefab;

                // 1) Add a tiny WorkProvider so the simulation can hire
                ecb.AddComponent(unfilteredChunkIndex, companyEntity, new WorkProvider
                {
                    m_MaxWorkers = 1,
                    m_EfficiencyCooldown = 0
                });

                // 2) Ensure the *company prefab* has WorkplaceData so the UI shows employees
                if (!WorkplaceDataLookup.HasComponent(prefabEntity))
                {
                    ecb.AddComponent(unfilteredChunkIndex, prefabEntity, new WorkplaceData
                    {
                        // Start at 1; your UpdateWorkplaceJob will scale this to the
                        // geometry-based value on the next pass.
                        m_MaxWorkers = 1
                    });
                }
                Mod.log.Info($"Added WorkProvider to: {companyEntity}");
            }
        }

    }
}