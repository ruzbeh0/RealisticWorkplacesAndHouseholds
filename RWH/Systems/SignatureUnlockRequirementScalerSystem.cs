using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class SignatureUnlockRequirementScalerSystem : GameSystemBase
    {
        private enum ZoneFamily
        {
            None,
            Residential,
            Mixed,
            Commercial,
            Office,
            Industrial
        }

        private enum ZoneDensity
        {
            Low,
            Medium,
            High
        }

        private EntityQuery m_SignaturePrefabQuery;
        private EntityQuery m_OriginalRequirementQuery;
        private PrefabSystem m_PrefabSystem;
        private bool m_Applied;
        private int m_LastSettingsHash;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SignaturePrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabData>(),
                    ComponentType.ReadOnly<SignatureBuildingData>(),
                    ComponentType.ReadOnly<UnlockRequirement>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>()
                }
            });
            m_OriginalRequirementQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZoneBuiltRequirementData>(),
                    ComponentType.ReadOnly<SignatureUnlockRequirementOriginalData>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>()
                }
            });

            RequireForUpdate(m_SignaturePrefabQuery);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_Applied = false;
            m_LastSettingsHash = 0;
        }

        protected override void OnUpdate()
        {
            if (Mod.m_Setting == null)
                return;

            int settingsHash = GetSettingsHash();
            if (m_Applied && settingsHash == m_LastSettingsHash)
                return;

            bool allScalesDefault = AreAllScalesDefault();
            if (allScalesDefault && m_OriginalRequirementQuery.IsEmptyIgnoreFilter)
            {
                m_Applied = true;
                m_LastSettingsHash = settingsHash;
                return;
            }

            m_Applied = true;
            m_LastSettingsHash = settingsHash;

            HashSet<Entity> processedRequirements = new HashSet<Entity>();
            List<Entity> zoneBuiltRequirements = new List<Entity>();

            using NativeArray<Entity> signaturePrefabs = m_SignaturePrefabQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < signaturePrefabs.Length; i++)
            {
                DynamicBuffer<UnlockRequirement> requirements = EntityManager.GetBuffer<UnlockRequirement>(signaturePrefabs[i], true);
                for (int j = 0; j < requirements.Length; j++)
                {
                    Entity requirement = requirements[j].m_Prefab;
                    if (!processedRequirements.Add(requirement))
                        continue;

                    if (!EntityManager.HasComponent<ZoneBuiltRequirementData>(requirement))
                        continue;

                    zoneBuiltRequirements.Add(requirement);
                }
            }

            for (int i = 0; i < zoneBuiltRequirements.Count; i++)
            {
                Entity requirement = zoneBuiltRequirements[i];
                if (!EntityManager.HasComponent<ZoneBuiltRequirementData>(requirement))
                    continue;

                ZoneBuiltRequirementData requirementData = EntityManager.GetComponentData<ZoneBuiltRequirementData>(requirement);
                if (!TryGetScale(requirementData, out int scale, out _))
                {
                    continue;
                }

                bool hasOriginal = EntityManager.HasComponent<SignatureUnlockRequirementOriginalData>(requirement);
                if (!hasOriginal && scale == 100)
                    continue;

                if (!hasOriginal)
                {
                    EntityManager.AddComponentData(requirement, new SignatureUnlockRequirementOriginalData
                    {
                        MinimumSquares = requirementData.m_MinimumSquares,
                        MinimumCount = requirementData.m_MinimumCount
                    });
                }

                SignatureUnlockRequirementOriginalData original = EntityManager.GetComponentData<SignatureUnlockRequirementOriginalData>(requirement);
                ZoneBuiltRequirementData scaledRequirement = requirementData;
                scaledRequirement.m_MinimumSquares = ScaleThreshold(original.MinimumSquares, scale);
                scaledRequirement.m_MinimumCount = ScaleThreshold(original.MinimumCount, scale);

                if (scaledRequirement.m_MinimumSquares == requirementData.m_MinimumSquares &&
                    scaledRequirement.m_MinimumCount == requirementData.m_MinimumCount)
                {
                    continue;
                }

                EntityManager.SetComponentData(requirement, scaledRequirement);
            }
        }

        private bool TryGetScale(ZoneBuiltRequirementData requirement, out int scale, out string category)
        {
            ZoneFamily family = ZoneFamily.None;
            ZoneDensity density = ZoneDensity.Medium;
            category = "Unknown";

            if (requirement.m_RequiredZone != Entity.Null)
            {
                if (!EntityManager.HasComponent<ZoneData>(requirement.m_RequiredZone))
                {
                    scale = 100;
                    return false;
                }

                ZoneData zoneData = EntityManager.GetComponentData<ZoneData>(requirement.m_RequiredZone);
                bool hasProperties = EntityManager.HasComponent<ZonePropertiesData>(requirement.m_RequiredZone);
                ZonePropertiesData properties = hasProperties ? EntityManager.GetComponentData<ZonePropertiesData>(requirement.m_RequiredZone) : default;
                string zoneName = GetPrefabNameSafe(requirement.m_RequiredZone);

                family = GetFamily(zoneName, zoneData, hasProperties, properties);
                density = GetDensity(zoneName, zoneData, hasProperties, properties, family);
                category = $"{family}/{density}/{zoneName}";
            }
            else if (requirement.m_RequiredType != AreaType.None)
            {
                family = GetFamily(requirement.m_RequiredType);
                density = ZoneDensity.Medium;
                category = $"{family}/{density}/area-wide";
            }

            if (family == ZoneFamily.None)
            {
                scale = 100;
                return false;
            }

            scale = GetScale(family, density);
            return true;
        }

        private ZoneFamily GetFamily(string zoneName, ZoneData zoneData, bool hasProperties, ZonePropertiesData properties)
        {
            if (ContainsIgnoreCase(zoneName, "mixed") ||
                (zoneData.m_AreaType == AreaType.Residential && hasProperties && properties.m_AllowedSold != Resource.NoResource))
            {
                return ZoneFamily.Mixed;
            }

            if ((zoneData.m_ZoneFlags & ZoneFlags.Office) != 0 || ContainsIgnoreCase(zoneName, "office"))
            {
                return ZoneFamily.Office;
            }

            return GetFamily(zoneData.m_AreaType);
        }

        private static ZoneFamily GetFamily(AreaType areaType)
        {
            switch (areaType)
            {
                case AreaType.Residential:
                    return ZoneFamily.Residential;
                case AreaType.Commercial:
                    return ZoneFamily.Commercial;
                case AreaType.Industrial:
                    return ZoneFamily.Industrial;
                default:
                    return ZoneFamily.None;
            }
        }

        private ZoneDensity GetDensity(string zoneName, ZoneData zoneData, bool hasProperties, ZonePropertiesData properties, ZoneFamily family)
        {
            if (family == ZoneFamily.Industrial)
                return ZoneDensity.Medium;

            if (ContainsIgnoreCase(zoneName, "row"))
                return ZoneDensity.Medium;

            if ((family == ZoneFamily.Residential || family == ZoneFamily.Mixed) && hasProperties)
            {
                if (!properties.m_ScaleResidentials)
                    return ZoneDensity.Low;

                return properties.m_ResidentialProperties > properties.m_SpaceMultiplier ? ZoneDensity.High : ZoneDensity.Medium;
            }

            if (ContainsIgnoreCase(zoneName, "high"))
                return ZoneDensity.High;

            if (ContainsIgnoreCase(zoneName, "medium") || ContainsIgnoreCase(zoneName, "med"))
                return ZoneDensity.Medium;

            if (ContainsIgnoreCase(zoneName, "low"))
                return ZoneDensity.Low;

            if (zoneData.m_MaxHeight > 0)
            {
                if (zoneData.m_MaxHeight <= 24)
                    return ZoneDensity.Low;

                if (zoneData.m_MaxHeight >= 60)
                    return ZoneDensity.High;
            }

            return ZoneDensity.Medium;
        }

        private int GetScale(ZoneFamily family, ZoneDensity density)
        {
            Setting setting = Mod.m_Setting;

            switch (family)
            {
                case ZoneFamily.Residential:
                    return ClampScale(density == ZoneDensity.Low
                        ? setting.signature_residential_low_requirement_scale
                        : density == ZoneDensity.High
                            ? setting.signature_residential_high_requirement_scale
                            : setting.signature_residential_medium_requirement_scale);
                case ZoneFamily.Mixed:
                    return ClampScale(density == ZoneDensity.Low
                        ? setting.signature_mixed_low_requirement_scale
                        : density == ZoneDensity.High
                            ? setting.signature_mixed_high_requirement_scale
                            : setting.signature_mixed_medium_requirement_scale);
                case ZoneFamily.Commercial:
                    return ClampScale(density == ZoneDensity.Low
                        ? setting.signature_commercial_low_requirement_scale
                        : density == ZoneDensity.High
                            ? setting.signature_commercial_high_requirement_scale
                            : setting.signature_commercial_medium_requirement_scale);
                case ZoneFamily.Office:
                    return ClampScale(density == ZoneDensity.Low
                        ? setting.signature_office_low_requirement_scale
                        : density == ZoneDensity.High
                            ? setting.signature_office_high_requirement_scale
                            : setting.signature_office_medium_requirement_scale);
                case ZoneFamily.Industrial:
                    return ClampScale(setting.signature_industrial_requirement_scale);
                default:
                    return 100;
            }
        }

        private int GetSettingsHash()
        {
            Setting setting = Mod.m_Setting;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ClampScale(setting.signature_residential_low_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_residential_medium_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_residential_high_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_mixed_low_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_mixed_medium_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_mixed_high_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_commercial_low_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_commercial_medium_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_commercial_high_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_office_low_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_office_medium_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_office_high_requirement_scale);
                hash = hash * 31 + ClampScale(setting.signature_industrial_requirement_scale);
                return hash;
            }
        }

        private bool AreAllScalesDefault()
        {
            Setting setting = Mod.m_Setting;
            return ClampScale(setting.signature_residential_low_requirement_scale) == 100 &&
                   ClampScale(setting.signature_residential_medium_requirement_scale) == 100 &&
                   ClampScale(setting.signature_residential_high_requirement_scale) == 100 &&
                   ClampScale(setting.signature_mixed_low_requirement_scale) == 100 &&
                   ClampScale(setting.signature_mixed_medium_requirement_scale) == 100 &&
                   ClampScale(setting.signature_mixed_high_requirement_scale) == 100 &&
                   ClampScale(setting.signature_commercial_low_requirement_scale) == 100 &&
                   ClampScale(setting.signature_commercial_medium_requirement_scale) == 100 &&
                   ClampScale(setting.signature_commercial_high_requirement_scale) == 100 &&
                   ClampScale(setting.signature_office_low_requirement_scale) == 100 &&
                   ClampScale(setting.signature_office_medium_requirement_scale) == 100 &&
                   ClampScale(setting.signature_office_high_requirement_scale) == 100 &&
                   ClampScale(setting.signature_industrial_requirement_scale) == 100;
        }

        private static int ScaleThreshold(int original, int scale)
        {
            if (original <= 0)
                return 0;

            return Math.Max(1, (int)Math.Round(original * (ClampScale(scale) / 100f), MidpointRounding.AwayFromZero));
        }

        private static int ClampScale(int value)
        {
            if (value < 1)
                return 1;

            if (value > 100)
                return 100;

            return value;
        }

        private string GetPrefabNameSafe(Entity entity)
        {
            try
            {
                if (entity != Entity.Null && EntityManager.HasComponent<PrefabData>(entity))
                    return m_PrefabSystem.GetPrefabName(entity);
            }
            catch
            {
            }

            return entity == Entity.Null ? "None" : entity.ToString();
        }

        private static bool ContainsIgnoreCase(string value, string pattern)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
