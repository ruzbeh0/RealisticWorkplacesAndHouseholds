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
using System.Reflection;

// These tests execute the linked production system, job and capacity calculation.
// Only the ECS/game API surfaces are replaced. Assertions describe behavior,
// rather than checking implementation text or reimplementing warehouse logic.
internal static class Program
{
    private static int passed;
    private static void Main()
    {
        Run("repair query selects companies missing a component", QueryMembership);
        Run("base query exclusions", QueryExclusions);
        Run("zoned warehouse with ServiceDispatch gets workplaces", ServiceDispatchWarehouse);
        Run("city-service storage without CompanyData is never touched", CityServiceStorage);
        Run("steady-state companies avoid synchronization and materialization", SteadyState);
        Run("synchronous initialization precedes deferred job and playback", ImmediateRepair);
        Run("repair keeps existing employees and avoids duplicate additions", EmployeeRetention);
        Run("unchanged capacity queues zero commands; changed capacity retains cooldown", ConditionalWrites);
        Run("serialized workplace defaults normalize once", WorkplaceNormalization);
        Run("ABC overrides on company and property, including removal", Overrides);
        Run("settings, footprint and prefab changes apply on subsequent updates", Invalidation);
        Run("consecutive prefab reuse and per-execution mesh invalidation", ConsecutivePrefabReuse);
        Run("no property renter and missing mesh data retain minimum capacity", MinimumCapacity);
        Run("one-floor formula matches the design table", FormulaTable);
        Run("capacity scan runs on its interval or right after a repair", CapacityCadence);
        Run("loading a save rescans capacity on the next update", ReloadRescansCapacity);
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
    private static void Initialize(EntityManager manager, Entity entity, int capacity = 20)
    {
        manager.AddComponentData(entity, new WorkProvider { m_MaxWorkers = capacity, m_EfficiencyCooldown = 37 });
        manager.AddBuffer<Employee>(entity);
        manager.AddComponentData(entity, new RealisticWorkplaceData { max_workers = capacity });
    }
    // The capacity scan is periodic; mark it due so each case observes one full update.
    private static void Update(WarehouseUpdateSystem system)
    {
        typeof(WarehouseUpdateSystem).GetField("m_TicksUntilCapacityCheck", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(system, 0);
        system.UpdateForTest();
    }
    private static int CapacityInterval =>
        (int)typeof(WarehouseUpdateSystem).GetField("kCapacityCheckInterval", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
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
        var selected = system.Queries[1].Entities.ToHashSet();
        Check(selected.SetEquals([noProvider, noEmployees, noWorkplace]), "Repair OR query membership differs");
        Equal(4, system.Queries[0].Entities.Count(), "Capacity query must keep initialized warehouses");
    }
    private static void QueryExclusions()
    {
        AssertExcluded<OutsideConnection>(); AssertExcluded<Deleted>(); AssertExcluded<Temp>();
        var system = NewSystem(); var entity = Storage(system.EntityManager, Entity.Null, company: false);
        Check(system.Queries.All(q => !q.Entities.Contains(entity)), "Storage without CompanyData must be excluded");
    }
    // Regression for ee38672: vanilla gives every StorageCompany a ServiceDispatch buffer.
    private static void ServiceDispatchWarehouse()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager)));
        manager.AddBuffer<ServiceDispatch>(storage);
        Update(system);
        Check(manager.HasComponent<WorkProvider>(storage), "Warehouse with ServiceDispatch must be repaired");
        Finish(system);
        Equal(20, Workers(manager, storage), "Warehouse with ServiceDispatch receives capacity");
    }
    private static void CityServiceStorage()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var station = Storage(manager, Entity.Null, company: false);
        manager.RemoveComponent<PropertyRenter>(station);
        manager.AddComponentData(station, new CargoTransportStation());
        manager.AddBuffer<ServiceDispatch>(station);
        Update(system); Finish(system);
        Check(!manager.HasComponent<WorkProvider>(station), "Cargo station must not get a WorkProvider");
        Check(!manager.HasBuffer<Employee>(station), "Cargo station must not get employees");
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
        Update(system);
        Equal(0, JobHandle.Completions, "Steady-state completion count");
        Equal(0, system.Queries[1].Materializations, "Steady-state repair array allocations");
        Finish(system);
        Equal(20, Workers(manager, system.Queries[0].Entities.First()), "Capacity stays active");
    }
    private static void ImmediateRepair()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var property = Building(manager, Prefab(manager)); var storage = Storage(manager, property);
        Update(system);
        Equal(1, Workers(manager, storage), "WorkProvider exists before job executes");
        Check(manager.HasBuffer<Employee>(storage), "Employees initialized immediately");
        Equal(1, manager.GetComponentData<RealisticWorkplaceData>(storage).max_workers, "RWD initialized immediately");
        Equal(0, system.World.Barrier.CommandCount, "Scheduled job has not yet run");
        JobChunkExtensions.Drain();
        Equal(1, Workers(manager, storage), "Calculated capacity remains deferred");
        Equal(2, system.World.Barrier.CommandCount, "Only two capacity sets needed after repair");
        system.World.Barrier.Playback();
        Equal(20, Workers(manager, storage), "Expected capacity after playback");
        Update(system); Finish(system);
        Equal(1, JobHandle.Completions, "No subsequent repair completion");
    }
    private static void EmployeeRetention()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager)));
        var worker = manager.Create(); var employees = manager.AddBuffer<Employee>(storage);
        employees.Add(new Employee { m_Worker = worker });
        Update(system);
        Check(ReferenceEquals(employees, manager.GetBuffer<Employee>(storage)), "Repair replaced employees");
        Finish(system);
        Equal(worker, manager.GetBuffer<Employee>(storage)[0].m_Worker, "Existing worker retained");
        Equal(20, Workers(manager, storage), "Capacity after repair");
    }
    private static void ConditionalWrites()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage);
        Update(system); JobChunkExtensions.Drain();
        Equal(0, system.World.Barrier.CommandCount, "Unchanged capacity must produce no commands"); system.World.Barrier.Playback();
        Mod.m_Setting.warehouse_sqm_per_worker = 200;
        Update(system); JobChunkExtensions.Drain();
        Equal(2, system.World.Barrier.CommandCount, "Changed capacity requires two sets"); system.World.Barrier.Playback();
        Equal(10, Workers(manager, storage), "New capacity");
        Equal(37, manager.GetComponentData<WorkProvider>(storage).m_EfficiencyCooldown, "Cooldown retained");
    }
    private static void WorkplaceNormalization()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage);
        manager.SetComponentData(storage, new RealisticWorkplaceData { max_workers = 20, version = 8, space_multiplier = 2 });
        Update(system); JobChunkExtensions.Drain();
        Equal(1, system.World.Barrier.CommandCount, "Non-default serialized fields must be normalized"); system.World.Barrier.Playback();
        var data = manager.GetComponentData<RealisticWorkplaceData>(storage);
        Equal(1, data.version, "Version reset matches old replacement semantics"); Equal(0f, data.space_multiplier, "Multiplier normalized");
        Update(system); JobChunkExtensions.Drain(); Equal(0, system.World.Barrier.CommandCount, "Normalization runs once"); system.World.Barrier.Playback();
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
            Update(system);
            Equal(1, Workers(manager, uninitialized), "ABC does not suppress immediate provider initialization");
            Check(manager.HasBuffer<Employee>(uninitialized), "ABC does not suppress immediate employee initialization");
            Equal(1, manager.GetComponentData<RealisticWorkplaceData>(uninitialized).max_workers, "ABC workplace initialization default");
            JobChunkExtensions.Drain(); Equal(0, system.World.Barrier.CommandCount, "ABC capacity protected"); system.World.Barrier.Playback();
            Equal(71, Workers(manager, storage), "Override value retained");
            Equal(1, Workers(manager, uninitialized), "ABC initialized default retained until override removal");
            manager.RemoveComponent<ABCWorkplaceOverride>(overridePrefab);
            manager.RemoveComponent<ABCWorkplaceOverride>(uninitializedOverridePrefab);
            Update(system); Finish(system); Equal(20, Workers(manager, storage), "Removing ABC override resumes capacity updates");
            Equal(20, Workers(manager, uninitialized), "Removing ABC override updates initialized warehouse");
        }
    }
    private static void Invalidation()
    {
        var system = NewSystem(); var manager = system.EntityManager; var prefab = Prefab(manager);
        var property = Building(manager, prefab); var storage = Storage(manager, property);
        Update(system); Finish(system); Equal(20, Workers(manager, storage), "Baseline capacity");
        Mod.m_Setting.results_reduction = 50;
        Update(system); Finish(system); Equal(10, Workers(manager, storage), "Reduction settings applied");
        Mod.m_Setting.results_reduction = 0;
        manager.AddComponentData(prefab, new UsableFootprintFactor { Value = 0.25f });
        Update(system); Finish(system); Equal(5, Workers(manager, storage), "Changed footprint applied");
        manager.RemoveComponent<UsableFootprintFactor>(prefab);
        Mod.m_Setting.industry_avg_floor_height = 10;
        Update(system); Finish(system); Equal(20, Workers(manager, storage), "Warehouses count one floor, whatever the floor height");
        manager.SetComponentData(property, new PrefabRef { m_Prefab = Prefab(manager, 20, 15, 50) });
        Update(system); Finish(system); Equal(10, Workers(manager, storage), "Property prefab reassignment applied");
    }
    private static void MinimumCapacity()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Entity.Null); manager.RemoveComponent<PropertyRenter>(storage);
        Update(system); Finish(system); Equal(20, Workers(manager, storage), "Storage acts as own property without renter");
        manager.SetComponentData(storage, new PrefabRef { m_Prefab = manager.Create() });
        Update(system); Finish(system); Equal(1, Workers(manager, storage), "Missing submesh fallback");
        manager.AddComponentData(storage, new PropertyRenter { m_Property = Entity.Null });
        Update(system); Finish(system); Equal(1, Workers(manager, storage), "Null property fallback");
        manager.SetComponentData(storage, new PropertyRenter { m_Property = manager.Create() });
        Update(system); Finish(system); Equal(1, Workers(manager, storage), "Live property without PrefabRef fallback");
        manager.SetComponentData(storage, new PropertyRenter { m_Property = Building(manager, Prefab(manager)) });
        Mod.m_Setting.results_reduction = 100;
        Update(system); Finish(system); Equal(1, Workers(manager, storage), "Capacity clamps to the minimum after reduction");
    }
    // Expected sizes from research/warehouse_workplaces.md (default 100 m² per worker).
    private static void FormulaTable()
    {
        Equal(24, BuildingUtils.warehouseWorkers(40, 60, 100), "40 x 60 m");
        Equal(45, BuildingUtils.warehouseWorkers(80, 100, 100), "80 x 100 m, smoothed x1.79");
        var system = NewSystem(); var manager = system.EntityManager;
        var small = Storage(manager, Building(manager, Prefab(manager, 10, 15, 10)));
        Update(system); Finish(system); Equal(1, Workers(manager, small), "Small warehouse gets the minimum");
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
        Update(system); Finish(system);
        int[] expected = [20, 20, 10, 20, 1, 1, 20];
        for (int i = 0; i < storage.Length; i++) Equal(expected[i], Workers(manager, storage[i]), $"A,A,B,A,Null,Null,A position {i}");
        Equal(4, ComponentLookup<MeshData>.IndexerReads, "Consecutive identical prefab should reuse geometry");
        var meshEntity = manager.GetBuffer<SubMesh>(prefabA)[0].m_SubMesh;
        var mesh = manager.GetComponentData<MeshData>(meshEntity);
        mesh.m_Bounds = new(0, new(20, 15, 50)); manager.SetComponentData(meshEntity, mesh);
        ComponentLookup<MeshData>.IndexerReads = 0;
        Update(system); Finish(system);
        for (int i = 0; i < storage.Length; i++) Equal(i is 4 or 5 ? 1 : 10, Workers(manager, storage[i]), $"Mesh mutation position {i}");
        Equal(4, ComponentLookup<MeshData>.IndexerReads, "Geometry must be read afresh on each execution");
    }
    private static void CapacityCadence()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage, 7);
        system.UpdateForTest(); Equal(1, JobChunkExtensions.Pending.Count, "First update scans capacity"); Finish(system);
        Equal(20, Workers(manager, storage), "First scan applies capacity");
        Mod.m_Setting.warehouse_sqm_per_worker = 200;
        for (int i = 1; i < CapacityInterval; i++) system.UpdateForTest();
        Equal(0, JobChunkExtensions.Pending.Count, "No capacity scan before the interval");
        system.UpdateForTest(); Equal(1, JobChunkExtensions.Pending.Count, "Capacity scan on the interval"); Finish(system);
        Equal(10, Workers(manager, storage), "Settings change applied on the interval");
        var added = Storage(manager, Building(manager, Prefab(manager)));
        system.UpdateForTest(); Equal(1, JobChunkExtensions.Pending.Count, "Repair triggers an immediate capacity scan"); Finish(system);
        Equal(10, Workers(manager, added), "Repaired warehouse receives capacity in the same frame");
    }
    private static void ReloadRescansCapacity()
    {
        var system = NewSystem(); var manager = system.EntityManager;
        var storage = Storage(manager, Building(manager, Prefab(manager))); Initialize(manager, storage);
        system.UpdateForTest(); Finish(system);
        Mod.m_Setting.warehouse_sqm_per_worker = 200;
        system.UpdateForTest(); Equal(0, JobChunkExtensions.Pending.Count, "No scan mid-interval");
        system.LoadForTest(); system.UpdateForTest(); Equal(1, JobChunkExtensions.Pending.Count, "Scan right after load"); Finish(system);
        Equal(10, Workers(manager, storage), "Capacity updated after load");
    }
}
