﻿using Eco.Gameplay.Economy;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class CurrencyDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[Currencies]"; } }
        protected override int TimerUpdateIntervalMs { get { return 60000; } }
        protected override int TimerStartDelayMs { get { return 10000; } }

        public override string ToString() => "Currency Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer | DlEventType.CurrencyCreated;
        protected override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DLConfig.Data.CurrencyDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();
            IEnumerable<Currency> currencies = Lookups.Currencies;
            var currencyTradesMap = Moose.Plugin.MooseStorage.WorldData.CurrencyToTradeCountMap;
            if (!(target is CurrencyChannelLink currencyLink))
                return;

            // Figure out which displays to enable based on config
            bool mintedExists = currencies.Any(c => c.Backed);
            bool useMinted = currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.Always
                || (mintedExists && currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.MintedExists)
                || (!mintedExists && currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.NoMintedExists);

            bool usePersonal = currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.Always
                || (mintedExists && currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.MintedExists)
                || (!mintedExists && currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.NoMintedExists);

            if (useMinted)
            {
                IEnumerable<Currency> mintedCurrencies = currencies.Where(c => c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = mintedCurrencies.GetEnumerator();
                for (int i = 0; i < currencyLink.MaxMintedCount && currencyEnumerator.MoveNext(); ++i)
                {
                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, currencyLink.MaxTopCurrencyHolderCount, currencyLink.UseBackingInfo, currencyLink.UseTradeCount);
                    if (currencyReport != null)
                        displayContent.Add(new DisplayContent($"{BaseTag} [{currencyEnumerator.Current.Id}]", embedContent: currencyReport));
                }
            }

            if (usePersonal)
            {
                IEnumerable<Currency> personalCurrencies = currencies.Where(c => !c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = personalCurrencies.GetEnumerator();
                for (int i = 0; i < currencyLink.MaxPersonalCount && currencyEnumerator.MoveNext(); ++i)
                {
                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, currencyLink.MaxTopCurrencyHolderCount, useBackingInfo: true, useTradeCount: true);
                    if (currencyReport != null)
                        displayContent.Add(new DisplayContent($"{BaseTag} [{currencyEnumerator.Current.Id}]", embedContent: currencyReport));
                }
            }
        }
    }
}
