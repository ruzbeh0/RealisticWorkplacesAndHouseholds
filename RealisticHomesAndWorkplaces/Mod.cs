using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
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

            updateSystem.UpdateAt<SchoolUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<HospitalUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<PowerPlantUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<AdminBuildingUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<PoliceStationUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<FireStationUpdateSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAt<UpdateWorkplaceSystem>(SystemUpdatePhase.GameSimulation);

        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}
