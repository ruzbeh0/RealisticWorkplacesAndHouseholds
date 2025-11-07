using Colossal.Mathematics;
using Game.Citizens;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RealisticWorkplacesAndHouseholds
{
    public static class BuildingUtils
    {
        const float ELEVATOR_SPACE = 4.0f; // 4sqm for an elevator
        const float STAIRS_SPACE = 8.0f; // 8sqm for an elevator
        const int MIN_APT_SIZE = 60; // Square Meters
        const int MAX_FLOORS = 163;

        //Get building dimensions. Original Method from the Realistic Occupancy Mod by Trejak
        public static Bounds3 GetBuildingDimensions(DynamicBuffer<SubMesh> subMeshes, ComponentLookup<MeshData> meshDataLookup)
        {
            var totalBounds = new Bounds3(0, 0);
            foreach (var submesh in subMeshes)
            {
                var meshData = meshDataLookup[submesh.m_SubMesh];
                if ((meshData.m_State & MeshFlags.Base) != MeshFlags.Base || (meshData.m_DecalLayer & Game.Rendering.DecalLayers.Buildings) != Game.Rendering.DecalLayers.Buildings)
                {
                    // not the main building of the asset, skip
                    continue;
                }
                totalBounds |= meshData.m_Bounds;
            }
            return totalBounds;
        }

        //Get number of people in the building (Students, workers or residents)
        public static int GetPeople(bool residential, float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator, int floor_limit)
        {
            var floorSize = width * length;
            var floorCount = (int)math.floor(height / floor_height);
            floorCount -= floor_offset;
            floorCount = math.min(floor_limit, math.max(floorCount, 1));

            float total_area = floorSize * floorCount;

            float total_elevator_area = 0f;
            if (sqm_per_elevator > 0)
            {
                int number_of_elevators = (int)Math.Floor(total_area / sqm_per_elevator);
                //Remove elevator area
                total_elevator_area = number_of_elevators * ELEVATOR_SPACE * floorCount;
                //Mod.log.Info($"Elevators: {number_of_elevators}, area: {total_area}");

            }

            //If few elevators one stair, if more than 4 elevators two stairs 
            int number_of_stairs = 1;
            if (floorCount >= 7 || width > 100 || length > 100)
            {
                //More stairs for tall or long bulidings
                number_of_stairs++;
            }
            float total_stair_area = number_of_stairs * STAIRS_SPACE * floorCount;


            if (residential)
            {
                if (sqm_per_unit > MIN_APT_SIZE)
                {
                    //If there is little space left in a floor area from a apartment, we remove that area.
                    //If this condition is met, what it represents is that this building has bigger apartments than the average
                    if (floorSize % sqm_per_unit < MIN_APT_SIZE)
                    {
                        total_area -= (floorSize % sqm_per_unit) * floorCount;
                    }
                } else
                {
                    //If it is a small apartment, lets have one per floor - with extra space for elevator and stairs

                    sqm_per_unit = floorSize - (total_stair_area + total_elevator_area) / floorCount;
                }
            }

            // Mod.log.Info($"Final Area: {total_area}, people: {Math.Max((int)Math.Round((total_area) / sqm_per_unit, 0), 1)}");

            total_area -= (total_stair_area + total_elevator_area);
            return Math.Max((int)Math.Round((total_area) / sqm_per_unit, 0), 1);
        }
        public static int GetPeople(bool residential, float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator)
        {
            // If no floor limit specified, use 163 which is the height of the world's tallest building
            int MAX_FLOORS = 163;
            return GetPeople(residential, width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, MAX_FLOORS);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator, int floor_limit)
        {
            // If residential not specified, false
            
            return GetPeople(false, width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, floor_limit);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int floor_offset, int sqm_per_elevator)
        {
            // If no floor limit specified, use 163 which is the height of the world's tallest building
            // If residential not specified, false
            return GetPeople(false,width, length, height, floor_height, sqm_per_unit, floor_offset, sqm_per_elevator, MAX_FLOORS);
        }

        public static int GetPeople(float width, float length, float height, float floor_height, float sqm_per_unit, int sqm_per_elevator)
        {
            // If no floor offset is specified use zero
            // If residential not specified, false
            return GetPeople(false, width, length, height, floor_height, sqm_per_unit, 0, sqm_per_elevator, MAX_FLOORS);
        }

        //Smooths area factor. This is useful for building types that have a good employee per sqm at small buildings but them it is too much for big buildings
        public static float smooth_area_factor(float base_area, float x, float y)
        {
            float area = x * y;
            if (area < base_area)
            {
                return 1;
            }
            else
            {
                return (float)Math.Sqrt(area / base_area);
            }
        }

        public static float smooth_height_factor(float base_height, float y)
        {
            if (y < base_height)
            {
                return 1;
            }
            else
            {
                return (float)Math.Sqrt(y / base_height);
            }
        }

        //Smooths area factor, where areas that are smaller than base area have fators > 1
        public static float smooth_area_factor2(float base_area, float x, float y)
        {
            float area = x * y;
            if (area < base_area)
            {
                return (float)Math.Sqrt(base_area / area);
            }
            else
            {
                return (float)Math.Sqrt(area / base_area);
            }
        }

        //Smooths area factor, where areas that are smaller than base area have fators > 1
        public static float smooth_area_factor3(float base_area, float x, float y)
        {
            float area = x * y;
            if (area < base_area)
            {
                return base_area / area;
            }
            else
            {
                return (float)Math.Sqrt(area / base_area);
            }
        }

        //Calculates number of workers for transportation depots, maintenance depots, and cargo stations
        public static int depotWorkers(float width, float length, float height, float industry_avg_floor_height, float depot_sqm_per_worker)
        {
            //Using a 2 story floor limit
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            return BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, depot_sqm_per_worker * area_factor, 1, 0, 2);
        }

        //Calculates number of workers for garbage facilities
        public static int garbageWorkers(float width, float length, float height, float industry_avg_floor_height, float garbage_sqm_per_worker)
        {
            //Using a 2 story floor limit
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            return BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker * area_factor, 1, 0, 2);
        }

        //Calculates number of workers for parks facilities
        public static int parkWorkers(float width, float length, float height, float industry_avg_floor_height, float park_sqm_per_worker)
        {
            //Using a 2 story floor limit
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            return BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, park_sqm_per_worker * area_factor, 1, 0, 2);
        }

        //Calculates number of workers for telecom facilities
        public static int telecomWorkers(float width, float length, float height, float industry_avg_floor_height, float garbage_sqm_per_worker, int oldworkers)
        {
            if (oldworkers == 0)
            {
                return 0;
            }
            //Using a 2 story floor limit
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor2(base_area, width, length);
            return BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, garbage_sqm_per_worker * area_factor, 1, 0, 2);
        }

        //Calculates number of workers for power plants
        public static int powerPlantWorkers(float width, float length, float height, float industry_avg_floor_height, float powerplant_sqm_per_employee)
        {
            //Apply floor limit because power plants have huge chimney 
            //Smooth the employees per sqm for bigger powerplants
            float base_area = 120 * 120;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            return BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, powerplant_sqm_per_employee * area_factor, 0, 0, 2);
        }

        //Calculates number of workers for hospitals
        public static int hospitalWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_hospital, int office_sqm_per_elevator)
        {
            //Smooth the employees per sqm for bigger hospitals
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor2(base_area, width, length);
            if(area_factor < 1f)
            {
                sqm_per_employee_hospital *= area_factor;
            } else
            {
                //For smaller clinics increase size instead of decreasing employees per sqm
                width *= (float)Math.Sqrt(area_factor);
                length *= (float)Math.Sqrt(area_factor);
            }
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_hospital, office_sqm_per_elevator);
        }

        //Calculates number of workers for Welfare office
        public static int welfareOfficeWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_office, int office_sqm_per_elevator, float non_usable_space_pct)
        {
            //Using same attributes as offices for admin buildings
            //Smooth the employees per sqm for bigger buildings
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            float area = sqm_per_employee_office * (1 + non_usable_space_pct);
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area * area_factor, office_sqm_per_elevator);
        }

        //Calculates number of workers for Admin Buildings
        public static int adminBuildingsWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_office, int office_sqm_per_elevator, float non_usable_space_pct)
        {
            //Using double sqm per employee as office for admin buildings
            //Smooth the employees per sqm for bigger buildings
            float base_area = 50 * 50;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            float area = 2 * sqm_per_employee_office * (1 + non_usable_space_pct);
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area * area_factor, office_sqm_per_elevator);
        }

        //Calculates number of workers for Post Facilities
        public static int postFacilitiesWorkers(float width, float length, float height, float industry_avg_floor_height, float postoffice_sqm_per_employee, float industry_sqm_per_employee, int office_sqm_per_elevator)
        {
            int workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee, office_sqm_per_elevator);
            //post sorting facility
            if (workers > 500)
            {
                float base_area = 30 * 30;
                float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
                //Removing one floor for trucks and mail storage
                workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, postoffice_sqm_per_employee * area_factor, 1, 0);

            }
            return workers;
        }

        //Calculates number of workers for Police stations
        public static int policeStationWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_police)
        {
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_police, 0, 0);
        }

        //Calculates number of workers for Fire stations
        public static int fireStationWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_fire)
        {
            float base_area = 60 * 60;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);

            //Skipping lobby because usually in fire stations the ground floor is the fire truck garage
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_employee_fire * area_factor, 1, 0, 4);
        }

        //Calculates school capacity
        public static int schoolCapacity(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_student, int educationLevel, float sqm_per_student_college_factor, float sqm_per_student_university_factor)
        {
            int level = educationLevel;
            float sqm_per_student_t = sqm_per_student;
            //Average number of students per teachers
            //College
            if (level == 3)
            {
                sqm_per_student_t *= sqm_per_student_college_factor;
            }
            else
            {
                //University
                if (level == 4)
                {
                    sqm_per_student_t *= sqm_per_student_university_factor;
                }
            }
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, sqm_per_student_t, 0);
        }

        //Calculates school workers
        public static int schoolWorkers(float studentCapacity, float studentPerTeacher, float support_staff)
        {
            float teacherAmount = studentCapacity / studentPerTeacher;

            //Support staff adjuster
            float supportStaffAdjuster = support_staff;
            return (int)(teacherAmount / (1f - supportStaffAdjuster));
        }

        //Calculates prison capacity
        public static int prisonCapacity(float width, float length, float height, float commercial_avg_floor_height, float prison_sqm_per_prisoner, float prison_non_usable_area, int office_sqm_per_elevator)
        {
            float area = prison_sqm_per_prisoner * (1 + prison_non_usable_area);
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area, office_sqm_per_elevator);
        }

        //Calculates prison workers
        public static int prisonWorkers(float capacity, float prison_officers_prisoner_ratio)
        {
            return (int)(capacity / prison_officers_prisoner_ratio);
        }

        //Calculates Research Facility Workers
        public static int researchFacilityWorkers(float width, float length, float height, float commercial_avg_floor_height, float sqm_per_employee_office, float non_usable_space_pct, float sqm_per_student_university_factor, int office_sqm_per_elevator)
        {
            float area = sqm_per_employee_office * (1 + non_usable_space_pct);
            float base_area = 100 * 100;
            float area_factor = BuildingUtils.smooth_area_factor(base_area, width, length);
            //Using university factor because they also do research
            return BuildingUtils.GetPeople(width, length, height, commercial_avg_floor_height, area_factor * area * sqm_per_student_university_factor, office_sqm_per_elevator);
        }

        //Calculates Public Transportation Workers
        public static int publicTransportationWorkers(float width, float length, float height, float industry_avg_floor_height, float sqm_per_employee_transit, float non_usable_space_pct, int office_sqm_per_elevator, int oldworkers)
        {
            float area = sqm_per_employee_transit * (1 + non_usable_space_pct);
            float base_area = 80 * 80;
            float area_factor = 2.5f*BuildingUtils.smooth_area_factor(base_area, width, length);
            //Using university factor because they also do research
            int workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, area_factor * area, office_sqm_per_elevator);
            //If building had very few workers, and new to old worker ratio is very high: reduce amount of workers by 5
            float ratio = 1f;
            if(oldworkers > 0)
            {
                ratio = workers / oldworkers;
            }
            if (oldworkers < 5 && ratio > 15)
            {
                workers /= 5;
            }

            if (oldworkers == 0)
            {
                return 1;
            } else
            {
                return workers;
            }
        }

        //Calculates Airport Workers
        public static int airportWorkers(float width, float length, float height, float industry_avg_floor_height, float sqm_per_employee_airport, float non_usable_space_pct, int office_sqm_per_elevator, int oldworkers)
        {
            float area = sqm_per_employee_airport * (1 + non_usable_space_pct);
            float base_area = 100 * 100;
            float area_factor = 2.1f * BuildingUtils.smooth_area_factor(base_area, width, length);
            //Using university factor because they also do research
            int workers = BuildingUtils.GetPeople(width, length, height, industry_avg_floor_height, area_factor * area, office_sqm_per_elevator);
            
            if (oldworkers == 0)
            {
                return 1;
            }
            else
            {
                return workers;
            }
        }

    }
}
