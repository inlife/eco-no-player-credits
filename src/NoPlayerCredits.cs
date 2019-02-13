using System.Linq;
using Harmony;
using Eco.Core.Utils;
using Eco.Core.Plugins.Interfaces;
using Eco.Gameplay.Players;
using Eco.Shared.Localization;
using Eco.Gameplay.Economy;
using System.Reflection;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Utils;
using Eco.Shared.Networking;
using Eco.Shared.Items;
using Eco.Gameplay.Aliases;
using Eco.Gameplay.Utils;
using System.Collections.Generic;

namespace NoPlayerCredits
{
    public class NoPlayerCreditsPlugin : IModKitPlugin, IInitializablePlugin
    {
        public string GetStatus() { return string.Empty; }
        public override string ToString() { return Localizer.DoStr("No player credits plugin"); }

        public NoPlayerCreditsPlugin()
        {
            var harmony = HarmonyInstance.Create("inlife.mod.noplayercredits");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Initialize(TimedTask timer)
        { }

        [HarmonyPatch(typeof(CurrencyManager))]
        [HarmonyPatch("TryCreateUserAccount")]
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
            static bool Prefix(CurrencyManager __instance, ref string userName, ref Currency __result)
            {
                __result = __instance.Currencies.FirstOrDefault<Currency>(x => x.CurrencyName.Length > 0);

                // create default currency if none exists
                if (__result == null)
                {
                    __instance.AddCurrency(userName, "Dollar", Eco.Shared.Items.CurrencyType.Backed);
                    __result = __instance.GetCurrency("Dollar");
                }

                return false; // prevent default method execution
            }
        }

        [HarmonyPatch(typeof(BankAccountManager))]
        [HarmonyPatch("TryCreateUserAccount")]
        class PatchBankAccountManager
        {
            static bool Prefix(BankAccountManager __instance, Dictionary<string, BankAccount> ___personalAccounts, User user)
            {
                if (user == null || __instance.All.Any<BankAccount>(x => x.PersonalAccountName == user.Name))
                    return false;

                if (___personalAccounts.ContainsKey(user.Name))
                    return false;

                BankAccount bankAccount = __instance.BankAccounts.Add((INetObject)null) as BankAccount;
                bankAccount.Name = (string)__instance.PlayerAccountName(user.Name);
                bankAccount.PersonalAccountName = user.Name;
                bankAccount.SpecialAccount = SpecialAccountType.Personal;
                bankAccount.DualPermissions.Managers.Add((Alias)user);
                Currency playerCurrency = Singleton<CurrencyManager>.Obj.GetPlayerCurrency(user.Name);
                bankAccount.CurrencyHoldings.Add(playerCurrency.Id, new CurrencyHolding()
                {
                    Currency = (CurrencyHandle)playerCurrency,
                    Val = 0
                });

                ___personalAccounts[user.Name] = bankAccount;

                return false; // prevent default method execution
            }
        }

    }

    public class NoPlayerCreditsPluginCommandHandler : IChatCommandHandler
    {
        [ChatCommand("rmcurrency", "Remove some specific currency by name", ChatAuthorizationLevel.Admin)]
        public static void RemoveCurrency(User user, string currencyName)
        {
            var cm = Eco.Shared.Utils.Singleton<CurrencyManager>.Obj;
            var bm = Eco.Shared.Utils.Singleton<BankAccountManager>.Obj;
            var currency = cm.GetClosestCurrency(currencyName);

            if (currency != null)
            {
                var accounts = bm.BankAccounts;

                foreach (var accountPair in bm.BankAccounts)
                {
                    var account = accountPair.Value;
                    var holdings = account.CurrencyHoldings.GetOr(currency.Id, null);

                    if (holdings != null)
                    {
                        holdings.Val = 0;
                        account.CurrencyHoldings.Remove(currency.Id);
                    }
                }

                cm.Currencies.Remove(currency);
                cm.Currencies.Sort();

                user.Player.SendTemporaryMessage(new LocString("currency has been successfully removed"));
            }
            else
            {
                user.Player.SendTemporaryError(new LocString("trying to remove an unknown currency"));
            }
        }
    }
}
