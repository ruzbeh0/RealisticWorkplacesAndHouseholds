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
using Unity.Mathematics;

namespace RealisticWorkplacesAndHouseholds.Systems
{
    public partial class BuildingDataExportSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            // [로그 1] 버튼 신호 감지
            if (Mod.m_Setting.export_data_requested)
            {
                Mod.log.Info("[RWH Debug] Export button click detected in OnUpdate. Starting process...");

                try
                {
                    ExportAllPrefabsToCSV();
                }
                catch (Exception ex)
                {
                    Mod.log.Error($"[RWH Error] Critical error during export: {ex.Message}\n{ex.StackTrace}");
                }

                Mod.m_Setting.export_data_requested = false;
                Mod.log.Info("[RWH Debug] Export process finished. Flag reset.");
            }
        }

        private void ExportAllPrefabsToCSV()
        {
            Mod.log.Info("[RWH Debug] ExportAllPrefabsToCSV method started.");

            var prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // CSV 헤더 준비
            var resCsv = new StringBuilder("Name,Type,Height,Width,Length,Households,Signature,AssetPack\n");
            var workCsv = new StringBuilder("Name,Category,Height,Width,Length,Workers,Signature,AssetPack\n");
            var schoolCsv = new StringBuilder("Name,StudentCapacity,Workers,Height,AssetPack\n");
            var hospitalCsv = new StringBuilder("Name,PatientCapacity,Workers,Height,AssetPack\n");
            var prisonCsv = new StringBuilder("Name,PrisonerCapacity,Workers,Height,AssetPack\n");

            // 엔티티 쿼리
            EntityQuery prefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<BuildingData>());
            var prefabEntities = prefabQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            // [로그 2] 쿼리 결과 확인 (매우 중요: 0개면 여기서부터 문제임)
            Mod.log.Info($"[RWH Debug] Query found {prefabEntities.Length} prefabs. Preparing to loop...");

            // [로그 3] 반복문 시작 알림
            Mod.log.Info("[RWH Debug] Loop Start (Processing Prefabs...)");

            int processedCount = 0;

            foreach (var prefabEntity in prefabEntities)
            {
                // 반복문 내부에서는 로그를 찍지 않습니다 (속도 저하 및 스팸 방지)
                processedCount++;

                var prefabBase = prefabSystem.GetPrefab<BuildingPrefab>(prefabEntity);
                if (prefabBase == null) continue;

                string name = prefabBase.name;

                if (!EntityManager.TryGetComponent<BuildingData>(prefabEntity, out var bData)) continue;
                int w = bData.m_LotSize.x;
                int l = bData.m_LotSize.y;

                float height = 0;
                if (EntityManager.TryGetComponent<ObjectGeometryData>(prefabEntity, out var geomData))
                {
                    height = geomData.m_Size.y;
                }

                string assetPack = GetAssetPackName(prefabEntity, prefabSystem);
                bool isSignature = EntityManager.HasComponent<SignatureBuildingData>(prefabEntity);

                // 1. Residential
                if (EntityManager.TryGetComponent<BuildingPropertyData>(prefabEntity, out var prop) && prop.m_ResidentialProperties > 0)
                {
                    string resType = GetDetailedResidentialType(prefabEntity, height);
                    resCsv.AppendLine($"{name},{resType},{height:F2},{w},{l},{prop.m_ResidentialProperties},{isSignature},{assetPack}");
                }

                // 2. Workplace
                if (EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var workData))
                {
                    string workCat = GetDetailedWorkplaceCategory(prefabEntity);
                    workCsv.AppendLine($"{name},{workCat},{height:F2},{w},{l},{workData.m_MaxWorkers},{isSignature},{assetPack}");
                }

                // 3. School
                if (EntityManager.TryGetComponent<SchoolData>(prefabEntity, out var schoolData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var sWork) ? sWork.m_MaxWorkers : 0;
                    schoolCsv.AppendLine($"{name},{schoolData.m_StudentCapacity},{staff},{height:F2},{assetPack}");
                }

                // 4. Hospital
                if (EntityManager.TryGetComponent<HospitalData>(prefabEntity, out var medicalData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var hWork) ? hWork.m_MaxWorkers : 0;
                    hospitalCsv.AppendLine($"{name},{medicalData.m_PatientCapacity},{staff},{height:F2},{assetPack}");
                }

                // 5. Prison
                if (EntityManager.TryGetComponent<PrisonData>(prefabEntity, out var prisonData))
                {
                    int staff = EntityManager.TryGetComponent<WorkplaceData>(prefabEntity, out var pWork) ? pWork.m_MaxWorkers : 0;
                    prisonCsv.AppendLine($"{name},{prisonData.m_PrisonerCapacity},{staff},{height:F2},{assetPack}");
                }
            }

            // [로그 4] 반복문 종료 알림
            Mod.log.Info($"[RWH Debug] Loop End. Processed {processedCount} entities.");

            // 파일 저장 시도
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Mod.log.Info($"[RWH Debug] Attempting to save files to: {docPath}");

            SaveFile(Path.Combine(docPath, "RWH_All_Residential.csv"), resCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Workplaces.csv"), workCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Schools.csv"), schoolCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Hospitals.csv"), hospitalCsv);
            SaveFile(Path.Combine(docPath, "RWH_All_Prisons.csv"), prisonCsv);

            prefabEntities.Dispose();
        }

        // ... (아래 Helper 함수들은 기존과 동일, 변경 없음) ...
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

        // [로그 5] 파일 저장 결과 확인
        private void SaveFile(string path, StringBuilder content)
        {
            try
            {
                File.WriteAllText(path, content.ToString(), Encoding.UTF8);
                Mod.log.Info($"[RWH Debug] Saved file: {Path.GetFileName(path)}"); // 성공 로그
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[RWH Error] Failed to save {path}: {ex.Message}"); // 실패 로그
            }
        }
    }
}