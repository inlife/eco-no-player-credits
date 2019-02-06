using System;
using System.Linq;
using Harmony;
using Eco.Core.Utils;
using Eco.Core.Plugins.Interfaces;
using Eco.Gameplay.Players;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using Eco.Gameplay.Economy;
using System.Reflection;

namespace NoPlayerCredits
{
    public class NoPlayerCreditsPlugin : IModKitPlugin, IInitializablePlugin
    {
        public string GetStatus() { return string.Empty; }
        public override string ToString() { return Localizer.DoStr("No player credits plugin"); }

        public void Initialize(TimedTask timer)
        {
            var harmony = HarmonyInstance.Create("inlife.mod.noplayercredits");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(CurrencyManager))]
        [HarmonyPatch("TryCreateAccount")]
        class PatchCurrencyManager1
        {
            static bool Prefix(CurrencyManager __instance, ref User user)
            {
                return false; // prevent default method execution
            }
        }

        [HarmonyPatch(typeof(CurrencyManager))]
        [HarmonyPatch("GetPlayerCurrency")]
        class PatchCurrencyManager2
        {
            static bool Prefix(CurrencyManager __instance, ref Currency __result)
            {
                __result = __instance.Currencies.FirstOrDefault<Currency>((Func<Currency, bool>) (x => x.CurrencyName.Length > 0));
                return false; // prevent default method execution
            }
        }
    }
}
