using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using RealisticWorkplacesAndHouseholds.Jobs;
using RealisticWorkplacesAndHouseholds.Systems;
using System.IO;

namespace RealisticWorkplacesAndHouseholds
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(RealisticWorkplacesAndHouseholds)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting m_Setting;

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

            updateSystem.UpdateAt<SchoolUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<HospitalUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PowerPlantUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<AdminBuildingUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WelfareOfficeUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PoliceStationUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<FireStationUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PublicTransportStationUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkplaceUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<HouseholdUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<CheckBuildingsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ResetHouseholdsSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
