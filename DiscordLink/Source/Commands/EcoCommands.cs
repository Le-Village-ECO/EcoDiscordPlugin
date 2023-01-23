using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using Eco.Plugins.DiscordLink.Extensions;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.SharedCommands;
using static Eco.Plugins.DiscordLink.Enums;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    [ChatCommandHandler]
    public class EcoCommands
    {
#pragma warning disable CS4014 // Call not awaited (Shared commands are async but Eco commands can't be)

        #region Commands Base

        private delegate Task EcoCommand(User callingUser, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(EcoCommand command, User callingUser, params string[] parameters)
        {
            // Trim the arguments since they often have a space at the beginning
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameters[i] = parameters[i].Trim();
            }

            string commandName = command.Method.Name;
            try
            {
                Logger.Debug($"{MessageUtils.StripTags(callingUser.Name)} invoked Eco command \"/{command.Method.Name}\"");
                await command(callingUser, parameters);
            }
            catch (Exception e)
            {
                EcoUtils.SendInfoBoxToUser(callingUser, $"Error occurred while attempting to run that command. Error message: {e}");
                Logger.Error($"An exception occured while attempting to execute a command.\nCommand name: \"{commandName}\"\nCalling user: \"{MessageUtils.StripTags(callingUser.Name)}\"\nError message: {e}");
            }
        }

        [ChatCommand("Commands for the Discord integration plugin.", "DL", ChatAuthorizationLevel.User)]
#pragma warning disable IDE0079 // Remove unnecessary suppression (This is a false positive case)
#pragma warning disable IDE0060 // Remove unused parameter - callingUser parameter required
        public static void DiscordLink(User callingUser) { }
#pragma warning restore IDE0079
#pragma warning restore IDE0060

        #endregion

        #region User Feedback

        public static void ReportCommandError(User callingUser, string message)
        {
            EcoUtils.SendErrorBoxToUser(callingUser, message);
        }

        public static void ReportCommandInfo(User callingUser, string message)
        {
            EcoUtils.SendInfoBoxToUser(callingUser, message);
        }

        public static void DisplayCommandData(User callingUser, string panelInstance, string title, string data)
        {
            EcoUtils.SendInfoPanelToUser(callingUser, panelInstance, title, data);
        }

        #endregion

        #region Plugin Management

        [ChatSubCommand("DiscordLink", "Forces an update.", "DL-Update", ChatAuthorizationLevel.Admin)]
        public static async Task Update(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.Update(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Restarts the plugin.", "DL-Restart", ChatAuthorizationLevel.Admin)]
        public static async Task Restart(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.Restart(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes all persistent storage data.", "DL-ResetPersistentData", ChatAuthorizationLevel.Admin)]
        public static async Task ResetPersistentData(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ResetPersistentData(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Resets world data as if a new world had been created.", "DL-ResetWorldData", ChatAuthorizationLevel.Admin)]
        public static async Task ResetWorldData(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ResetWorldData(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Deletes all Discord roles created and tracked by DiscordLink.", "DL-ClearRoles", ChatAuthorizationLevel.Admin)]
        public static async Task ClearRoles(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ClearRoles(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Server Management

        [ChatSubCommand("DiscordLink", "Shuts the server down.", "DL-Servershutdown", ChatAuthorizationLevel.Admin)]
        public static async Task ServerShutdown(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ServerShutdown(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Meta

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", "DL-About", ChatAuthorizationLevel.User)]
        public static async Task About(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"About DiscordLink {Plugins.DiscordLink.DiscordLink.Obj.PluginVersion}", MessageBuilder.Shared.GetAboutMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status.", "DL-Status", ChatAuthorizationLevel.Admin)]
        public static async Task PluginStatus(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status", MessageBuilder.Shared.GetDisplayStringAsync(verbose: false).Result);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status including verbose debug level information.", "DL-StatusVerbose", ChatAuthorizationLevel.Admin)]
        public static async Task PluginStatusVerbose(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status Verbose", MessageBuilder.Shared.GetDisplayStringAsync(verbose: true).Result);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks configuration setup and reports any errors.", "DL-VerifyConfig", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyConfig(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyConfig(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions and intents needed for the current configuration and reports any missing ones.", "DL-VerifyPermissions", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyPermissions(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.All);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all intents needed and reports any missing ones.", "DL-VerifyIntents", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyIntents(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all server permissions needed and reports any missing ones.", "DL-VerifyServerPermissions", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyServerPermissions(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions needed for the given channel and reports any missing ones.", "DL-VerifyChannelPermissions", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyChannelPermissions(User callingUser, string channelNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissionsForChannel(CommandInterface.Eco, callingUser, channelNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Presents a list of all channel links.", "DL-ListChannels", ChatAuthorizationLevel.Admin)]
        public static async Task ListLinkedChannels(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ListChannelLinks(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Lookups

        [ChatSubCommand("DiscordLink", "Displays the Player Report for the given player.", "DL-PlayerReport", ChatAuthorizationLevel.User)]
        public static async Task PlayerReport(User callingUser, string playerNameOrID, string reportType = "All")
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                if(!Enum.TryParse(reportType, out PlayerReportComponentFlag reportTypeEnum))
                {
                    ReportCommandError(callingUser, $"\"{reportType}\" is not a valid report type. The available report types are: {string.Join(", ", Enum.GetNames(typeof(PlayerReportComponentFlag)))}");
                    return;
                }

                await SharedCommands.PlayerReport(CommandInterface.Eco, callingUser, playerNameOrID, reportTypeEnum);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Currency Report for the given currency.", "DL-CurrencyReport", ChatAuthorizationLevel.User)]
        public static async Task CurrencyReport(User callingUser, string currencyNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.CurrencyReport(CommandInterface.Eco, callingUser, currencyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the top used currencies.", "DL-Currencies", ChatAuthorizationLevel.User)]
        public static async Task CurrenciesReport(User callingUser, string currencyType = "all",
            int maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            int holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                CurrencyType type;
                if (!Enum.TryParse(currencyType, out type))
                {
                    ReportCommandError(callingUser, "The CurrencyType parameter must be \"All\", \"Personal\" or \"Minted\".");
                    return;
                }

                await SharedCommands.CurrenciesReport(CommandInterface.Eco, callingUser, type, maxCurrenciesPerType, holdersPerCurrency);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Election Report for the given election.", "DL-Election", ChatAuthorizationLevel.User)]
        public static async Task ElectionReport(User callingUser, string electionNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ElectionReport(CommandInterface.Eco, callingUser, electionNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active elections.", "DL-Elections", ChatAuthorizationLevel.User)]
        public static async Task ElectionsReport(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ElectionsReport(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Work Party Report for the given work party.", "DL-WorkParty", ChatAuthorizationLevel.User)]
        public static async Task WorkPartyReport(User callingUser, string workPartyNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.WorkPartyReport(CommandInterface.Eco, callingUser, workPartyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active work parties.", "DL-WorkParties", ChatAuthorizationLevel.User)]
        public static async Task WorkPartiesReport(User callingUser)
        {
            await ExecuteCommand<object>(async(lUser, args) =>
            {
                await SharedCommands.WorkPartiesReport(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Invites

        [ChatSubCommand("DiscordLink", "Posts the Discord invite message to the target user. The invite will be broadcasted if no target user is specified.", "DL-Invite", ChatAuthorizationLevel.User)]
        public static async Task Invite(User callingUser, string targetUserName = "")
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.PostDiscordInvite(CommandInterface.Eco, callingUser, targetUserName);
            }, callingUser);
        }

        #endregion

        #region Account Linking

        [ChatSubCommand("DiscordLink", "Presents information about account linking.", "DL-LinkInfo", ChatAuthorizationLevel.User)]
        public static async Task LinkInformation(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"Eco --> Discord Account Linking", MessageBuilder.Shared.GetLinkAccountInfoMessage(CommandInterface.Eco));
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Links the calling user account to a Discord account.", "DL-Link", ChatAuthorizationLevel.User)]
        public static async Task LinkDiscordAccount(User callingUser, string discordName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                if (!plugin.Client.BotHasIntent(DiscordIntents.GuildMembers))
                {
                    ReportCommandError(callingUser, $"This server is not configured to use account linking as the bot lacks the elevated Guild Members Intent.");
                    return;
                }

                // Find the Discord user
                DiscordMember matchingMember = null;
                IReadOnlyCollection<DiscordMember> guildMembers = plugin.Client.GetGuildMembersAsync().Result;
                if (guildMembers == null)
                    return;

                foreach (DiscordMember member in guildMembers)
                {
                    if (member.HasNameOrID(discordName))
                    {
                        matchingMember = member;
                        break;
                    }
                }

                if (matchingMember == null)
                {
                    ReportCommandError(callingUser, $"No Discord account with the name \"{discordName}\" could be found.\nUse `/DL-LinkInfo` for linking instructions.");
                    return;
                }

                // Make sure that the accounts aren't already linked to any account
                foreach (LinkedUser linkedUser in DLStorage.PersistentData.LinkedUsers)
                {
                    bool hasSLGID = !string.IsNullOrWhiteSpace(callingUser.SlgId) && !string.IsNullOrWhiteSpace(linkedUser.SlgID);
                    bool hasSteamID = !string.IsNullOrWhiteSpace(callingUser.SteamId) && !string.IsNullOrWhiteSpace(linkedUser.SteamID);
                    if ((hasSLGID && callingUser.SlgId == linkedUser.SlgID) || (hasSteamID && callingUser.SteamId == linkedUser.SteamID))
                    {
                        if (linkedUser.DiscordID == matchingMember.Id.ToString())
                            ReportCommandInfo(callingUser, $"Eco account is already linked to this Discord account.\nUse `/DL-Unlink` to remove the existing link.");
                        else
                            ReportCommandInfo(callingUser, $"Eco account is already linked to a different Discord account.\nUse `/DL-Unlink` to remove the existing link.");
                        return;
                    }
                    else if (linkedUser.DiscordID == matchingMember.Id.ToString())
                    {
                        ReportCommandError(callingUser, "Discord account is already linked to a different Eco account.");
                        return;
                    }
                }
               
                // Try to Notify the Discord account, that a link has been made and ask for verification
                DiscordMessage message = plugin.Client.SendDMAsync(matchingMember, null, MessageBuilder.Discord.GetVerificationDM(callingUser)).Result;

                // This message can be null, when the target user has blocked direct messages from guild members.
                if(message != null)
                {
                   _ = message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                   _ = message.CreateReactionAsync(DLConstants.DENY_EMOJI);
                }
                else
                {
                    ReportCommandError(callingUser, $"Failed to send direct message to {matchingMember.Username}.\nPlease check your privacy settings and verify, that members of your server are allowed to send you direct messages.");
                    return;
                }

                // Create a linked user from the combined Eco and Discord info
                UserLinkManager.AddLinkedUser(callingUser, matchingMember.Id.ToString(), matchingMember.Guild.Id.ToString());


                // Notify the Eco user that the link has been created and that verification is required
                ReportCommandInfo(callingUser, $"Your account has been linked.\nThe link requires verification before becoming active.\nInstructions have been sent to the linked Discord account.");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Unlinks the Eco account from a linked Discord account.", "DL-Unlink", ChatAuthorizationLevel.User)]
        public static async Task UnlinkDiscordAccount(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                bool result = UserLinkManager.RemoveLinkedUser(callingUser);
                if (result)
                    ReportCommandInfo(callingUser, $"Discord account unlinked.");
                else
                    ReportCommandError(callingUser, $"No linked Discord account could be found.");
            }, callingUser);
        }

        #endregion

        #region Trades

        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store.", "DL-Trades", ChatAuthorizationLevel.User)]
        public static async Task Trades(User callingUser, string searchName)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.Trades(CommandInterface.Eco, callingUser, searchName);
            }, callingUser);
        }

        // Wrapper for the Trades command in order to facilitate more command aliases
        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store.", "DLT", ChatAuthorizationLevel.User)]
        public static async Task Trade(User user, string searchName)
        {
            Trades(user, searchName);
        }

        [ChatSubCommand("DiscordLink", "Creates a live updated display of available trades by player, tag, item or store", "DL-WatchTradeDisplay", ChatAuthorizationLevel.User)]
        public static async Task AddTradeWatcherDisplay(User callingUser, string searchName)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.AddTradeWatcher(CommandInterface.Eco, callingUser, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the live updated display of available trades for the player, tag, item or store.", "DL-UnwatchTradeDisplay", ChatAuthorizationLevel.User)]
        public static async Task RemoveTradeWatcherDisplay(User callingUser, string searchName)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.RemoveTradeWatcher(CommandInterface.Eco, callingUser, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a feed where the bot will post trades filtered by the search query, as they occur ingame. The search query can filter by player, tag, item or store.", "DL-WatchTradeFeed", ChatAuthorizationLevel.User)]
        public static async Task AddTradeWatcherFeed(User callingUser, string searchName)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.AddTradeWatcher(CommandInterface.Eco, callingUser, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the trade watcher feed for a player, tag, item or store.", "DL-UnwatchTradeFeed", ChatAuthorizationLevel.User)]
        public static async Task RemoveTradeWatcherFeed(User callingUser, string searchName)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.RemoveTradeWatcher(CommandInterface.Eco, callingUser, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Lists all trade watchers for the calling user.", "DL-TradeWatchers", ChatAuthorizationLevel.User)]
        public static async Task ListTradeWatchers(User callingUser)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.ListTradeWatchers(CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Snippets

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord to Eco.", "DL-Snippet", ChatAuthorizationLevel.User)]
        public static void Snippet(User callingUser, string snippetKey = "")
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.Snippet(CommandInterface.Eco, callingUser, CommandInterface.Eco, callingUser.Name, snippetKey);
            }, callingUser);
        }

        #endregion

        #region Message Relaying

        [ChatSubCommand("DiscordLink", "Sends a message to a specific server and channel.", ChatAuthorizationLevel.Admin)]
        public static async Task SendMessageToDiscordChannel(User callingUser, string channelNameOrID, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                DiscordChannel channel = plugin.Client.ChannelByNameOrID(channelNameOrID);
                if (channel == null)
                {
                    ReportCommandError(callingUser, $"No channel with the name or ID \"{channelNameOrID}\" could be found.");
                    return;
                }

                _ = plugin.Client.SendMessageAsync(channel, $"**{callingUser.Name.Replace("@", "")}**: {message}");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco server message to all online users.", "DL-ServerMessage", ChatAuthorizationLevel.Admin)]
        public static async Task ServerMessageToAll(User callingUser, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendServerMessage(CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco server message to the specified user.", "DL-ServermessageUser", ChatAuthorizationLevel.Admin)]
        public static async Task ServerMessageToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendServerMessage(CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an info box message to all online users.", "DL-Announce", ChatAuthorizationLevel.Admin)]
        public static async Task AnnouncementToAll(User callingUser, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an info box message to the specified user.", "DL-AnnounceUser", ChatAuthorizationLevel.Admin)]
        public static async Task AnnouncementToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a warning box message to all online users.", "DL-Warning", ChatAuthorizationLevel.Admin)]
        public static async Task WarningToAll(User callingUser, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a warning box message to the specified user.", "DL-WarnUser", ChatAuthorizationLevel.Admin)]
        public static async Task WarningToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an error box message to all online users.", "DL-Error", ChatAuthorizationLevel.Admin)]
        public static async Task ErrorToAll(User callingUser, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an error box message to the specified user.", "DL-ErrorUser", ChatAuthorizationLevel.Admin)]
        public static async Task ErrorToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a notification message to all online and conditionally offline users.", "DL-Notify", ChatAuthorizationLevel.Admin)]
        public static async Task NotificationToAll(User callingUser, string message, bool includeOfflineUsers = true)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendNotification(CommandInterface.Eco, callingUser, message, string.Empty, includeOfflineUsers);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a notification message to the specified user.", "DL-NotifyUser", ChatAuthorizationLevel.Admin)]
        public static async Task NotificationToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendNotification(CommandInterface.Eco, callingUser, message, recipientUserNameOrID, includeOfflineUsers: true);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an OK box popup message to all online users.", "DL-Popup", ChatAuthorizationLevel.Admin)]
        public static async Task PopupToAll(User callingUser, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendPopup(CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an OK box popup message to the specified user.", "DL-PopupUser", ChatAuthorizationLevel.Admin)]
        public static async Task PopupToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendPopup(CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays an info panel to all online users.", "DL-InfoPanel", ChatAuthorizationLevel.Admin)]
        public static async Task InfoPanelToAll(User callingUser, string title, string message)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendInfoPanel(CommandInterface.Eco, callingUser, DLConstants.ECO_PANEL_NOTIFICATION, title, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays an info panel to the specified user.", "DL-InfoPanelUser", ChatAuthorizationLevel.Admin)]
        public static async Task InfoPanelToUser(User callingUser, string title, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                SharedCommands.SendInfoPanel(CommandInterface.Eco, callingUser, DLConstants.ECO_PANEL_NOTIFICATION, title, message, recipientUserNameOrID);
            }, callingUser);
        }

        #endregion

#pragma warning restore CS4014 // Call not awaited
    }
}
