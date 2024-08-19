using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
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


            AssetDatabase.global.LoadSettings(nameof(RealisticWorkplacesAndHouseholds), m_Setting, new Setting(this));

            // Disable original systems
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.BuildingPollutionAddSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.UI.InGame.PollutionSection>().Enabled = false;

            updateSystem.UpdateAt<RWHBuildingPollutionAddSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CityServicesUpdateSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<WorkplaceUpdateSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<HouseholdUpdateSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<NoisePollutionParameterUpdaterSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<NoisePollutionParameterUpdaterSystem>(SystemUpdatePhase.PrefabReferences);
            //updateSystem.UpdateAfter<CheckBuildingsSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<ResetHouseholdsSystem>(SystemUpdatePhase.GameSimulation);

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
