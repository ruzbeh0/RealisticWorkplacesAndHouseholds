using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using RealisticWorkplacesAndHouseholds.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Entities;
using Unity.Mathematics; // float3 사용을 위해 필수

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class BuildingDataExportSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            // 설정창 버튼이 눌렸는지 확인
            if (Mod.m_Setting.export_data_requested)
            {
                ExportAllPrefabsToCSV();
                // 다시 false로 돌려놓아 중복 실행 방지
                Mod.m_Setting.export_data_requested = false;
            }
        }

        private void ExportAllPrefabsToCSV()
        {
            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // CSV 헤더 설정 (Signature 열 포함)
            var resCsv = new StringBuilder("Name,Type,Height,Width,Length,Households,Signature,AssetPack\n");
            var workCsv = new StringBuilder("Name,Category,Height,Width,Length,Workers,Signature,AssetPack\n");
            var schoolCsv = new StringBuilder("Name,StudentCapacity,Workers,Height,AssetPack\n");
            var hospitalCsv = new StringBuilder("Name,PatientCapacity,Workers,Height,AssetPack\n");
            var prisonCsv = new StringBuilder("Name,PrisonerCapacity,Workers,Height,AssetPack\n");

            // 게임 내 모든 BuildingData를 가진 프리팹 쿼리 (전수 조사)
            EntityQuery prefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<BuildingData>());
            var prefabEntities = prefabQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var prefabEntity in prefabEntities)
            {
                var prefabBase = prefabSystem.GetPrefab<BuildingPrefab>(prefabEntity);
                if (prefabBase == null) continue;

                string name = prefabBase.name;

                // 1. 로트 크기 (BuildingData)
                if (!EntityManager.TryGetComponent<BuildingData>(prefabEntity, out var bData)) continue;
                int w = bData.m_LotSize.x;
                int l = bData.m_LotSize.y;

                // 2. 건물 높이 (ObjectGeometryData에서 가져옴 - 수정됨)
                float height = 0;
                if (EntityManager.TryGetComponent<ObjectGeometryData>(prefabEntity, out var geomData))
                {
                    height = geomData.m_Size.y;
                }

                string assetPack = GetAssetPackName(prefabEntity, prefabSystem);
                bool isSignature = EntityManager.HasComponent<SignatureBuildingData>(prefabEntity);

                // --- 1. Residential & Mixed-Use ---
                if (EntityManager.TryGetComponent<BuildingPropertyData>(prefabEntity, out var prop) && prop.m_ResidentialProperties > 0)
                {
                    string resType = GetDetailedResidentialType(prefabEntity, height);
                    resCsv.AppendLine($"{name},{resType},{height:F2},{w},{l},{prop.m_ResidentialProperties},{isSignature},{assetPack}");
                }

                // --- 2. Workplace ---
                if (EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var workData))
                {
                    string workCat = GetDetailedWorkplaceCategory(prefabEntity);
                    workCsv.AppendLine($"{name},{workCat},{height:F2},{w},{l},{workData.m_MaxWorkers},{isSignature},{assetPack}");
                }

                // --- 3. School ---
                if (EntityManager.TryGetComponent<SchoolData>(prefabEntity, out var schoolData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var sWork) ? sWork.m_MaxWorkers : 0;
                    schoolCsv.AppendLine($"{name},{schoolData.m_StudentCapacity},{staff},{height:F2},{assetPack}");
                }

                // --- 4. Hospital (HospitalData 사용 - 수정됨) ---
                if (EntityManager.TryGetComponent<HospitalData>(prefabEntity, out var medicalData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var hWork) ? hWork.m_MaxWorkers : 0;
                    hospitalCsv.AppendLine($"{name},{medicalData.m_PatientCapacity},{staff},{height:F2},{assetPack}");
                }

                // --- 5. Prison ---
                if (EntityManager.TryGetComponent<PrisonData>(prefabEntity, out var prisonData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var pWork) ? pWork.m_MaxWorkers : 0;
                    prisonCsv.AppendLine($"{name},{prisonData.m_PrisonerCapacity},{staff},{height:F2},{assetPack}");
                }
            }

            // 내 문서 폴더에 저장
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            SaveFile(Path.Combine(docPath, "RWH_All_Residential.csv"), resCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Workplaces.csv"), workCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Schools.csv"), schoolCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Hospitals.csv"), hospitalCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Prisons.csv"), prisonCsv);

            Mod.log.Info($"[RWH] Export Complete. 5 files saved to Documents.");
            prefabEntities.Dispose();
        }

        private string GetDetailedResidentialType(Entity entity, float height)
        {
            if (TryGetZoneData(entity, out var zoneData, out var ambienceData))
            {
                if (ambienceData.m_AmbienceType == GroupAmbienceType.ResidentialMixed) return "MixedUse";

                if (zoneData.m_AreaType == AreaType.Residential)
                {
                    if ((zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) != 0) return "RowHome";
                    if (height > 50) return "HighDensity";
                    if (height > 20) return "MediumDensity";
                    return "LowDensity";
                }
            }
            return "OtherResidential";
        }

        private string GetDetailedWorkplaceCategory(Entity entity)
        {
            if (TryGetZoneData(entity, out var zoneData, out _))
            {
                if (zoneData.m_AreaType == AreaType.Commercial) return "Commercial";
                if (zoneData.m_AreaType == AreaType.Industrial)
                {
                    if (EntityManager.TryGetComponent<SpawnableBuildingData>(entity, out var spawnData))
                    {
                        var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
                        var zonePrefab = prefabSystem.GetPrefab<ZonePrefab>(spawnData.m_ZonePrefab);
                        if (zonePrefab != null && zonePrefab.m_Office) return "Office";
                    }
                    if (EntityManager.HasComponent<ExtractorFacilityData>(entity)) return "Specialized/Extractor";
                    return "Industrial";
                }
            }
            return "Service/Public";
        }

        private bool TryGetZoneData(Entity entity, out ZoneData zoneData, out GroupAmbienceData ambienceData)
        {
            zoneData = default;
            ambienceData = default;

            if (EntityManager.TryGetComponent<SpawnableBuildingData>(entity, out var spawnData))
            {
                if (EntityManager.TryGetComponent(spawnData.m_ZonePrefab, out zoneData) &&
                    EntityManager.TryGetComponent(spawnData.m_ZonePrefab, out ambienceData))
                    return true;
            }
            else if (EntityManager.TryGetComponent<PlaceholderBuildingData>(entity, out var placeholderData))
            {
                if (EntityManager.TryGetComponent(placeholderData.m_ZonePrefab, out zoneData) &&
                    EntityManager.TryGetComponent(placeholderData.m_ZonePrefab, out ambienceData))
                    return true;
            }
            return false;
        }

        private void SaveFile(string path, StringBuilder content)
        {
            try { File.WriteAllText(path, content.ToString(), Encoding.UTF8); }
            catch (Exception ex) { Mod.log.Error($"Failed to save {path}: {ex.Message}"); }
        }

        private string GetAssetPackName(Entity prefab, PrefabSystem prefabSystem)
        {
            if (EntityManager.HasBuffer<AssetPackElement>(prefab))
            {
                var packs = EntityManager.GetBuffer<AssetPackElement>(prefab);
                if (packs.Length > 0)
                    return prefabSystem.GetPrefab<PrefabBase>(packs[0].m_Pack).name;
            }
            return "Default";
        }
    }
}