﻿using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class CraftingFeed : Feed
    {
        public override string ToString()
        {
            return "Crafting Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.WorkOrderCreated;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.CraftingFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is WorkOrderAction craftingEvent)) return;
            if (craftingEvent.Citizen == null) return; // Happens when a crafting table contiues crafting after finishing an item
            if (craftingEvent.MarkedUpName != "Create Work Order") return; // Happens when a player feeds materials to a blocked work order

            string itemName = craftingEvent.OrderCount > 1 ? craftingEvent.CraftedItem.DisplayNamePlural : craftingEvent.CraftedItem.DisplayName; 
            string message = $"**{craftingEvent.Citizen.Name}** started crafting {craftingEvent.OrderCount} `{itemName}` at {(craftingEvent.WorldObject as WorldObject).Name}.";

            foreach (ChannelLink craftingChannel in DLConfig.Data.CraftingFeedChannels)
            {
                if (!craftingChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrID(craftingChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrID(craftingChannel.DiscordChannel);
                if (discordChannel == null) continue;
                await DiscordUtil.SendAsync(discordChannel, message);
                ++_opsCount;
            }
        }
    }
}
