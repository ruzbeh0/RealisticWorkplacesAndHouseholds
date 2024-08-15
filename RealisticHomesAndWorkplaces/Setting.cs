using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.InGame;
using Game.UI.Widgets;
using System.Collections.Generic;
using static Game.Simulation.TerrainSystem;

namespace RealisticWorkplacesAndHouseholds
{
    [FileLocation($"ModsSettings\\{nameof(RealisticWorkplacesAndHouseholds)}\\{nameof(RealisticWorkplacesAndHouseholds)}")]
    [SettingsUIGroupOrder(ResidentialGroup, CommercialGroup, OfficeGroup, IndustryGroup, SchoolGroup, HospitalGroup, PowerPlantGroup, AdminGroup, PoliceFireGroup)]
    [SettingsUIShowGroupName(HospitalGroup, PowerPlantGroup, AdminGroup, PoliceFireGroup)]
    public class Setting : ModSetting
    {
        public const string ResidentialSection = "Residential";
        public const string ResidentialGroup = "ResidentialGroup";
        public const string CommercialSection = "Commercial";
        public const string CommercialGroup = "CommercialGroup";
        public const string OfficeSection = "Office";
        public const string OfficeGroup = "OfficeGroup";
        public const string IndustrySection = "Industry";
        public const string IndustryGroup = "IndustryGroup";
        public const string SchoolSection = "School";
        public const string SchoolGroup = "SchoolGroup";
        public const string CityServicesSection = "CityServices";
        public const string HospitalGroup = "HospitalGroup";
        public const string PowerPlantGroup = "PowerPlantGroup";
        public const string AdminGroup = "AdminGroup";
        public const string PoliceFireGroup = "PoliceFireGroup";

        public Setting(IMod mod) : base(mod)
        {
            if (students_per_teacher == 0) SetDefaults();
        }

        public override void SetDefaults()
        {
            students_per_teacher = 14;
            sqm_per_student = 7;
            sqm_college_adjuster = 3f;
            sqm_univ_adjuster = 4f;
            support_staff = 30f;
            commercial_avg_floor_height = 3.05f;
            commercial_sqm_per_worker = 37;
            police_fire_sqm_per_worker = 47;
            office_sqm_per_worker = 27;
            hospital_sqm_per_worker = 25;
            hospital_sqm_per_patient = 50;
            industry_sqm_per_worker = 50;
            industry_avg_floor_height = 4.5f;
        }

        [SettingsUISlider(min = 2f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public float commercial_avg_floor_height { get; set; }

        [SettingsUISlider(min = 2f, max = 10f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(IndustrySection, IndustryGroup)]
        public float industry_avg_floor_height { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public int commercial_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OfficeSection, OfficeGroup)]
        public int office_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, HospitalGroup)]
        public int hospital_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceFireGroup)]
        public int police_fire_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(IndustrySection, IndustryGroup)]
        public int industry_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, HospitalGroup)]
        public int hospital_sqm_per_patient { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public int students_per_teacher { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public float support_staff { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public int sqm_per_student { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public float sqm_college_adjuster { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public float sqm_univ_adjuster { get; set; }

    }  

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Realistic Workplaces And Households" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "School" },
                { m_Setting.GetOptionTabLocaleID(Setting.ResidentialSection), "Residential" },
                { m_Setting.GetOptionTabLocaleID(Setting.CommercialSection), "Commercial" },
                { m_Setting.GetOptionTabLocaleID(Setting.IndustrySection), "Industry" },
                { m_Setting.GetOptionTabLocaleID(Setting.OfficeSection), "Office" },
                { m_Setting.GetOptionTabLocaleID(Setting.CityServicesSection), "City Services" },

                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolGroup), "School Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialGroup), "Residential Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "Commercial Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "Office Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HospitalGroup), "Hospital Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PowerPlantGroup), "Power Plant Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AdminGroup), "Admin Buildings Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PoliceFireGroup), "Police and Fire Stations Settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avg_floor_height)), "Average Floor Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avg_floor_height)), $"Average Floor Height for commercial, office, and city services buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avg_floor_height)), "Average Floor Height" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avg_floor_height)), $"Average Floor Height for industry buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.police_fire_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.police_fire_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_patient)), "Square Meters per Patient" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_patient)), $"Number of square meters per patient. Higher numbers will decrease the hospital capacity." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.students_per_teacher)), "Number of Students per Teacher" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.students_per_teacher)), $"Number of Students per Teacher. This will be used to calculate the number of workers required at schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.support_staff)), "Percentage of Support Staff" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.support_staff)), $"Percentage of staff at schools that are not teachers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_per_student)), "Base Square Meters per Student" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_per_student)), $"Space required for each student. This will be used to calculate the school capacity." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_college_adjuster)), "Extra College Space Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_college_adjuster)), $"This factor will be applied to the base square meters per student for Colleges." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_univ_adjuster)), "Extra University Space Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_univ_adjuster)), $"This factor will be applied to the base square meters per student for Universities." },

            };
        }

        public void Unload()
        {

        }
    }
}
