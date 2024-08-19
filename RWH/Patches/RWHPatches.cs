using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.City;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using RWH;
using RealisticWorkplacesAndHouseholds;
using Game.UI.InGame;

namespace RWH.Patches
{
    [HarmonyPatch]
    public class RWHPatches
    {
        [HarmonyPatch(typeof(CityServiceBudgetSystem), "GetExpense", new Type[] { typeof(ExpenseSource), typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void RWHPatches_GetExpense_Postfix(ExpenseSource source, NativeArray<int> expenses, ref int __result)
        {
            float r = (float)__result;
            if (source.Equals(ExpenseSource.ServiceUpkeep))
            {
                r *= ((float)(100 - Mod.m_Setting.service_upkeep_reduction) / 100f);
            }
            __result = (int)r;
        }

        [HarmonyPatch(typeof(CityServiceBudgetSystem), "GetTotalExpenses", new Type[] { typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void RWHPatches_GetTotalExpenses_Postfix(NativeArray<int> expenses, ref int __result)
        { 
             float r = (float)__result;
             __result = (int)(r * ((float)(100 - Mod.m_Setting.service_upkeep_reduction) / 100f));
        }

    }
}
