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
        private CitySystem _citySystem;
        private EntityQuery _taggedPrefabsQuery;
        private EntityQuery _taggedHouseholdPrefabsQuery;

        private bool _abcDetected;
        private bool _reflectionReady;
        private bool _reflectionFailedLogged;

        private Type _modifiedPrefabType;
        private Type _updateValueTypeEnum;

        private FieldInfo _valueTypeField;
        private FieldInfo _enabledField;
        private FieldInfo _modEntityField;

        private int _workplaceMaxWorkersEnumValue;
        private int _residentialPropertiesEnumValue;

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

            _abcDetected = IsABCInstalled();
            _reflectionReady = false;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Game)
            {
                TryInitReflection();
                RefreshTags();
            }
        }

        protected override void OnUpdate()
        {
            if (!_abcDetected)
                return;

            if (!_reflectionReady)
                TryInitReflection();

            if (_reflectionReady)
                RefreshTags();
        }

        private bool IsABCInstalled()
        {
            foreach (var modInfo in GameManager.instance.modManager)
            {
                if (modInfo.asset.name.Equals("AdvancedBuildingControl", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void TryInitReflection()
        {
            if (_reflectionReady || !_abcDetected)
                return;

            try
            {
                _modifiedPrefabType = Type.GetType("AdvancedBuildingControl.Components.ModifiedPrefab, AdvancedBuildingControl", false);
                _updateValueTypeEnum = Type.GetType("AdvancedBuildingControl.Variables.UpdateValueType, AdvancedBuildingControl", false);

                if (_modifiedPrefabType == null || _updateValueTypeEnum == null)
                    return;

                _valueTypeField = _modifiedPrefabType.GetField("ValueType", BindingFlags.Public | BindingFlags.Instance);
                _enabledField = _modifiedPrefabType.GetField("Enabled", BindingFlags.Public | BindingFlags.Instance);
                _modEntityField = _modifiedPrefabType.GetField("ModEntity", BindingFlags.Public | BindingFlags.Instance);

                if (_valueTypeField == null || _enabledField == null || _modEntityField == null)
                    return;

                object workplaceEnumValue = Enum.Parse(_updateValueTypeEnum, "WorkplaceData_MaxWorkers");
                _workplaceMaxWorkersEnumValue = Convert.ToInt32(workplaceEnumValue);

                object householdEnumValue = Enum.Parse(_updateValueTypeEnum, "BuildingPropertyData_ResidentialProperties");
                _residentialPropertiesEnumValue = Convert.ToInt32(householdEnumValue);

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

        private void RefreshTags()
        {
            ClearExistingTags();

            Entity cityEntity = _citySystem.City;
            if (cityEntity == Entity.Null)
                return;

            if (!_reflectionReady || _modifiedPrefabType == null)
                return;

            try
            {
                MethodInfo hasBufferMethod = typeof(EntityManager)
                    .GetMethod(nameof(EntityManager.HasBuffer), BindingFlags.Public | BindingFlags.Instance)?
                    .MakeGenericMethod(_modifiedPrefabType);

                MethodInfo getBufferMethod = typeof(EntityManager)
                    .GetMethod(nameof(EntityManager.GetBuffer), new[] { typeof(Entity), typeof(bool) })?
                    .MakeGenericMethod(_modifiedPrefabType);

                if (hasBufferMethod == null || getBufferMethod == null)
                    return;

                bool cityHasBuffer = (bool)hasBufferMethod.Invoke(EntityManager, new object[] { cityEntity });
                if (!cityHasBuffer)
                    return;

                object reflectedBuffer = getBufferMethod.Invoke(EntityManager, new object[] { cityEntity, true });
                if (reflectedBuffer == null)
                    return;

                Type reflectedBufferType = reflectedBuffer.GetType();
                PropertyInfo lengthProperty = reflectedBufferType.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo itemProperty = reflectedBufferType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);

                if (lengthProperty == null || itemProperty == null)
                    return;

                int reflectedLength = (int)lengthProperty.GetValue(reflectedBuffer);

                for (int bufferIndex = 0; bufferIndex < reflectedLength; bufferIndex++)
                {
                    object reflectedEntry = itemProperty.GetValue(reflectedBuffer, new object[] { bufferIndex });
                    if (reflectedEntry == null)
                        continue;

                    byte reflectedEnabledRaw = Convert.ToByte(_enabledField.GetValue(reflectedEntry));
                    if (reflectedEnabledRaw == 0)
                        continue;

                    int reflectedValueType = Convert.ToInt32(_valueTypeField.GetValue(reflectedEntry));
                    if (reflectedValueType != _workplaceMaxWorkersEnumValue &&
                        reflectedValueType != _residentialPropertiesEnumValue)
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
                    }
                    else if (reflectedValueType == _residentialPropertiesEnumValue)
                    {
                        if (!EntityManager.HasComponent<ABCHouseholdOverride>(targetPrefab))
                            EntityManager.AddComponent<ABCHouseholdOverride>(targetPrefab);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_reflectionFailedLogged)
                {
                    Mod.log.Warn($"[RWH] Failed while refreshing ABC compatibility tags: {ex}");
                    _reflectionFailedLogged = true;
                }
            }
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