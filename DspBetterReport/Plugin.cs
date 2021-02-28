using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace DspBetterReport
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess(GAME_PROCESS)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "cn.sinianluoye.game.dsp.betterreport";
        public const string NAME = "DspBetterReport";
        public const string VERSION = "0.0.1";
        private const string GAME_PROCESS = "DSPGAME.exe";

        // private static bool ADDED_RANKBOX = false;

        public void Start()
        {
            Logger.LogInfo("Start Patch!");
            new Harmony(GUID).PatchAll();
            
        }

        [HarmonyPatch(typeof(XConsole), "Update")]
        class PatchXConsole
        {
            public static void Prefix(XConsole __instance)
            {
                __instance.password = 0;
            }
        }

        [HarmonyPatch(typeof(UIProductionStatWindow), "_OnInit")]
        class AddRankBoxPatch
        {
            public static void Prefix(UIProductionStatWindow __instance)
            {
                UIComboBox productRankBox = Traverse.Create(__instance).Field("productRankBox").GetValue<UIComboBox>();
                productRankBox.Items.Add("需要加水的放前边");
                productRankBox.UpdateItems();
            }
        }

        [HarmonyPatch(typeof(UIProductionStatWindow), "AddToDisplayEntries")]
        class AddToDisplayEntriesPatch
        {
            public static int GetScore(int production, int consume)
            {
                if(production == 0)
                {
                    if(consume == 0)
                    {
                        return 0x7f7f7f7f;
                    }
                    return -consume;
                }
                int flag = 1;
                if (production < consume)
                {
                    flag = -1;
                    int t = production;
                    production = consume;
                    consume = t;
                }
                return flag * ((production - consume) * 20 / production);
            }

            public static bool Prefix(UIProductionStatWindow __instance, FactoryProductionStat factoryStat)
            {
                int[] itemIds = ItemProto.itemIds;
                int length = itemIds.Length;
                var displayEntries = Traverse.Create(__instance).Field("displayEntries").GetValue<List<int[]>>();
                int[] productIndices = factoryStat.productIndices;
                for (int index1 = 0; index1 < length; ++index1)
                {
                    int c1 = __instance.statTimeLevel + 1;
                    int c2 = c1 + 7;
                    int id = itemIds[index1];
                    switch (id)
                    {
                        case 11901:
                        case 11902:
                        case 11903:
                            continue;
                        default:
                            int favoriteId = __instance.production.favoriteIds[id];
                            if (productIndices[id] > 0 && ((favoriteId & __instance.favoriteMask) != 0 || __instance.favoriteMask == 0))
                            {
                                int count = displayEntries.Count;
                                bool flag = false;
                                for (int index2 = 0; index2 < count; ++index2)
                                {
                                    if (displayEntries[index2][0] == id)
                                    {
                                        ProductStat productStat = factoryStat.productPool[productIndices[id]];
                                        displayEntries[index2][1] += productStat.total[c1];
                                        displayEntries[index2][3] += productStat.total[c2];
                                        displayEntries[index2][4] = GetScore(displayEntries[index2][1], displayEntries[index2][3]);
                                        flag = true;
                                    }
                                }
                                if (!flag)
                                {
                                    ProductStat productStat = factoryStat.productPool[productIndices[id]];
                                    displayEntries.Add(new int[5]
                                    {
                                        id,
                                        productStat.total[c1],
                                        LDB.items.Select(id).index,
                                        productStat.total[c2],
                                        GetScore(productStat.total[c1], productStat.total[c2])
                                    });
                                    continue;
                                }
                                continue;
                            }
                            continue;
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(UIProductionStatWindow), "RankEntries")]
        class RankEntriesPatch
        {
           
            public static bool Prefix(UIProductionStatWindow __instance)
            {
                if (__instance.statRank == 3)
                {
                    var displayEntries = Traverse.Create(__instance).Field("displayEntries").GetValue<List<int[]>>();
                    displayEntries.Sort((x, y) =>
                    {
                        if(x[4] == y[4])
                        {
                            return x[2] < y[2] ? -1 : 1;
                        }
                        return x[4] < y[4] ? -1 : 1;
                    });
                    return false;
                }
                return true;
            }
        }
    }
}