using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using HarmonyLib;
using RealisticWorkplacesAndHouseholds.Jobs;
using RealisticWorkplacesAndHouseholds.Systems;
using RWH.Systems;
using System.IO;
using System.Linq;
using Unity.Entities;

namespace RealisticWorkplacesAndHouseholds
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(RealisticWorkplacesAndHouseholds)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting m_Setting;
        public static readonly string harmonyID = "RWH";

        // Mods Settings Folder
        public static string SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(RealisticWorkplacesAndHouseholds));
        readonly public static int kComponentVersion = 1;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            GameManager.instance.localizationManager.AddSource("pt-BR", new LocalePT(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(RealisticWorkplacesAndHouseholds), m_Setting, new Setting(this));

            // Disable original systems
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.HouseholdFindPropertySystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.HouseholdSpawnSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.RentAdjustSystem>().Enabled = false;

            if (!Mod.m_Setting.disable_households_calculations)
            {
                updateSystem.UpdateAt<HouseholdUpdateSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAfter<CheckBuildingsSystem>(SystemUpdatePhase.GameSimulation);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.ZoneSpawnSystem>().Enabled = false;
                updateSystem.UpdateAt<RWHZoneSpawnSystem>(SystemUpdatePhase.GameSimulation);
            } 
            updateSystem.UpdateAfter<NoisePollutionParameterUpdaterSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<NoisePollutionParameterUpdaterSystem>(SystemUpdatePhase.PrefabReferences);
            updateSystem.UpdateAfter<ConsumptionDataUpdaterSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<ConsumptionDataUpdaterSystem>(SystemUpdatePhase.PrefabReferences);

            if (!Mod.m_Setting.disable_workplace_calculations)
            {
                updateSystem.UpdateAt<WorkplaceUpdateSystem>(SystemUpdatePhase.GameSimulation);
            }
            
            if(!Mod.m_Setting.disable_cityservices_calculations)
            {
                updateSystem.UpdateAt<CityServicesWorkplaceUpdateSystem>(SystemUpdatePhase.GameSimulation);
                updateSystem.UpdateAt<CityServicesWorkproviderUpdateSystem>(SystemUpdatePhase.GameSimulation);
            }
            
            //updateSystem.UpdateAt<RWHHouseholdFindPropertySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ResetHouseholdsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ResidentialVacancySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<RWHHouseholdSpawnSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EconomyParameterUpdaterSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<RWHRentAdjustSystem>(SystemUpdatePhase.GameSimulation);

            //Harmony
            var harmony = new Harmony(harmonyID);
            //Harmony.DEBUG = true;
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}");
            }
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
