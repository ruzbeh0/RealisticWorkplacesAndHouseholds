using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [BurstCompile]
    public partial class WarehouseDebugSystem : GameSystemBase
    {
        private EntityQuery _storageCompaniesQuery;

        private ComponentLookup<WorkProvider> _workProviderLookup;
        private ComponentLookup<WorkplaceData> _workplaceDataLookup;
        private ComponentLookup<PrefabRef> _prefabRefLookup;

        private SimulationSystem _sim;
        private bool _printed;

        // NEW: one-frame delay state
        private bool _armed;
        private int _armFrame;

        protected override void OnCreate()
        {
            base.OnCreate();

            _sim = World.GetOrCreateSystemManaged<SimulationSystem>();

            _storageCompaniesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<CompanyData>(),
                    ComponentType.ReadOnly<PropertyRenter>(),
                    ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
                },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });

            // We intentionally do NOT RequireForUpdate — we want to wait until something exists.
        }

        protected override void OnUpdate()
        {
            if (_printed) return;

            // --- One-frame delay: arm on first call, then wait for next frame ---
            if (!_armed)
            {
                _armed = true;
                _armFrame = (int)_sim.frameIndex;
                // Optional: Mod.log.Info($"[WarehouseDebug] Armed at frame={_armFrame}, will run next frame.");
                return;
            }
            if ((int)_sim.frameIndex <= _armFrame)
            {
                // Still same frame; wait for ECB playback at end-of-frame
                return;
            }
            // --------------------------------------------------------------------

            int total = _storageCompaniesQuery.CalculateEntityCount();
            if (total == 0) return; // keep waiting until warehouses exist

            _workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true);
            _workplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(true);
            _prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true);

            var companies = _storageCompaniesQuery.ToEntityArray(Allocator.Temp);

            int withProvider = 0;
            int withWorkplaceOnPrefab = 0;

            int sampleLimit = math.min(10, total);
            int samplesMissingProvider = 0;
            int samplesMissingWorkplace = 0;

            for (int i = 0; i < companies.Length; i++)
            {
                Entity company = companies[i];
                bool hasProvider = _workProviderLookup.HasComponent(company);
                if (hasProvider) withProvider++;

                Entity prefab = _prefabRefLookup.HasComponent(company)
                    ? _prefabRefLookup[company].m_Prefab
                    : Entity.Null;

                bool hasWorkplace = prefab != Entity.Null && _workplaceDataLookup.HasComponent(prefab);
                if (hasWorkplace) withWorkplaceOnPrefab++;

                if (!hasProvider && samplesMissingProvider < sampleLimit)
                {
                    Mod.log.Info($"[WarehouseDebug] Missing WorkProvider on company entity={company.Index}, prefab={prefab.Index}");
                    samplesMissingProvider++;
                }
                if (!hasWorkplace && samplesMissingWorkplace < sampleLimit)
                {
                    Mod.log.Info($"[WarehouseDebug] Missing WorkplaceData on company PREFAB entity={prefab.Index}, company={company.Index}");
                    samplesMissingWorkplace++;
                }
            }

            Mod.log.Info($"[WarehouseDebug] StorageCompany summary: total={total}, with WorkProvider={withProvider}, with WorkplaceData on PREFAB={withWorkplaceOnPrefab}");

            // Print a few examples with current values (optional)
            int printedExamples = 0;
            foreach (var company in companies)
            {
                if (printedExamples >= 5) break;

                bool hasProvider = _workProviderLookup.HasComponent(company);
                Entity prefab = _prefabRefLookup.HasComponent(company) ? _prefabRefLookup[company].m_Prefab : Entity.Null;
                bool hasWorkplace = prefab != Entity.Null && _workplaceDataLookup.HasComponent(prefab);

                if (hasProvider || hasWorkplace)
                {
                    int providerMax = hasProvider ? _workProviderLookup[company].m_MaxWorkers : -1;
                    int prefabMax = hasWorkplace ? _workplaceDataLookup[prefab].m_MaxWorkers : -1;
                    Mod.log.Info($"[WarehouseDebug] company={company.Index} providerMax={providerMax} | prefab={prefab.Index} workplaceMax={prefabMax}");
                    printedExamples++;
                }
            }

            companies.Dispose();

            _printed = true;
            Enabled = false; // one-shot
        }
    }
}
