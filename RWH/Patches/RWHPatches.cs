using Game.City;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.UI.InGame;
using HarmonyLib;
using RealisticWorkplacesAndHouseholds;
using RWH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
            int serviceUpkeepIndex = (int)ExpenseSource.ServiceUpkeep;

            if (serviceUpkeepIndex >= 0 && serviceUpkeepIndex < expenses.Length)
            {
                int original = expenses[serviceUpkeepIndex];
                float factor = (100f - Mod.m_Setting.service_upkeep_reduction) / 100f;
                int reduced = Mathf.RoundToInt(original * factor);
                int difference = original - reduced;

                // Add back the over-subtracted amount (because original method did "total -= value")
                __result += difference;
            }
        }
    }
}
