// Deliberately small managed test doubles. These do not implement native ECS,
// Burst, dependencies, chunk change versions, or parallel worker execution.
using System.Collections;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Burst { public class BurstCompileAttribute : Attribute { } }
namespace Unity.Burst.Intrinsics { public struct v128 { } }
namespace UnityEngine { }
namespace UnityEngine.Scripting { public class PreserveAttribute : Attribute { } }
namespace Game.Citizens { }
namespace Colossal.Serialization.Entities
{
    public enum Purpose { LoadGame }
    public interface ISerializable { }
    public interface IWriter { void Write(int value); void Write(float value); }
    public interface IReader { void Read(out int value); void Read(out float value); }
}
namespace Unity.Mathematics
{
    public struct float3(float x, float y, float z)
    {
        public float x = x, y = y, z = z;
        public static implicit operator float3(float value) => new(value, value, value);
    }
    #pragma warning disable CS8981 // Match the real Unity API name.
    public static class math
    {
        public static float floor(float value) => MathF.Floor(value);
        public static int min(int a, int b) => Math.Min(a, b);
        public static int max(int a, int b) => Math.Max(a, b);
        public static float clamp(float value, float low, float high) => Math.Clamp(value, low, high);
    }
    #pragma warning restore CS8981
}
namespace Colossal.Mathematics
{
    public struct Bounds3(Unity.Mathematics.float3 min, Unity.Mathematics.float3 max)
    {
        public Unity.Mathematics.float3 min = min, max = max;
        public static Bounds3 operator |(Bounds3 a, Bounds3 b) => new(
            new(Math.Min(a.min.x, b.min.x), Math.Min(a.min.y, b.min.y), Math.Min(a.min.z, b.min.z)),
            new(Math.Max(a.max.x, b.max.x), Math.Max(a.max.y, b.max.y), Math.Max(a.max.z, b.max.z)));
    }
}
namespace Unity.Collections
{
    public class ReadOnlyAttribute : Attribute { }
    public enum Allocator { Temp }
    public struct NativeArray<T>(T[] items, Action<int, T> write = null) : IEnumerable<T>, IDisposable
    {
        public int Length => items?.Length ?? 0;
        public T this[int i] { get => items[i]; set { items[i] = value; write?.Invoke(i, value); } }
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose() { }
    }
}
namespace Unity.Jobs
{
    public struct JobHandle
    {
        public static int Completions;
        public void Complete() => Completions++;
    }
}
namespace Unity.Entities
{
    public interface IComponentData { }
    public interface IQueryTypeParameter { }
    public readonly record struct Entity(int Id) { public static Entity Null => default; }
    public struct EntityTypeHandle { }
    public struct ComponentTypeHandle<T> { }
    public struct ComponentType(Type type)
    {
        public Type Type = type;
        public static ComponentType ReadOnly<T>() => new(typeof(T));
        public static ComponentType ReadWrite<T>() => new(typeof(T));
        public static ComponentType Exclude<T>() => new(typeof(T));
    }
    public class EntityQueryDesc
    {
        public ComponentType[] All = [], Any = [], None = [];
    }
    public class EntityManager
    {
        private int next;
        public readonly Dictionary<Entity, Dictionary<Type, object>> Data = [];
        public readonly Dictionary<Type, int> Writes = [];
        public Entity Create() { var entity = new Entity(++next); Data.Add(entity, []); return entity; }
        public bool HasComponent<T>(Entity entity) => Has(entity, typeof(T));
        public bool HasBuffer<T>(Entity entity) => HasComponent<T>(entity);
        public bool Has(Entity entity, Type type) => Data.TryGetValue(entity, out var data) && data.ContainsKey(type);
        public T GetComponentData<T>(Entity entity) => (T)Data[entity][typeof(T)];
        public void SetComponentData<T>(Entity entity, T value)
        { Data[entity][typeof(T)] = value; Writes[typeof(T)] = Writes.GetValueOrDefault(typeof(T)) + 1; }
        public void AddComponentData<T>(Entity entity, T value)
        {
            if (HasComponent<T>(entity)) throw new InvalidOperationException($"Duplicate addition: {typeof(T).Name}");
            SetComponentData(entity, value);
        }
        public void RemoveComponent<T>(Entity entity) => Data[entity].Remove(typeof(T));
        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
        {
            if (HasBuffer<T>(entity)) throw new InvalidOperationException($"Would replace existing buffer: {typeof(T).Name}");
            var buffer = new DynamicBuffer<T>(); Data[entity][typeof(T)] = buffer; return buffer;
        }
        public DynamicBuffer<T> GetBuffer<T>(Entity entity) => (DynamicBuffer<T>)Data[entity][typeof(T)];
    }
    public class DynamicBuffer<T> : List<T> { }
    public struct ComponentLookup<T>(EntityManager manager)
    {
        public static int IndexerReads;
        public bool HasComponent(Entity entity) => manager.HasComponent<T>(entity);
        public T this[Entity entity] { get { IndexerReads++; return manager.GetComponentData<T>(entity); } set => manager.SetComponentData(entity, value); }
        public bool TryGetComponent(Entity entity, out T value)
        {
            value = HasComponent(entity) ? this[entity] : default;
            return HasComponent(entity);
        }
    }
    public struct BufferLookup<T>(EntityManager manager)
    {
        public bool HasBuffer(Entity entity) => manager.HasBuffer<T>(entity);
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> buffer)
        {
            buffer = HasBuffer(entity) ? manager.GetBuffer<T>(entity) : null;
            return HasBuffer(entity);
        }
    }
    public class EntityQuery(EntityManager manager, EntityQueryDesc[] descriptions)
    {
        public int Materializations;
        public IEnumerable<Entity> Entities => manager.Data.Keys.Where(entity => descriptions.Any(desc =>
            desc.All.All(c => manager.Has(entity, c.Type)) &&
            (desc.Any.Length == 0 || desc.Any.Any(c => manager.Has(entity, c.Type))) &&
            desc.None.All(c => !manager.Has(entity, c.Type))));
        public bool IsEmptyIgnoreFilter => !Entities.Any();
        public void ResetFilter() { }
        public void SetChangedVersionFilter(Type type) { }
        public NativeArray<Entity> ToEntityArray(Allocator allocator)
        { Materializations++; return new(Entities.ToArray()); }
    }
    public struct ArchetypeChunk(EntityManager manager, Entity[] entities)
    {
        public NativeArray<Entity> GetNativeArray(EntityTypeHandle handle) => new(entities);
        public bool Has<T>(ref ComponentTypeHandle<T> handle) => entities.Length > 0 && manager.HasComponent<T>(entities[0]);
        public NativeArray<T> GetNativeArray<T>(ref ComponentTypeHandle<T> handle)
        {
            var localManager = manager;
            var localEntities = entities;
            return new(localEntities.Select(localManager.GetComponentData<T>).ToArray(),
                (i, value) => localManager.SetComponentData(localEntities[i], value));
        }
    }
    public interface IJobChunk
    {
        void Execute(in ArchetypeChunk chunk, int index, bool enabled, in Unity.Burst.Intrinsics.v128 mask);
    }
    public static class JobChunkExtensions
    {
        // Execute when explicitly drained by a test, not during scheduling.
        public static readonly Queue<Action> Pending = [];
        public static Unity.Jobs.JobHandle ScheduleParallel<T>(this T job, EntityQuery query, Unity.Jobs.JobHandle dependency)
            where T : struct, IJobChunk
        {
            var manager = SystemAPI.Manager;
            Pending.Enqueue(() =>
            {
                int index = 0;
                foreach (var group in query.Entities.GroupBy(manager.HasComponent<Game.Buildings.PropertyRenter>))
                    job.Execute(new ArchetypeChunk(manager, group.ToArray()), index++, false, default);
            });
            return default;
        }
        public static void Drain() { while (Pending.TryDequeue(out var action)) action(); }
    }
    public static class SystemAPI
    {
        public static EntityManager Manager;
        public static EntityTypeHandle GetEntityTypeHandle() => default;
        public static ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool readOnly) => default;
        public static ComponentLookup<T> GetComponentLookup<T>(bool readOnly) => new(Manager);
        public static BufferLookup<T> GetBufferLookup<T>(bool readOnly) => new(Manager);
    }
    public class EntityCommandBuffer
    {
        public readonly List<Action> Commands = [];
        public ParallelWriter AsParallelWriter() => new(this, SystemAPI.Manager);
        public void Playback() { foreach (var command in Commands) command(); Commands.Clear(); }
        public struct ParallelWriter(EntityCommandBuffer buffer, EntityManager manager)
        {
            public void SetComponent<T>(int key, Entity entity, T data)
            { var target = manager; buffer.Commands.Add(() => target.SetComponentData(entity, data)); }
            public void AddComponent<T>(int key, Entity entity, T data)
            { var target = manager; buffer.Commands.Add(() => target.AddComponentData(entity, data)); }
            public void AddBuffer<T>(int key, Entity entity)
            { var target = manager; buffer.Commands.Add(() => target.AddBuffer<T>(entity)); }
        }
    }
    public class World
    {
        public readonly Game.Simulation.EndFrameBarrier Barrier = new();
        public readonly EndSimulationEntityCommandBufferSystem SimulationBarrier = new();
        public T GetOrCreateSystemManaged<T>() where T : class => typeof(T) == typeof(EndSimulationEntityCommandBufferSystem) ? SimulationBarrier as T : Barrier as T;
    }
    public class EndSimulationEntityCommandBufferSystem : Game.Simulation.EndFrameBarrier { }
}
namespace Game
{
    public enum GameMode { Game }
    public abstract class GameSystemBase
    {
        public EntityManager EntityManager = new();
        public World World = new();
        public Unity.Jobs.JobHandle Dependency;
        public readonly List<EntityQuery> Queries = [];
        private EntityQuery required;
        protected virtual void OnCreate() { }
        protected abstract void OnUpdate();
        protected virtual void OnDestroy() { }
        protected virtual void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode) { }
        protected EntityQuery GetEntityQuery(params EntityQueryDesc[] descriptions)
        { var query = new EntityQuery(EntityManager, descriptions); Queries.Add(query); return query; }
        protected void RequireForUpdate(EntityQuery query) { required = query; }
        public void CreateForTest() { SystemAPI.Manager = EntityManager; OnCreate(); }
        public void UpdateForTest() { SystemAPI.Manager = EntityManager; if (required == null || !required.IsEmptyIgnoreFilter) OnUpdate(); }
        public void LoadForTest() => OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose.LoadGame, GameMode.Game);
    }
}
namespace Game.Common { public struct Deleted { } public struct ServiceDispatch { } }
namespace Game.Tools { public struct Temp { } }
namespace Game.City { }
namespace Game.Companies { public struct StorageCompany { } public struct CompanyData { } }
namespace Game.Buildings
{
    public struct CityServiceUpkeep { }
    public struct PropertyRenter { public Entity m_Property; }
    public struct WorkProvider { public int m_MaxWorkers; public int m_EfficiencyCooldown; }
    public struct Employee { public Entity m_Worker; }
    public struct ResourceConsumer { } public struct Hospital { }
    public struct TransportDepot { } public struct CargoTransportStation { }
    public struct MaintenanceDepot { } public struct TransportStation { }
}
namespace Game.Prefabs
{
    public struct ServiceCompanyData { public int m_WorkPerUnit; }
    public struct IndustrialProcessData { public int m_WorkPerUnit; public ProcessOutput m_Output; }
    public struct ProcessOutput { public Game.Economy.Resource m_Resource; }
    public struct WorkplaceData { public int m_MaxWorkers; }
    public struct SpawnableBuildingData { public Entity m_ZonePrefab; }
    public struct ZoneData { public Game.Zones.AreaType m_AreaType; public ZoneFlags m_ZoneFlags; public float m_MaxHeight; }
    public enum ZoneFlags { Office = 1 }
    public struct BuildingPropertyData { public Game.Economy.Resource m_AllowedSold; }
    public struct ExtractorCompanyData { }
    public struct TriggerPrefabData { }
    public struct PrefabRef { public Entity m_Prefab; }
    public struct SubMesh { public Entity m_SubMesh; }
    public struct MeshData
    {
        public MeshFlags m_State;
        public Game.Rendering.DecalLayers m_DecalLayer;
        public Colossal.Mathematics.Bounds3 m_Bounds;
    }
    public enum MeshFlags { Base = 1 }
    public struct TransportDepotData { } public struct CargoTransportStationData { }
    public struct MaintenanceDepotData { } public struct TransportStationData { }
    public struct ServiceObjectData { } public struct ServiceUpkeepData { }
    public struct ResourceConsumerData { } public struct HospitalData { }
}
namespace Game.Rendering { public enum DecalLayers { Buildings = 1 } }
namespace Game.Objects
{
    public struct OutsideConnection { }
    public static class ObjectUtils
    {
        public static Unity.Mathematics.float3 GetSize(Colossal.Mathematics.Bounds3 bounds) =>
            new(bounds.max.x - bounds.min.x, bounds.max.y - bounds.min.y, bounds.max.z - bounds.min.z);
    }
}
namespace Game.Simulation
{
    public class EndFrameBarrier
    {
        public readonly List<EntityCommandBuffer> Buffers = [];
        public EntityCommandBuffer CreateCommandBuffer() { var buffer = new EntityCommandBuffer(); Buffers.Add(buffer); return buffer; }
        public void AddJobHandleForProducer(Unity.Jobs.JobHandle handle) { }
        public int CommandCount => Buffers.Sum(b => b.Commands.Count);
        public void Playback() { foreach (var buffer in Buffers) buffer.Playback(); Buffers.Clear(); }
    }
}
namespace RealisticWorkplacesAndHouseholds
{
    public static class Mod { public static TestSettings m_Setting = new(); }
    public class TestSettings
    {
        public float industry_avg_floor_height = 5;
        public float warehouse_sqm_per_worker = 100;
        public float results_reduction = 0;
        public float commercial_sqm_per_worker = 100, office_sqm_per_worker = 100;
        public float commercial_avg_floor_height = 5, industry_sqm_per_worker = 100;
        public int office_elevators_per_sqm = 0;
        public bool commercial_self_service_gas;
        public float office_non_usable_space = 0;
        public float commercial_sqm_per_worker_restaurants = 100, commercial_sqm_per_worker_supermarket = 100;
        public float commercial_sqm_per_worker_hotel = 100, commercial_sqm_per_worker_rec_entertainment = 100;
        public int industry_area_base = 2500, office_height_base = 10;
    }
}
namespace Game.Economy
{
    [Flags]
    public enum Resource : ulong
    {
        NoResource = 0, Furniture = 1UL << 0, Textiles = 1UL << 1, Chemicals = 1UL << 2,
        Plastics = 1UL << 3, Electronics = 1UL << 4, Vehicles = 1UL << 5,
        Food = 1UL << 6, Beverages = 1UL << 7, Paper = 1UL << 8,
        Pharmaceuticals = 1UL << 9, Fish = 1UL << 10, Petrochemicals = 1UL << 11,
        Meals = 1UL << 12, ConvenienceFood = 1UL << 13, Recreation = 1UL << 14,
        Entertainment = 1UL << 15, Lodging = 1UL << 16, Wood = 1UL << 17,
        Vegetables = 1UL << 18, Cotton = 1UL << 19, Grain = 1UL << 20,
        Livestock = 1UL << 21, Oil = 1UL << 22, Ore = 1UL << 23,
        Coal = 1UL << 24, Stone = 1UL << 25
    }
}
namespace Game.Zones { public enum AreaType { Residential, Commercial, Industrial } }
namespace Mono.Cecil { }
namespace Unity.Entities.UniversalDelegates { }
