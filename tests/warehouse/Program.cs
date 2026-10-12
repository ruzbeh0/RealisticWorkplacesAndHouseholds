using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using RealisticWorkplacesAndHouseholds;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Systems;
using Unity.Entities;
using Unity.Jobs;

// These tests execute the linked production system, job and capacity calculation.
// Only the ECS/game API surfaces are replaced. Assertions describe behavior,
// rather than checking implementation text or reimplementing warehouse logic.
internal static class Program
{
    private static int passed;
    private static void Main()
    {
        Run("repair query selects missing components and property fallback", QueryMembership);
        Run("base query exclusions", QueryExclusions);
        Run("steady-state companies avoid synchronization and materialization", SteadyState);
        Run("synchronous initialization precedes deferred job and playback", ImmediateRepair);
        Run("shared property preserves employees and avoids duplicate additions", SharedProperty);
        Run("unchanged capacity queues zero commands; changed capacity retains cooldown", ConditionalWrites);
        Run("serialized workplace defaults normalize once", WorkplaceNormalization);
        Run("ABC overrides on company and property, including removal", Overrides);
        Run("service and transport protection on entities and prefabs", ServiceProtection);
        Run("settings, footprint and prefab changes apply on subsequent updates", Invalidation);
        Run("consecutive prefab reuse and per-execution mesh invalidation", ConsecutivePrefabReuse);
        Run("no property renter and missing mesh data retain minimum capacity", MinimumCapacity);
        Console.WriteLine($"PASS: {passed} warehouse behavioral regression cases.");
    }

    private static void Run(string name, Action test)
    {
        Mod.m_Setting = new();
        JobHandle.Completions = 0;
        Check(JobChunkExtensions.Pending.Count == 0, "Previous test left a pending job");
        test(); passed++; Console.WriteLine($"PASS {name}");
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual, string message)
    { Check(EqualityComparer<T>.Default.Equals(expected, actual), $"{message}: expected {expected}, got {actual}"); }
    private static WarehouseUpdateSystem NewSystem()
    { var system = new WarehouseUpdateSystem(); system.CreateForTest(); return system; }
    private static Entity Prefab(EntityManager manager, float width = 40, float height = 15, float length = 50)
    {
        var mesh = manager.Create();
        manager.AddComponentData(mesh, new MeshData
        {
            m_State = MeshFlags.Base,
            m_DecalLayer = Game.Rendering.DecalLayers.Buildings,
            m_Bounds = new(0, new(width, height, length))
        });
        var prefab = manager.Create();
        manager.AddBuffer<SubMesh>(prefab).Add(new SubMesh { m_SubMesh = mesh });
        return prefab;
    }
    private static Entity Building(EntityManager manager, Entity prefab)
    { var entity = manager.Create(); manager.AddComponentData(entity, new PrefabRef { m_Prefab = prefab }); return entity; }
    private static Entity Storage(EntityManager manager, Entity property, bool company = true)
    {
        var entity = Building(manager, Prefab(manager));
        manager.AddComponentData(entity, new StorageCompany());
        if (company) manager.AddComponentData(entity, new CompanyData());
        manager.AddComponentData(entity, new PropertyRenter { m_Property = property });
        return entity;
    }
    private static void Initialize(EntityManager manager, Entity entity, int capacity = 40)
    {
        manager.AddComponentData(entity, new WorkProvider { m_MaxWorkers = capacity, m_EfficiencyCooldown = 37 });
        manager.AddBuffer<Employee>(entity);
        manager.AddComponentData(entity, new RealisticWorkplaceData { max_workers = capacity });
    }
    private static void Finish(WarehouseUpdateSystem system)
    { JobChunkExtensions.Drain(); system.World.Barrier.Playback(); }
    private static int Workers(EntityManager manager, Entity entity) => manager.GetComponentData<WorkProvider>(entity).m_MaxWorkers;

    private static void QueryMembership()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var property = Building(manager, Prefab(manager));
        var complete = Storage(manager, property); Initialize(manager, complete);
        var noProvider = Storage(manager, property); Initialize(manager, noProvider); manager.RemoveComponent<WorkProvider>(noProvider);
        var noEmployees = Storage(manager, property); Initialize(manager, noEmployees); manager.RemoveComponent<Employee>(noEmployees);
        var noWorkplace = Storage(manager, property); Initialize(manager, noWorkplace); manager.RemoveComponent<RealisticWorkplaceData>(noWorkplace);
        var fallback = Storage(manager, property, false); Initialize(manager, fallback);
        var nonRenter = Storage(manager, property, false); Initialize(manager, nonRenter); manager.RemoveComponent<PropertyRenter>(nonRenter);
        var selected = system.Queries[1].Entities.ToHashSet();
        Check(selected.SetEquals([noProvider, noEmployees, noWorkplace, fallback]), "Repair OR query membership differs");
        Equal(6, system.Queries[0].Entities.Count(), "Capacity query must keep initialized warehouses");
    }
    private static void QueryExclusions()
    {
        AssertExcluded<OutsideConnection>(); AssertExcluded<CityServiceUpkeep>();
        AssertExcluded<ResourceConsumer>(); AssertExcluded<Hospital>();
        AssertExcluded<Deleted>(); AssertExcluded<Temp>();
        var system = NewSystem(); var entity = Storage(system.EntityManager, Entity.Null);
        system.EntityManager.AddBuffer<ServiceDispatch>(entity);
        Check(system.Queries.All(q => !q.Entities.Contains(entity)), "Service dispatch buffer must exclude storage");
    }
    private static void AssertExcluded<T>() where T : new()
    {
        var system = NewSystem(); var entity = Storage(system.EntityManager, Entity.Null);
        system.EntityManager.AddComponentData(entity, new T());
        Check(system.Queries.All(q => !q.Entities.Contains(entity)), $"{typeof(T).Name} must exclude storage");
    }
    private static void SteadyState()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var property = Building(manager, Prefab(manager));
        for (int i = 0; i < 1000; i++) Initialize(manager, Storage(manager, property));
        system.UpdateForTest();
        Equal(0, JobHandle.Completions, "Steady-state completion count");
        Equal(0, system.Queries[1].Materializations, "Steady-state repair array allocations");
        Finish(system);
        Equal(40, Workers(manager, system.Queries[0].Entities.First()), "Capacity stays active");
    }
    private static void ImmediateRepair()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property);
        system.UpdateForTest();
        Equal(1, Workers(manager, storage), "WorkProvider exists before job executes");
        Check(manager.HasBuffer<Employee>(storage), "Employees initialized immediately");
        Equal(1, manager.GetComponentData<RealisticWorkplaceData>(storage).max_workers, "RWD initialized immediately");
        Equal(0, system.World.Barrier.CommandCount, "Scheduled job has not yet run");
        JobChunkExtensions.Drain();
        Equal(1, Workers(manager, storage), "Calculated capacity remains deferred");
        Equal(2, system.World.Barrier.CommandCount, "Only two capacity sets needed after repair");
        system.World.Barrier.Playback();
        Equal(40, Workers(manager, storage), "Expected capacity after playback");
        system.UpdateForTest(); Finish(system);
        Equal(1, JobHandle.Completions, "No subsequent repair completion");
    }
    private static void SharedProperty()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var property = Building(manager, Prefab(manager));
        var worker = manager.Create(); var employees = manager.AddBuffer<Employee>(property);
        employees.Add(new Employee { m_Worker = worker });
        var first = Storage(manager, property, false); var second = Storage(manager, property, false);
        system.UpdateForTest();
        Check(ReferenceEquals(employees, manager.GetBuffer<Employee>(property)), "Repair replaced property employees");
        Finish(system);
        Equal(1, manager.GetBuffer<Employee>(property).Count, "Playback retained property employees");
        Equal(worker, manager.GetBuffer<Employee>(property)[0].m_Worker, "Existing worker retained");
        Equal(40, Workers(manager, property), "Property fallback capacity");
        Equal(40, Workers(manager, first), "First storage capacity"); Equal(40, Workers(manager, second), "Second storage capacity");
        // Fallback renters deliberately remain eligible, since property components
        // can disappear independently of the storage entity's archetype.
        manager.RemoveComponent<WorkProvider>(property);
        system.UpdateForTest(); Equal(1, Workers(manager, property), "Property loss repaired immediately"); Finish(system);
    }
    private static void ConditionalWrites()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage);
        system.UpdateForTest(); JobChunkExtensions.Drain();
        Equal(0, system.World.Barrier.CommandCount, "Unchanged capacity must produce no commands"); system.World.Barrier.Playback();
        Mod.m_Setting.warehouse_sqm_per_worker = 200;
        system.UpdateForTest(); JobChunkExtensions.Drain();
        Equal(2, system.World.Barrier.CommandCount, "Changed capacity requires two sets"); system.World.Barrier.Playback();
        Equal(20, Workers(manager, storage), "New capacity");
        Equal(37, manager.GetComponentData<WorkProvider>(storage).m_EfficiencyCooldown, "Cooldown retained");
    }
    private static void WorkplaceNormalization()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage);
        manager.SetComponentData(storage, new RealisticWorkplaceData { max_workers = 40, version = 8, space_multiplier = 2 });
        system.UpdateForTest(); JobChunkExtensions.Drain();
        Equal(1, system.World.Barrier.CommandCount, "Non-default serialized fields must be normalized"); system.World.Barrier.Playback();
        var data = manager.GetComponentData<RealisticWorkplaceData>(storage);
        Equal(1, data.version, "Version reset matches old replacement semantics"); Equal(0f, data.space_multiplier, "Multiplier normalized");
        system.UpdateForTest(); JobChunkExtensions.Drain(); Equal(0, system.World.Barrier.CommandCount, "Normalization runs once"); system.World.Barrier.Playback();
    }
    private static void Overrides()
    {
        foreach (bool onProperty in new[] { false, true })
        {
            var system = NewSystem(); var manager = system.EntityManager;
            var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property); Initialize(manager, storage, 71);
            var overridePrefab = manager.GetComponentData<PrefabRef>(onProperty ? property : storage).m_Prefab;
            manager.AddComponentData(overridePrefab, new ABCWorkplaceOverride());
            var uninitialized = Storage(manager, property);
            var uninitializedOverridePrefab = manager.GetComponentData<PrefabRef>(onProperty ? property : uninitialized).m_Prefab;
            if (!manager.HasComponent<ABCWorkplaceOverride>(uninitializedOverridePrefab))
                manager.AddComponentData(uninitializedOverridePrefab, new ABCWorkplaceOverride());
            system.UpdateForTest();
            Equal(1, Workers(manager, uninitialized), "ABC does not suppress immediate provider initialization");
            Check(manager.HasBuffer<Employee>(uninitialized), "ABC does not suppress immediate employee initialization");
            Equal(1, manager.GetComponentData<RealisticWorkplaceData>(uninitialized).max_workers, "ABC workplace initialization default");
            JobChunkExtensions.Drain(); Equal(0, system.World.Barrier.CommandCount, "ABC capacity protected"); system.World.Barrier.Playback();
            Equal(71, Workers(manager, storage), "Override value retained");
            Equal(1, Workers(manager, uninitialized), "ABC initialized default retained until override removal");
            manager.RemoveComponent<ABCWorkplaceOverride>(overridePrefab);
            manager.RemoveComponent<ABCWorkplaceOverride>(uninitializedOverridePrefab);
            system.UpdateForTest(); Finish(system); Equal(40, Workers(manager, storage), "Removing ABC override resumes capacity updates");
            Equal(40, Workers(manager, uninitialized), "Removing ABC override updates initialized warehouse");
        }
    }
    private static void ServiceProtection()
    {
        ProtectEntity<TransportDepot>(); ProtectEntity<CargoTransportStation>();
        ProtectEntity<MaintenanceDepot>(); ProtectEntity<TransportStation>();
        ProtectEntity<CityServiceUpkeep>(); ProtectEntity<ResourceConsumer>(); ProtectEntity<Hospital>();
        ProtectPrefab<ServiceObjectData>(); ProtectPrefab<ResourceConsumerData>(); ProtectPrefab<HospitalData>();
        ProtectPrefab<TransportDepotData>(); ProtectPrefab<CargoTransportStationData>();
        ProtectPrefab<MaintenanceDepotData>(); ProtectPrefab<TransportStationData>();
        foreach (bool onProperty in new[] { false, true })
        {
            var system = NewSystem(); var manager = system.EntityManager;
            var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property);
            var target = onProperty ? property : storage;
            manager.AddBuffer<ServiceDispatch>(target);
            system.UpdateForTest(); Finish(system); Check(!manager.HasComponent<WorkProvider>(storage), "Service buffer must protect initialization");
            var second = NewSystem(); manager = second.EntityManager;
            property = Building(manager, Prefab(manager)); storage = Storage(manager, property);
            manager.AddBuffer<ServiceUpkeepData>(manager.GetComponentData<PrefabRef>(onProperty ? property : storage).m_Prefab);
            second.UpdateForTest(); Finish(second); Check(!manager.HasComponent<WorkProvider>(storage), "Prefab upkeep buffer must protect initialization");
        }
    }
    private static void ProtectEntity<T>() where T : new()
    {
        foreach (bool onProperty in new[] { false, true })
        {
            var system = NewSystem(); var manager = system.EntityManager;
            var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property);
            manager.AddComponentData(onProperty ? property : storage, new T());
            system.UpdateForTest(); Finish(system);
            Check(!manager.HasComponent<WorkProvider>(storage), $"Entity {typeof(T).Name} protection failed");
        }
    }
    private static void ProtectPrefab<T>() where T : new()
    {
        foreach (bool onProperty in new[] { false, true })
        {
            var system = NewSystem(); var manager = system.EntityManager;
            var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property);
            manager.AddComponentData(manager.GetComponentData<PrefabRef>(onProperty ? property : storage).m_Prefab, new T());
            system.UpdateForTest(); Finish(system);
            Check(!manager.HasComponent<WorkProvider>(storage), $"Prefab {typeof(T).Name} protection failed");
        }
    }
    private static void Invalidation()
    {
        var system = NewSystem(); var manager = system.EntityManager; var prefab = Prefab(manager);
        var property = Building(manager, prefab); var storage = Storage(manager, property);
        system.UpdateForTest(); Finish(system); Equal(40, Workers(manager, storage), "Baseline capacity");
        Mod.m_Setting.results_reduction = 50;
        system.UpdateForTest(); Finish(system); Equal(20, Workers(manager, storage), "Reduction settings applied");
        Mod.m_Setting.results_reduction = 0;
        manager.AddComponentData(prefab, new UsableFootprintFactor { Value = 0.25f });
        system.UpdateForTest(); Finish(system); Equal(10, Workers(manager, storage), "Changed footprint applied");
        manager.RemoveComponent<UsableFootprintFactor>(prefab);
        Mod.m_Setting.industry_avg_floor_height = 10;
        system.UpdateForTest(); Finish(system); Equal(20, Workers(manager, storage), "Floor-height settings applied");
        manager.SetComponentData(property, new PrefabRef { m_Prefab = Prefab(manager, 20, 15, 50) });
        system.UpdateForTest(); Finish(system); Equal(10, Workers(manager, storage), "Property prefab reassignment applied");
    }
    private static void MinimumCapacity()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Entity.Null); manager.RemoveComponent<PropertyRenter>(storage);
        system.UpdateForTest(); Finish(system); Equal(40, Workers(manager, storage), "Storage acts as own property without renter");
        manager.SetComponentData(storage, new PrefabRef { m_Prefab = manager.Create() });
        system.UpdateForTest(); Finish(system); Equal(1, Workers(manager, storage), "Missing submesh fallback");
        manager.AddComponentData(storage, new PropertyRenter { m_Property = Entity.Null });
        system.UpdateForTest(); Finish(system); Equal(1, Workers(manager, storage), "Null property fallback");
        manager.SetComponentData(storage, new PropertyRenter { m_Property = manager.Create() });
        system.UpdateForTest(); Finish(system); Equal(1, Workers(manager, storage), "Live property without PrefabRef fallback");
        Mod.m_Setting.results_reduction = 100;
        system.UpdateForTest(); Finish(system); Equal(1, Workers(manager, storage), "Capacity clamps to one");
    }
    private static void ConsecutivePrefabReuse()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var prefabA = Prefab(manager); var prefabB = Prefab(manager, 20);
        var propertyA = Building(manager, prefabA); var otherPropertyA = Building(manager, prefabA);
        var propertyB = Building(manager, prefabB);
        Entity[] properties = [propertyA, otherPropertyA, propertyB, propertyA, Entity.Null, Entity.Null, otherPropertyA];
        var storage = properties.Select(property => Storage(manager, property)).ToArray();
        ComponentLookup<MeshData>.IndexerReads = 0;
        system.UpdateForTest(); Finish(system);
        int[] expected = [40, 40, 20, 40, 1, 1, 40];
        for (int i = 0; i < storage.Length; i++) Equal(expected[i], Workers(manager, storage[i]), $"A,A,B,A,Null,Null,A position {i}");
        Equal(4, ComponentLookup<MeshData>.IndexerReads, "Consecutive identical prefab should reuse geometry");
        var meshEntity = manager.GetBuffer<SubMesh>(prefabA)[0].m_SubMesh;
        var mesh = manager.GetComponentData<MeshData>(meshEntity);
        mesh.m_Bounds = new(0, new(20, 15, 50)); manager.SetComponentData(meshEntity, mesh);
        ComponentLookup<MeshData>.IndexerReads = 0;
        system.UpdateForTest(); Finish(system);
        for (int i = 0; i < storage.Length; i++) Equal(i is 4 or 5 ? 1 : 20, Workers(manager, storage[i]), $"Mesh mutation position {i}");
        Equal(4, ComponentLookup<MeshData>.IndexerReads, "Geometry must be read afresh on each execution");
    }
}
