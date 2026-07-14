using Colossal.Entities;
using Game;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using System.Collections;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    [Preserve]
    public partial class ABCCompatibilitySystem : GameSystemBase
    {
        private const int kCompatibilityRefreshInterval = 4096;

        private CitySystem _citySystem;
        private EntityQuery _taggedPrefabsQuery;
        private EntityQuery _taggedHouseholdPrefabsQuery;
        private EntityQuery _workProviderQuery;

        private bool _abcDetected;
        private bool _reflectionReady;
        private bool _reflectionFailedLogged;
        private bool _waitingForABCLogged;

        private Type _modifiedPrefabType;
        private Type _updateValueTypeEnum;

        private FieldInfo _valueTypeField;
        private FieldInfo _enabledField;
        private FieldInfo _modifiedField;
        private FieldInfo _modEntityField;
        private MethodInfo _hasModifiedPrefabBufferMethod;
        private MethodInfo _getModifiedPrefabBufferMethod;
        private PropertyInfo _bufferLengthProperty;
        private PropertyInfo _bufferItemProperty;

        private int _workplaceMaxWorkersEnumValue;
        private int _residentialPropertiesEnumValue;
        private int _lastAppliedSignature;
        private bool _tagsApplied;

        protected override void OnCreate()
        {
            base.OnCreate();

            _citySystem = World.GetOrCreateSystemManaged<CitySystem>();

            _taggedPrefabsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ABCWorkplaceOverride>() },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });

            _taggedHouseholdPrefabsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ABCHouseholdOverride>() },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });

            _workProviderQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<Game.Companies.WorkProvider>() },
                None = new[]
                {
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>()
                }
            });

            _abcDetected = IsABCInstalled();
            _reflectionReady = false;
            if (!_abcDetected)
                Enabled = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Game)
            {
                if (!_abcDetected)
                    return;

                TryInitReflection();
                RefreshTagsIfChanged(true);
            }
        }

        protected override void OnUpdate()
        {
            if (!_abcDetected)
            {
                Enabled = false;
                return;
            }

            if (!_reflectionReady)
                TryInitReflection();

            if (_reflectionReady)
                RefreshTagsIfChanged(false);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return kCompatibilityRefreshInterval;
        }

        private bool IsABCInstalled()
        {
            foreach (var modInfo in GameManager.instance.modManager)
            {
                string assetName = modInfo.asset.name;
                if (assetName.Equals("AdvancedBuildingControl", StringComparison.OrdinalIgnoreCase) ||
                    assetName.Equals("Advanced Building Control", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return TryResolveType("AdvancedBuildingControl.Components.ModifiedPrefab") != null;
        }

        private static Type TryResolveType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, AdvancedBuildingControl", false);
            if (type != null)
                return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.Equals("AdvancedBuildingControl", StringComparison.OrdinalIgnoreCase))
                    continue;

                type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private void TryInitReflection()
        {
            if (_reflectionReady)
                return;

            try
            {
                _modifiedPrefabType = TryResolveType("AdvancedBuildingControl.Components.ModifiedPrefab");
                _updateValueTypeEnum = TryResolveType("AdvancedBuildingControl.Variables.UpdateValueType");

                if (_modifiedPrefabType == null || _updateValueTypeEnum == null)
                {
                    if (_abcDetected && !_waitingForABCLogged)
                    {
                        Mod.log.Info("[RWH] Waiting for Advanced Building Control types to load.");
                        _waitingForABCLogged = true;
                    }

                    return;
                }

                _valueTypeField = _modifiedPrefabType.GetField("ValueType", BindingFlags.Public | BindingFlags.Instance);
                _enabledField = _modifiedPrefabType.GetField("Enabled", BindingFlags.Public | BindingFlags.Instance);
                _modifiedField = _modifiedPrefabType.GetField("Modified", BindingFlags.Public | BindingFlags.Instance);
                _modEntityField = _modifiedPrefabType.GetField("ModEntity", BindingFlags.Public | BindingFlags.Instance);

                if (_valueTypeField == null || _enabledField == null || _modifiedField == null || _modEntityField == null)
                    return;

                _hasModifiedPrefabBufferMethod = typeof(EntityManager)
                    .GetMethod(nameof(EntityManager.HasBuffer), BindingFlags.Public | BindingFlags.Instance)?
                    .MakeGenericMethod(_modifiedPrefabType);

                _getModifiedPrefabBufferMethod = typeof(EntityManager)
                    .GetMethod(nameof(EntityManager.GetBuffer), new[] { typeof(Entity), typeof(bool) })?
                    .MakeGenericMethod(_modifiedPrefabType);

                if (_hasModifiedPrefabBufferMethod == null || _getModifiedPrefabBufferMethod == null)
                    return;

                object workplaceEnumValue = Enum.Parse(_updateValueTypeEnum, "WorkplaceData_MaxWorkers");
                _workplaceMaxWorkersEnumValue = Convert.ToInt32(workplaceEnumValue);

                object householdEnumValue = Enum.Parse(_updateValueTypeEnum, "BuildingPropertyData_ResidentialProperties");
                _residentialPropertiesEnumValue = Convert.ToInt32(householdEnumValue);

                _abcDetected = true;
                _reflectionReady = true;
                Mod.log.Info("[RWH] ABC compatibility reflection initialized.");
            }
            catch (Exception ex)
            {
                if (!_reflectionFailedLogged)
                {
                    Mod.log.Warn($"[RWH] Failed to initialize ABC compatibility reflection: {ex}");
                    _reflectionFailedLogged = true;
                }
            }
        }

        private void RefreshTagsIfChanged(bool force)
        {
            int signature = GetABCModificationSignature();
            if (!force && _tagsApplied && signature == _lastAppliedSignature)
                return;

            if (RefreshTags())
            {
                _lastAppliedSignature = signature;
                _tagsApplied = true;
            }
        }

        private int GetABCModificationSignature()
        {
            try
            {
                if (!TryGetModifiedPrefabBuffer(out object reflectedBuffer, out int reflectedLength, out PropertyInfo itemProperty))
                    return 0;

                unchecked
                {
                    int hash = 17;
                    int relevantCount = 0;

                    for (int bufferIndex = 0; bufferIndex < reflectedLength; bufferIndex++)
                    {
                        object reflectedEntry = itemProperty.GetValue(reflectedBuffer, new object[] { bufferIndex });
                        if (reflectedEntry == null)
                            continue;

                        byte reflectedEnabledRaw = Convert.ToByte(_enabledField.GetValue(reflectedEntry));
                        if (reflectedEnabledRaw == 0)
                            continue;

                        int reflectedValueType = Convert.ToInt32(_valueTypeField.GetValue(reflectedEntry));
                        if (!IsRelevantValueType(reflectedValueType))
                            continue;

                        Entity reflectedModEntity = Entity.Null;
                        object reflectedModEntityObj = _modEntityField.GetValue(reflectedEntry);
                        if (reflectedModEntityObj is Entity entity)
                            reflectedModEntity = entity;

                        long reflectedModified = Convert.ToInt64(_modifiedField.GetValue(reflectedEntry));
                        relevantCount++;
                        hash = hash * 31 + reflectedValueType;
                        hash = hash * 31 + reflectedModEntity.Index;
                        hash = hash * 31 + reflectedModEntity.Version;
                        hash = hash * 31 + reflectedModified.GetHashCode();
                    }

                    return hash * 31 + relevantCount;
                }
            }
            catch (Exception ex)
            {
                if (!_reflectionFailedLogged)
                {
                    Mod.log.Warn($"[RWH] Failed while checking ABC compatibility state: {ex}");
                    _reflectionFailedLogged = true;
                }

                return _lastAppliedSignature;
            }
        }

        private bool TryGetModifiedPrefabBuffer(out object reflectedBuffer, out int reflectedLength, out PropertyInfo itemProperty)
        {
            reflectedBuffer = null;
            reflectedLength = 0;
            itemProperty = null;

            Entity cityEntity = _citySystem.City;
            if (cityEntity == Entity.Null || !_reflectionReady || _modifiedPrefabType == null)
                return false;

            if (_hasModifiedPrefabBufferMethod == null || _getModifiedPrefabBufferMethod == null)
                return false;

            bool cityHasBuffer = (bool)_hasModifiedPrefabBufferMethod.Invoke(EntityManager, new object[] { cityEntity });
            if (!cityHasBuffer)
                return false;

            reflectedBuffer = _getModifiedPrefabBufferMethod.Invoke(EntityManager, new object[] { cityEntity, true });
            if (reflectedBuffer == null)
                return false;

            Type reflectedBufferType = reflectedBuffer.GetType();
            if (_bufferLengthProperty == null || _bufferItemProperty == null)
            {
                _bufferLengthProperty = reflectedBufferType.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
                _bufferItemProperty = reflectedBufferType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            }

            if (_bufferLengthProperty == null || _bufferItemProperty == null)
                return false;

            reflectedLength = (int)_bufferLengthProperty.GetValue(reflectedBuffer);
            itemProperty = _bufferItemProperty;
            return true;
        }

        private bool IsRelevantValueType(int reflectedValueType)
        {
            return reflectedValueType == _workplaceMaxWorkersEnumValue ||
                   reflectedValueType == _residentialPropertiesEnumValue;
        }

        private bool RefreshTags()
        {
            ClearExistingTags();

            if (!_reflectionReady || _modifiedPrefabType == null)
                return true;

            try
            {
                if (!TryGetModifiedPrefabBuffer(out object reflectedBuffer, out int reflectedLength, out PropertyInfo itemProperty))
                    return true;

                for (int bufferIndex = 0; bufferIndex < reflectedLength; bufferIndex++)
                {
                    object reflectedEntry = itemProperty.GetValue(reflectedBuffer, new object[] { bufferIndex });
                    if (reflectedEntry == null)
                        continue;

                    byte reflectedEnabledRaw = Convert.ToByte(_enabledField.GetValue(reflectedEntry));
                    if (reflectedEnabledRaw == 0)
                        continue;

                    int reflectedValueType = Convert.ToInt32(_valueTypeField.GetValue(reflectedEntry));
                    if (!IsRelevantValueType(reflectedValueType))
                        continue;

                    object reflectedModEntityObj = _modEntityField.GetValue(reflectedEntry);
                    if (reflectedModEntityObj == null)
                        continue;

                    Entity reflectedModEntity = (Entity)reflectedModEntityObj;
                    if (reflectedModEntity == Entity.Null || !EntityManager.Exists(reflectedModEntity))
                        continue;

                    if (!EntityManager.TryGetComponent(reflectedModEntity, out PrefabRef reflectedPrefabRef))
                        continue;

                    Entity targetPrefab = reflectedPrefabRef.m_Prefab;
                    if (targetPrefab == Entity.Null || !EntityManager.Exists(targetPrefab))
                        continue;

                    if (reflectedValueType == _workplaceMaxWorkersEnumValue)
                    {
                        if (!EntityManager.HasComponent<ABCWorkplaceOverride>(targetPrefab))
                            EntityManager.AddComponent<ABCWorkplaceOverride>(targetPrefab);

                        int maxWorkers = ToInt32Saturated(Convert.ToInt64(_modifiedField.GetValue(reflectedEntry)));
                        ApplyWorkProviderOverride(targetPrefab, maxWorkers);
                    }
                    else if (reflectedValueType == _residentialPropertiesEnumValue)
                    {
                        if (!EntityManager.HasComponent<ABCHouseholdOverride>(targetPrefab))
                            EntityManager.AddComponent<ABCHouseholdOverride>(targetPrefab);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!_reflectionFailedLogged)
                {
                    Mod.log.Warn($"[RWH] Failed while refreshing ABC compatibility tags: {ex}");
                    _reflectionFailedLogged = true;
                }

                return false;
            }
        }

        private void ApplyWorkProviderOverride(Entity targetPrefab, int maxWorkers)
        {
            using var providers = _workProviderQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < providers.Length; i++)
            {
                Entity providerEntity = providers[i];
                if (!IsWorkProviderForPrefab(providerEntity, targetPrefab))
                    continue;

                Game.Companies.WorkProvider workProvider = EntityManager.GetComponentData<Game.Companies.WorkProvider>(providerEntity);
                if (workProvider.m_MaxWorkers == maxWorkers)
                    continue;

                workProvider.m_MaxWorkers = maxWorkers;
                EntityManager.SetComponentData(providerEntity, workProvider);

                if (EntityManager.HasComponent<RealisticWorkplaceData>(providerEntity))
                {
                    RealisticWorkplaceData realisticData = EntityManager.GetComponentData<RealisticWorkplaceData>(providerEntity);
                    realisticData.max_workers = maxWorkers;
                    EntityManager.SetComponentData(providerEntity, realisticData);
                }

                if (!EntityManager.HasComponent<Updated>(providerEntity))
                    EntityManager.AddComponent<Updated>(providerEntity);
            }
        }

        private bool IsWorkProviderForPrefab(Entity providerEntity, Entity targetPrefab)
        {
            if (EntityManager.TryGetComponent(providerEntity, out PrefabRef providerPrefabRef) &&
                providerPrefabRef.m_Prefab == targetPrefab)
            {
                return true;
            }

            if (!EntityManager.TryGetComponent(providerEntity, out Game.Buildings.PropertyRenter propertyRenter))
                return false;

            return EntityManager.TryGetComponent(propertyRenter.m_Property, out PrefabRef propertyPrefabRef) &&
                propertyPrefabRef.m_Prefab == targetPrefab;
        }

        private static int ToInt32Saturated(long value)
        {
            if (value > int.MaxValue)
                return int.MaxValue;
            if (value < int.MinValue)
                return int.MinValue;
            return (int)value;
        }

        private void ClearExistingTags()
        {
            using (var tagged = _taggedPrefabsQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < tagged.Length; i++)
                {
                    if (EntityManager.HasComponent<ABCWorkplaceOverride>(tagged[i]))
                        EntityManager.RemoveComponent<ABCWorkplaceOverride>(tagged[i]);
                }
            }

            using (var taggedHouseholds = _taggedHouseholdPrefabsQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < taggedHouseholds.Length; i++)
                {
                    if (EntityManager.HasComponent<ABCHouseholdOverride>(taggedHouseholds[i]))
                        EntityManager.RemoveComponent<ABCHouseholdOverride>(taggedHouseholds[i]);
                }
            }
        }
    }
}
