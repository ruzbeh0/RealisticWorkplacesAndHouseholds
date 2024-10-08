﻿using Colossal;
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

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, HospitalGroup)]
        public int hospital_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PublicTransportGroup)]
        public int transit_station_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, AirportGroup)]
        public int airport_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PostOfficeGroup)]
        public int postoffice_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        public int police_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, FireGroup)]
        public int fire_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 2, max = 30, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        public int prison_sqm_per_prisoner { get; set; }

        [SettingsUISlider(min = 2, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        public int prisoners_per_officer { get; set; }

        [SettingsUISlider(min = 10, max = 70, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUISection(CityServicesSection, PoliceGroup)]
        public int prison_non_usable_space { get; set; }

        [SettingsUISlider(min = 1, max = 200, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(IndustrySection, IndustryGroup)]
        public int industry_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 400, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        public int powerplant_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 400, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, ParkGroup)]
        public int park_sqm_per_worker { get; set; }

        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        public bool increase_power_production { get; set; }

        [SettingsUISlider(min = 1, max = 10, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, PowerPlantGroup)]
        public int solarpowerplant_reduction_factor { get; set; }

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

        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, DepotGroup)]
        public int depot_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 1, max = 150, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(CityServicesSection, GarbageGroup)]
        public int garbage_sqm_per_worker { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
        public float sqm_college_adjuster { get; set; }

        [SettingsUISlider(min = 0.1f, max = 5f, step = 1, scalarMultiplier = 1, unit = Unit.kFloatSingleFraction)]
        [SettingsUISection(SchoolSection, SchoolGroup)]
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

        //[SettingsUIButton]
        //[SettingsUIConfirmation]
        //[SettingsUIHideByCondition(typeof(Setting), "IsInGame")]
        //[SettingsUISection(OtherSection, OtherGroup)]
        //public bool SeekNewHouseholds
        //{
        //    set
        //    {
        //        ResetHouseholdsSystem.TriggerReset(Components.ResetType.FindNewHome);
        //    }
        //}
        //
        //[SettingsUIButton]
        //[SettingsUIConfirmation]
        //[SettingsUIHideByCondition(typeof(Setting), "IsInGame")]
        //[SettingsUISection(OtherSection, OtherGroup)]
        //public bool DeleteOverflowHouseholds
        //{
        //    set
        //    {
        //        ResetHouseholdsSystem.TriggerReset(Components.ResetType.Delete);
        //    }
        //}

        



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
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.solarpowerplant_reduction_factor)), $"Reduced the calculated worker count for Solar Power Plants by the specified factor." },
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
}
