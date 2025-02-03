using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.InGame;
using Game.UI.Widgets;
using RealisticWorkplacesAndHouseholds.Components;
using RealisticWorkplacesAndHouseholds.Systems;
using System.Collections.Generic;
using System.Net.Configuration;
using static Game.Simulation.TerrainSystem;

namespace RealisticWorkplacesAndHouseholds
{
    [FileLocation($"ModsSettings\\{nameof(RealisticWorkplacesAndHouseholds)}\\{nameof(RealisticWorkplacesAndHouseholds)}")]
    [SettingsUIGroupOrder(ResidentialGroup, ResidentialLowDensityGroup, RowHomesGroup, ResidentialHighDensityGroup, RowHomesGroup, CommercialGroup, OfficeGroup, IndustryGroup, SchoolGroup, HospitalGroup, PowerPlantGroup, ParkGroup, AdminGroup, PoliceGroup, FireGroup, PostOfficeGroup, DepotGroup, GarbageGroup, PublicTransportGroup, AirportGroup, OtherGroup, FindPropertyGroup)]
    [SettingsUIShowGroupName(ResidentialLowDensityGroup, RowHomesGroup, ResidentialHighDensityGroup, HospitalGroup, PowerPlantGroup, ParkGroup, AdminGroup, PoliceGroup, FireGroup, PostOfficeGroup, GarbageGroup, DepotGroup, PublicTransportGroup, AirportGroup, FindPropertyGroup)]
    public class Setting : ModSetting
    {
        public const string ResidentialSection = "Residential";
        public const string ResidentialGroup = "ResidentialGroup";
        public const string ResidentialLowDensityGroup = "ResidentialLowDensityGroup";
        public const string ResidentialHighDensityGroup = "ResidentialHighDensityGroup";
        public const string RowHomesGroup = "RowHomesGroup";
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
        public const string ParkGroup = "ParkGroup";
        public const string AdminGroup = "AdminGroup";
        public const string PoliceGroup = "PoliceGroup";
        public const string FireGroup = "FireGroup";
        public const string DepotGroup = "DepotGroup";
        public const string GarbageGroup = "GarbageGroup";
        public const string PublicTransportGroup = "PublicTransportGroup";
        public const string PostOfficeGroup = "PostOfficeGroup";
        public const string AirportGroup = "AirportGroup";
        public const string OtherSection = "Other";
        public const string OtherGroup = "OtherGroup";
        public const string FindPropertyGroup = "FindPropertyGroup";

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
            commercial_self_service_gas = false;
            police_sqm_per_worker = 60;
            fire_sqm_per_worker = 60;
            office_sqm_per_worker = 23;
            office_elevators_per_sqm = 4180;
            hospital_sqm_per_worker = 100;
            hospital_sqm_per_patient = 50;
            industry_sqm_per_worker = 50;
            powerplant_sqm_per_worker = 200;
            park_sqm_per_worker = 50;
            postoffice_sqm_per_worker = 46;
            transit_station_sqm_per_worker = 60;
            airport_sqm_per_worker = 70;
            industry_avg_floor_height = 4.5f;
            residential_avg_floor_height = 3;
            residential_sqm_per_apartment = 120;
            rowhomes_apt_per_floor = 1;
            disable_row_homes_apt_per_floor = false;
            rowhomes_basement = true;
            residential_units_per_elevator = 70;
            single_household_low_density = true;
            disable_high_level_less_apt = false;
            residential_hallway_space = 10;
            residential_l4_reduction = 10;
            residential_l5_reduction = 20;
            office_non_usable_space = 20;
            commercial_sqm_per_worker_supermarket = 65;
            commercial_sqm_per_worker_restaurants = 28;
            service_upkeep_reduction = 70;
            electricity_consumption_reduction = 20;
            water_consumption_reduction = 20;
            results_reduction = 0;
            residential_lowdensity_sqm_per_apartment = 150;
            prison_sqm_per_prisoner = 4;
            prisoners_per_officer = 4;
            prison_non_usable_space = 40;
            depot_sqm_per_worker = 65;
            garbage_sqm_per_worker = 95;
            increase_power_production = false;
            solarpowerplant_reduction_factor = 5;
            find_property_limit_factor = 2;
            find_property_night = false;
            rent_discount = 20;
            disable_households_calculations = false;
            disable_cityservices_calculations = false;
            disable_workplace_calculations = false;
            residential_vacancy_rate = 5;
        }

        [SettingsUISection(ResidentialSection, ResidentialLowDensityGroup)]
        public bool single_household_low_density { get; set; }

        [SettingsUISlider(min = 60, max = 500, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ResidentialSection, ResidentialLowDensityGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(single_household_low_density))]
        public int residential_lowdensity_sqm_per_apartment { get; set; }

        [SettingsUISlider(min = 2f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(ResidentialSection, ResidentialGroup)]
        public float residential_avg_floor_height { get; set; }

        [SettingsUISlider(min = 60, max = 400, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ResidentialSection, ResidentialGroup)]
        public int residential_sqm_per_apartment { get; set; }

        [SettingsUISlider(min = 0, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ResidentialSection, ResidentialGroup)]
        public int residential_vacancy_rate { get; set; }

        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ResidentialSection, ResidentialHighDensityGroup)]
        public int residential_hallway_space { get; set; }

        [SettingsUISlider(min = 10, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ResidentialSection, ResidentialHighDensityGroup)]
        public int residential_units_per_elevator { get; set; }

        [SettingsUISection(ResidentialSection, ResidentialHighDensityGroup)]
        public bool disable_high_level_less_apt { get; set; }

        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ResidentialSection, ResidentialHighDensityGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_high_level_less_apt))]
        public int residential_l4_reduction { get; set; }

        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(ResidentialSection, ResidentialHighDensityGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_high_level_less_apt))]
        public int residential_l5_reduction { get; set; }

        [SettingsUISection(ResidentialSection, RowHomesGroup)]
        public bool disable_row_homes_apt_per_floor { get; set; }

        [SettingsUISlider(min = 1, max = 4, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(ResidentialSection, RowHomesGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_row_homes_apt_per_floor))]
        public int rowhomes_apt_per_floor { get; set; }

        [SettingsUISection(ResidentialSection, RowHomesGroup)]
        public bool rowhomes_basement { get; set; }

        [SettingsUISlider(min = 2f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public float commercial_avg_floor_height { get; set; }

        [SettingsUISection(CommercialSection, CommercialGroup)]
        public bool commercial_self_service_gas { get; set; }

        [SettingsUISlider(min = 2f, max = 10f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(IndustrySection, IndustryGroup)]
        public float industry_avg_floor_height { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public int commercial_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public int commercial_sqm_per_worker_supermarket { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CommercialSection, CommercialGroup)]
        public int commercial_sqm_per_worker_restaurants { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OfficeSection, OfficeGroup)]
        public int office_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OfficeSection, OfficeGroup)]
        public int office_non_usable_space { get; set; }

        [SettingsUISlider(min = 2000, max = 6000, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OfficeSection, OfficeGroup)]
        public int office_elevators_per_sqm { get; set; }

        [SettingsUISection(CityServicesSection, HospitalGroup)]
        public bool disable_hospital { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, HospitalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_hospital))]
        public int hospital_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PublicTransportGroup)]
        public bool disable_transit { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PublicTransportGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_transit))]
        public int transit_station_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, AirportGroup)]
        public bool disable_airport { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, AirportGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_airport))]
        public int airport_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PostOfficeGroup)]
        public bool disable_postoffice { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PostOfficeGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_postoffice))]
        public int postoffice_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PoliceGroup)]
        public bool disable_police { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_police))]
        public int police_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, FireGroup)]
        public bool disable_fire { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, FireGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_fire))]
        public int fire_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 2, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_police))]
        public int prison_sqm_per_prisoner { get; set; }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_police))]
        public int prisoners_per_officer { get; set; }

        [SettingsUISlider(min = 10, max = 70, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_police))]
        public int prison_non_usable_space { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(IndustrySection, IndustryGroup)]
        public int industry_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        public bool disable_powerplant { get; set; }

        [SettingsUISlider(min = 1, max = 400, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_powerplant))]
        public int powerplant_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, ParkGroup)]
        public bool disable_park { get; set; }

        [SettingsUISlider(min = 1, max = 400, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, ParkGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_park))]
        public int park_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_powerplant))]
        public bool increase_power_production { get; set; }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_powerplant))]
        public int solarpowerplant_reduction_factor { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, HospitalGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_hospital))]
        public int hospital_sqm_per_patient { get; set; }

        [SettingsUISection(SchoolSection, SchoolGroup)]
        public bool disable_school { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_school))]
        public int students_per_teacher { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_school))]
        public float support_staff { get; set; }

        [SettingsUISlider(min = 1, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_school))]
        public int sqm_per_student { get; set; }

        [SettingsUISection(CityServicesSection, DepotGroup)]
        public bool disable_depot { get; set; }

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, DepotGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_depot))]
        public int depot_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, GarbageGroup)]
        public bool disable_garbage { get; set; }

        [SettingsUISlider(min = 1, max = 150, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, GarbageGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_garbage))]
        public int garbage_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_school))]
        public float sqm_college_adjuster { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disable_school))]
        public float sqm_univ_adjuster { get; set; }

        [SettingsUISection(OtherSection, OtherGroup)]
        public bool disable_households_calculations { get; set; }

        [SettingsUISection(OtherSection, OtherGroup)]
        public bool disable_workplace_calculations { get; set; }

        [SettingsUISection(OtherSection, OtherGroup)]
        public bool disable_cityservices_calculations { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int service_upkeep_reduction { get; set; }

        [SettingsUISlider(min = -100, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int electricity_consumption_reduction { get; set; }

        [SettingsUISlider(min = -100, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int water_consumption_reduction { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int rent_discount { get; set; }

        [SettingsUISlider(min = 0, max = 90, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(OtherSection, OtherGroup)]
        public int results_reduction { get; set; }

        [SettingsUISection(OtherSection, OtherGroup)]
        public ResetType evicted_reset_type { get; set; } = ResetType.FindNewHome;

        [SettingsUIButton]
        [SettingsUISection(OtherSection, OtherGroup)]
        public bool Button
        {
            set
            {
                SetDefaults();
            }

        }

        [SettingsUISection(OtherSection, FindPropertyGroup)]
        [SettingsUIMultilineText]
        public string DTText => string.Empty;

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(OtherSection, FindPropertyGroup)]
        public int find_property_limit_factor { get; set; }

        [SettingsUISection(OtherSection, FindPropertyGroup)]
        public bool find_property_night { get; set; }
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
                { m_Setting.GetSettingsLocaleID(), "Realistic W & H" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "School" },
                { m_Setting.GetOptionTabLocaleID(Setting.ResidentialSection), "Residential" },
                { m_Setting.GetOptionTabLocaleID(Setting.CommercialSection), "Commercial" },
                { m_Setting.GetOptionTabLocaleID(Setting.IndustrySection), "Industry" },
                { m_Setting.GetOptionTabLocaleID(Setting.OfficeSection), "Office" },
                { m_Setting.GetOptionTabLocaleID(Setting.CityServicesSection), "City Services" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Other" },

                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolGroup), "School" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialGroup), "Residential" },
                { m_Setting.GetOptionGroupLocaleID(Setting.RowHomesGroup), "Row Homes" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialLowDensityGroup), "Low Density" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialHighDensityGroup), "Medium and High Density" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "Commercial Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "Office Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HospitalGroup), "Hospital" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PowerPlantGroup), "Power Plant" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ParkGroup), "Parks" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AdminGroup), "Admin Buildings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PoliceGroup), "Police Stations" },
                { m_Setting.GetOptionGroupLocaleID(Setting.FireGroup), "Fire Stations" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DepotGroup), "Depots And Cargo" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GarbageGroup), "Garbage Facilities" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PublicTransportGroup), "Transportation Stations" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AirportGroup), "Airports" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PostOfficeGroup), "Post Office Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.FindPropertyGroup), "Find Property System Settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_airport)), "Disable Airports" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_airport)), $"Disable workplace calculations for airports." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_depot)), "Disable Depots and Cargo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_depot)), $"Disable workplace calculations for depots and cargo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_fire)), "Disable Fire Stations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_fire)), $"Disable workplace calculations for fire stations." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_transit)), "Disable Transportation Stations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_transit)), $"Disable workplace calculations for transportation stations." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_garbage)), "Disable Garbage Facilities" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_garbage)), $"Disable workplace calculations for garbage facilities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_hospital)), "Disable Hospitals" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_hospital)), $"Disable workplace calculations for hospitals." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_police)), "Disable Police Stations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_police)), $"Disable workplace calculations for police stations." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_park)), "Disable Parks" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_park)), $"Disable workplace calculations for parks." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_postoffice)), "Disable Post Offices" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_postoffice)), $"Disable workplace calculations for post offices." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_powerplant)), "Disable Power Plants" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_powerplant)), $"Disable workplace calculations for power plants." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_school)), "Disable Schools" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_school)), $"Disable workplace calculations for schools." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_vacancy_rate)), "Vacancy Rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_vacancy_rate)), $"Percentage of apartments or houses that will be free and reserved for the local population of the city." },


                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avg_floor_height)), "Average Floor Height (meters)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avg_floor_height)), $"Average Floor Height for commercial, office, and city services buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_self_service_gas)), "Self Service Gas Stations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_self_service_gas)), $"If selected, gas stations will have fewer workers" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.single_household_low_density)), "Single Household for Low Density" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.single_household_low_density)), $"If true, low density houses will only have one household" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_avg_floor_height)), "Average Floor Height (meters)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_avg_floor_height)), $"Average Floor Height for residential buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_sqm_per_apartment)), "Average Apartment Size (Square Meters)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_sqm_per_apartment)), $"Average Apartment Size in Square Meters." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_lowdensity_sqm_per_apartment)), "Average Apartment Size (Square Meters)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_lowdensity_sqm_per_apartment)), $"Average Apartment Size in Square Meters." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_units_per_elevator)), "Number of Apartments per Elevator" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_units_per_elevator)), $"Number of Apartments per Elevator. The Elevator area will be subtracted and reduce the space available for apartments in the building." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_hallway_space)), "Percentage of floor space for hallways" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_hallway_space)), $"Percentage of floor space for hallways. Does not include space for elevators and stairs." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_row_homes_apt_per_floor)), "Disable Apt. per floors for Row Homes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_row_homes_apt_per_floor)), $"If disabled, Row Homes will use the average sqm per apartment defined above. Otherwise, a specific number of apartments per floors will be used for Row Homes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rowhomes_apt_per_floor)), "Number of apartments per floor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rowhomes_apt_per_floor)), $"Number of apartments per floor in Row Homes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_high_level_less_apt)), "Disable Fewer Households On Luxury High Rises" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_high_level_less_apt)), $"Number of apartments will be reduced once the building reaches levels 4 and 5. WARNING: This feature may cause performance issues and more homeless people in bigger cities." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_l4_reduction)), "Level 4: Apartment Size Increase" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_l4_reduction)), $"Increase in apartment size when compared to the average apartment size." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_l5_reduction)), "Level 5: Apartment Size Increase" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_l5_reduction)), $"Increase in apartment size when compared to the average apartment size." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rowhomes_basement)), "Basement" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rowhomes_basement)), $"If set to true, Row Homes will have an extra floor in the basement." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avg_floor_height)), "Average Floor Height (meters)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avg_floor_height)), $"Average Floor Height for industry buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.police_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.police_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.fire_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.fire_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.garbage_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.garbage_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.depot_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.depot_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker_supermarket)), "Supermarkets: Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker_supermarket)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker_restaurants)), "Restaurants: Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker_restaurants)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.powerplant_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.powerplant_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.increase_power_production)), "Increase Power Production" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.increase_power_production)), $"Increases the amount of electricity produced based on the increase of employees compared to the vanilla game. If there is a decrease of employees, power production will not change." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.solarpowerplant_reduction_factor)), "Solar Employee Reduction Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.solarpowerplant_reduction_factor)), $"Reduce the calculated worker count for Solar Power Plants by the specified factor." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.transit_station_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.transit_station_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.airport_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.airport_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.postoffice_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.postoffice_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_patient)), $"Number of square meters per patient. Higher numbers will decrease the hospital capacity." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_patient)), "Square Meters per Patient" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sqm_per_worker)), "Square Meters per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_elevators_per_sqm)), $"The total amount of space required to have one elevator. This area will be used to calculate the number of elevators in the building, and the elevator area will reduce the available space for workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_elevators_per_sqm)), "Square Meters per Elevator" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sqm_per_worker)), $"Number of square meters per worker. Higher numbers will decrease the number of workers." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_non_usable_space)), $"Percentage of Non-usable Area" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_non_usable_space)), "Percentage of area in Office Buildings that are non-usable. This includes for example: hallways, conference rooms and mail rooms. It does not include space for elevators and stairs." },
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.service_upkeep_reduction)), "Service Upkeep Cost Reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.service_upkeep_reduction)), $"Reduce the cost of service upkeep. Use this to compensate for the extra cost due to more workers at service buildings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.electricity_consumption_reduction)), "Electricity Consumption Reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.electricity_consumption_reduction)), $"Reduce electricity consumptions. Use this to compensate for the increase of electricity consumption in buildings. Electricity consumption is directly related to number of residents or employees in a building, so this mod will cause buildings to use more electricity." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.water_consumption_reduction)), "Water Consumption Reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.water_consumption_reduction)), $"Reduce water consumptions. Use this to compensate for the increase of water consumption in buildings. Water consumption is directly related to number of residents or employees in a building, so this mod will cause buildings to use more water." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rent_discount)), "Residential Rent Discount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rent_discount)), $"This mod may cause higher rents due to changes in the number of building households. Use this setting to set a rent discount that will compensate the increase in rent." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.results_reduction)), "Global Workplace/Households reduction" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.results_reduction)), $"Reduce all workplace and househols results by this percentage. Note that some buildings need to have a minimum of 1 worker or household and those might not be reduced." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prison_sqm_per_prisoner)), "Square Meters per Prisoner" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prison_sqm_per_prisoner)), $"Number of square meters per prisoner. Higher numbers will decrease the number of prisoners." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prisoners_per_officer)), "Number of Prisoners per Correctional Officer" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prisoners_per_officer)), $"Number of Prisoners per Correctional Officer. This will be used to calculate the number of workers required at prisons." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Reset Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Reset settings to default values" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prison_non_usable_space)), $"Percentage of Non-usable Area" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prison_non_usable_space)), "Percentage of area in Prison Buildings that are as living quarters for prisoners. This include hallways, cafeterias, medical facilities, offices, etc." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Update the settings below if you are having issues with residential demand and buildings without households" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.find_property_limit_factor)), $"Requests Limit Factor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.find_property_limit_factor)), "Increase the allowed limit of find property requests. Vanilla value is 1. Higher values will impact performance and slow the game." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.find_property_night)), $"Double Limit at Night" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.find_property_night)), "Double the amount of find property requests at night. This will impact performance and is only recommended to use with the Realistic Trips Mod" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evicted_reset_type)), $"Evicted Households Action" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evicted_reset_type)), "Select what action to do with evicted households. They can either look for a new home or be deleted." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_households_calculations)), $"Disable Household Calculations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_households_calculations)), "Disable calculations for households. Household values for residential buildings will be set to vanilla. Requires restarting the game." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_workplace_calculations)), $"Disable Workplace Calculations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_workplace_calculations)), "Disable calculations for workplaces on Office, Commercial, and Industrial zones. Workplace values will be set to vanilla. Requires restarting the game." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_cityservices_calculations)), $"Disable City Services Workplace Calculations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_cityservices_calculations)), "Disable calculations for workplaces on City Services buildings. Workplace values will be set to vanilla. Requires restarting the game." },
                { m_Setting.GetEnumValueLocaleID(ResetType.Delete), "Delete" },
                { m_Setting.GetEnumValueLocaleID(ResetType.FindNewHome), "Find New Home" },

                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.SeekNewHouseholds)), "Seek New Households" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.SeekNewHouseholds)), $"RECOMMENDED: If any building has more households than properties (usually when this mod is started with a pre-existing existing save), click this button to have some households look for a new home while the simulation plays. Effect is near immediate, so be aware." },
                //{ m_Setting.GetOptionWarningLocaleID(nameof(Setting.SeekNewHouseholds)), "Read the setting description first and prepare for residents to move out. The other option won't work, and this can't be undone. Are you sure you want to reset the households?"},
                //
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteOverflowHouseholds)), "Delete Overflow Households" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteOverflowHouseholds)), $"USE WITH CAUTION: If any building has more households than properties (usually when this mod is started with a pre-existing existing save), click this button to remove those households. This change is abrupt and immediate after pressing play." },
                //{ m_Setting.GetOptionWarningLocaleID(nameof(Setting.DeleteOverflowHouseholds)), "Read the setting description first and prepare for a large drop in population. The other option won't work, and this can't be undone. Are you sure you want to delete the overflow households?"}


            };
        }

        public void Unload()
        {

        }
    }

    public class LocalePT : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocalePT(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Realistic W & H" },
                { m_Setting.GetOptionTabLocaleID(Setting.SchoolSection), "Escola" },
                { m_Setting.GetOptionTabLocaleID(Setting.ResidentialSection), "Residência" },
                { m_Setting.GetOptionTabLocaleID(Setting.CommercialSection), "Comércio" },
                { m_Setting.GetOptionTabLocaleID(Setting.IndustrySection), "Indústria" },
                { m_Setting.GetOptionTabLocaleID(Setting.OfficeSection), "Escritório" },
                { m_Setting.GetOptionTabLocaleID(Setting.CityServicesSection), "Serviços Públicos" },
                { m_Setting.GetOptionTabLocaleID(Setting.OtherSection), "Outros" },

                { m_Setting.GetOptionGroupLocaleID(Setting.SchoolGroup), "Escola" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialGroup), "Residência" },
                { m_Setting.GetOptionGroupLocaleID(Setting.RowHomesGroup), "Casa Geminada" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialLowDensityGroup), "Baixa Densidade" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ResidentialHighDensityGroup), "Média e Alta Densidade" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialGroup), "Configurações Comerciais" },
                { m_Setting.GetOptionGroupLocaleID(Setting.OfficeGroup), "Configurações de Escritórios" },
                { m_Setting.GetOptionGroupLocaleID(Setting.HospitalGroup), "Hospitais" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PowerPlantGroup), "Usinas de Energia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ParkGroup), "Parques" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AdminGroup), "Prédios Administrativos" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PoliceGroup), "Delegacias de Polícia" },
                { m_Setting.GetOptionGroupLocaleID(Setting.FireGroup), "Bombeiros" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DepotGroup), "Depósitos e Carga" },
                { m_Setting.GetOptionGroupLocaleID(Setting.GarbageGroup), "Instalações de lixo" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PublicTransportGroup), "Estações de Transporte" },
                { m_Setting.GetOptionGroupLocaleID(Setting.AirportGroup), "Aeroportos" },
                { m_Setting.GetOptionGroupLocaleID(Setting.PostOfficeGroup), "Correios" },
                { m_Setting.GetOptionGroupLocaleID(Setting.FindPropertyGroup), "Configurações do sistema de encontrar propriedade" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_airport)), "Desativar Aeroportos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_airport)), $"Desabilite cálculos de local de trabalho para aeroportos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_depot)), "Desativar Depósitos e Carga" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_depot)), $"Desabilite cálculos de local de trabalho para Depósitos e Carga." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_fire)), "Desativar Bombeiros" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_fire)), $"Desabilite cálculos de local de trabalho para Bombeiros." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_transit)), "Desativar Estações de Transporte" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_transit)), $"Desabilite cálculos de local de trabalho para Estações de Transporte." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_garbage)), "Desativar Instalações de lixo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_garbage)), $"Desabilite cálculos de local de trabalho para Instalações de lixo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_hospital)), "Desativar Hospitais" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_hospital)), $"Desabilite cálculos de local de trabalho para Hospitais." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_police)), "Desativar Delegacias de Polícia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_police)), $"Desabilite cálculos de local de trabalho para Delegacias de Polícia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_park)), "Desativar Parques" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_park)), $"Desabilite cálculos de local de trabalho para Parques." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_postoffice)), "Desativar Correios" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_postoffice)), $"Desabilite cálculos de local de trabalho para Correios." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_powerplant)), "Desativar Usinas de Energia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_powerplant)), $"Desabilite cálculos de local de trabalho para Usinas de Energia." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_school)), "Desativar Escolas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_school)), $"Desabilite cálculos de local de trabalho para Escolas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_avg_floor_height)), "Altura média de um andar (metros)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_avg_floor_height)), $"Altura média de um andar para comércio, escritórios, e serviços públicos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_self_service_gas)), "Postos de gasolina sem frentistas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_self_service_gas)), $"Se selecionado, postos de gasolina vão ter menos empregados." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.single_household_low_density)), "Apenas uma família para baixa densidade" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.single_household_low_density)), $"Se selecionado, casas de baixa densidade vão ter apenas uma família." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_avg_floor_height)), "Altura média de um andar (metros)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_avg_floor_height)), $"Altura média de um andar para prédios residenciaiss" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_sqm_per_apartment)), "Tamanho média de um apartamento (metros quadrados)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_sqm_per_apartment)), $"Tamanho média de um apartamento em metros quadrados." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_lowdensity_sqm_per_apartment)), "Tamanho média de um apartamento (metros quadrados)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_lowdensity_sqm_per_apartment)), $"Tamanho média de um apartamento em metros quadrados." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_units_per_elevator)), "Número de apartamentos por elevador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_units_per_elevator)), $"Número de apartamentos por elevador. A área do elevador será subtraída e reduzirá o espaço disponível para apartamentos no edifício." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_hallway_space)), "Porcentagem de área do piso para corredores" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_hallway_space)), $"Porcentagem de espaço do piso para corredores. Não inclui espaço para elevadores e escadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_row_homes_apt_per_floor)), "Desabilitar Apt. por andar para Casas Geminadas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_row_homes_apt_per_floor)), $"Se desabilitado, casas geminadas usará a média de m² por apartamento definida acima. Caso contrário, um número específico de apartamentos por andar será usado." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rowhomes_apt_per_floor)), "Número de apartamentos por andar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rowhomes_apt_per_floor)), $"Número de apartamentos por andar para casas geminadas" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_high_level_less_apt)), "Desabilitar menos famílias em arranha-céus de luxo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_high_level_less_apt)), $"O número de apartamentos será reduzido quando o edifício atingir os níveis 4 e 5. AVISO: Esse recurso pode causar problemas de desempenho e mais moradores de rua em cidades grandes." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_l4_reduction)), "Nível 4: Aumento do tamanho do apartamento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_l4_reduction)), $"Aumento do tamanho do apartamento em comparação ao tamanho médio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.residential_l5_reduction)), "Nível 5: Aumento do tamanho do apartamento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.residential_l5_reduction)), $"Aumento do tamanho do apartamento em comparação ao tamanho médio." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rowhomes_basement)), "Porão" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rowhomes_basement)), $"Se definido como verdadeiro, casas geminadas terão um andar extra no porão." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_avg_floor_height)), "Altura média de um andar (metros)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_avg_floor_height)), $"Altura média de um andar para edifícios industriais." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.police_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.police_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.fire_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.fire_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.garbage_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.garbage_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.depot_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.depot_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker_supermarket)), "Supermercados: Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker_supermarket)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.commercial_sqm_per_worker_restaurants)), "Restaurantes: Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.commercial_sqm_per_worker_restaurants)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.industry_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.industry_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.park_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.park_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.powerplant_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.powerplant_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.increase_power_production)), "Aumentar a produção de energia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.increase_power_production)), $"Aumenta a quantidade de eletricidade produzida com base no aumento de funcionários em comparação ao jogo vanilla. Se houver uma diminuição de funcionários, a produção de energia não mudará." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.solarpowerplant_reduction_factor)), "Fator de redução de funcionários da Usina Solar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.solarpowerplant_reduction_factor)), $"Reduz a contagem calculada de trabalhadores para Usinas de Energia Solar pelo fator especificado." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.transit_station_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.transit_station_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.airport_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.airport_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.postoffice_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.postoffice_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hospital_sqm_per_patient)), $"Número de metros quadrados por paciente. Números maiores diminuirão a capacidade do hospital." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hospital_sqm_per_patient)), "Metros quadrados por paciente" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_sqm_per_worker)), "Metros quadrados por trabalhador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_elevators_per_sqm)), $"A quantidade total de espaço necessária para ter um elevador. Esta área será usada para calcular o número de elevadores no edifício, e a área do elevador reduzirá o espaço disponível para os trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_elevators_per_sqm)), "Metros quadrados por Elevador" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_sqm_per_worker)), $"Número de metros quadrados por trabalhador. Números maiores diminuirão o número de trabalhadores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.office_non_usable_space)), $"Porcentagem de área não utilizável" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.office_non_usable_space)), "Porcentagem de área em Edifícios de Escritórios que não são utilizáveis. Isso inclui, por exemplo: corredores, salas de conferência e salas de correspondência. Não inclui espaço para elevadores e escadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.students_per_teacher)), "Número de alunos por professor" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.students_per_teacher)), $"Número de Alunos por Professor. Isso será usado para calcular o número de trabalhadores necessários nas escolas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.support_staff)), "Porcentagem de equipe de suporte" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.support_staff)), $"Porcentagem de funcionários em escolas que não são professores." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_per_student)), "Metros quadrados base por aluno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_per_student)), $"Espaço necessário para cada aluno. Isso será usado para calcular a capacidade da escola." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_college_adjuster)), "Fator de espaço extra para faculdades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_college_adjuster)), $"Este fator será aplicado metros quadrados base por aluno para faculdades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.sqm_univ_adjuster)), "Fator de espaço extra para universidades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.sqm_univ_adjuster)), $"Este fator será aplicado metros quadrados base por aluno para universidades." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.service_upkeep_reduction)), "Redução de custos de manutenção de serviços públicos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.service_upkeep_reduction)), $"Reduza o custo de manutenção de serviços públicos. Use isso para compensar o custo extra devido a mais trabalhadores em prédios de serviços públicos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.electricity_consumption_reduction)), "Redução do Consumo de Eletricidade" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.electricity_consumption_reduction)), $"Reduza o consumo de eletricidade. Use isso para compensar o aumento do consumo de eletricidade em prédios. O consumo de eletricidade está diretamente relacionado ao número de moradores ou funcionários em um prédio, então este mod fará com que os prédios usem mais eletricidade." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.water_consumption_reduction)), "Redução do Consumo de Água" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.water_consumption_reduction)), $"Reduza o consumo de água. Use isso para compensar o aumento do consumo de água em prédios. O consumo de água está diretamente relacionado ao número de moradores ou funcionários em um prédio, então este mod fará com que os prédios usem mais água." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.rent_discount)), "Desconto de Aluguel Residencial" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.rent_discount)), $"Este mod pode causar aluguéis mais altos devido a mudanças no número de domicílios no prédio. Use esta configuração para definir um desconto de aluguel que compensará o aumento do aluguel." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.results_reduction)), "Redução global do local de trabalho/domicílios" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.results_reduction)), $"Reduza todos os resultados de local de trabalho e domicílios por esta porcentagem. Observe que alguns edifícios precisam ter no mínimo 1 trabalhador ou domicílio e estes podem não ser reduzidos." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prison_sqm_per_prisoner)), "Metros quadrados por prisioneiro" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prison_sqm_per_prisoner)), $"Número de metros quadrados por prisioneiro. Números maiores diminuirão o número de prisioneiros." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prisoners_per_officer)), "Número de presos por agente penitenciário" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prisoners_per_officer)), $"Número de Prisioneiros por agente penitenciário. Isso será usado para calcular o número de trabalhadores necessários nas prisões." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Button)), "Redefinir configurações" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Button)), $"Redefinir as configurações para os valores padrão" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.prison_non_usable_space)), $"Porcentagem de área não utilizável" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.prison_non_usable_space)), "Porcentagem de área em penitenciárias que não são usadas como alojamentos para prisioneiros. Isso inclui corredores, refeitórios, instalações médicas, escritórios, etc." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DTText)), $"Atualize as configurações abaixo se você estiver tendo problemas com demanda residencial e edifícios sem domicílios" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.find_property_limit_factor)), $"Fator Limite de Solicitações de Propriedades" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.find_property_limit_factor)), "Aumente o limite permitido de solicitações de de busca por propriedades residenciais. O valor vanilla é 1. Valores mais altos impactarão o desempenho e deixarão o jogo mais lento." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.find_property_night)), $"Limite Duplo à Noite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.find_property_night)), "Dobrar a quantidade de solicitações de busca de propriedades à noite. Isso impactará o desempenho e só é recomendado para uso com o Realistic Trips Mod" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.evicted_reset_type)), $"Ação para famílias despejadas" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.evicted_reset_type)), "Selecione qual ação fazer com as famílias despejadas de sua residência. Elas podem procurar um novo lar ou serem apagadas." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_households_calculations)), $"Desativar cálculos de número de domicílios" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_households_calculations)), "Desabilitar cálculos para domicílios. Os valores de domicílios para prédios residenciais serão definidos como vanilla. Requer reiniciar o jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_workplace_calculations)), $"Desativar cálculos do locais de trabalho" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_workplace_calculations)), "Desabilite cálculos para locais de trabalho em zonas de escritório, comércio e indústria. Os valores de locais de trabalho serão definidos como vanilla. Requer reiniciar o jogo." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.disable_cityservices_calculations)), $"Desabilitar cálculos do local de trabalho dos serviços da públicos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.disable_cityservices_calculations)), "Desabilite cálculos para locais de trabalho em prédios de Serviços Públicos. Os valores dos locais de trabalho serão definidos como vanilla. Requer reiniciar o jogo." },
                { m_Setting.GetEnumValueLocaleID(ResetType.Delete), "Apagar" },
                { m_Setting.GetEnumValueLocaleID(ResetType.FindNewHome), "Encontrar um novo lar" },

                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.SeekNewHouseholds)), "Seek New Households" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.SeekNewHouseholds)), $"RECOMMENDED: If any building has more households than properties (usually when this mod is started with a pre-existing existing save), click this button to have some households look for a new home while the simulation plays. Effect is near immediate, so be aware." },
                //{ m_Setting.GetOptionWarningLocaleID(nameof(Setting.SeekNewHouseholds)), "Read the setting description first and prepare for residents to move out. The other option won't work, and this can't be undone. Are you sure you want to reset the households?"},
                //
                //{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteOverflowHouseholds)), "Delete Overflow Households" },
                //{ m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteOverflowHouseholds)), $"USE WITH CAUTION: If any building has more households than properties (usually when this mod is started with a pre-existing existing save), click this button to remove those households. This change is abrupt and immediate after pressing play." },
                //{ m_Setting.GetOptionWarningLocaleID(nameof(Setting.DeleteOverflowHouseholds)), "Read the setting description first and prepare for a large drop in population. The other option won't work, and this can't be undone. Are you sure you want to delete the overflow households?"}


            };
        }

        public void Unload()
        {

        }
    }
}
