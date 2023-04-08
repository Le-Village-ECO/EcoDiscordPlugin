﻿using Eco.Core.Systems;
using Eco.Core.Utils;
using Eco.EM.Framework.ChatBase;
using Eco.EM.Framework.Text;
using Eco.EW.Tools;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.ModKit.Internal;
using Eco.Shared.Items;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using static Eco.EM.Framework.ChatBase.ChatBase;
using static System.Net.Mime.MediaTypeNames;
using User = Eco.Gameplay.Players.User;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtils
    {
        public enum BoxMessageType
        {
            Info,
            Warning,
            Error
        }

        public static readonly string DefaultChatChannelName = "general";

        #region Lookups

        public static IEnumerable<User> Users => UserManager.Users;
        public static IEnumerable<User> UsersAlphabetical => UserManager.Users.OrderBy(user => user.Name);
        public static User UserByName(string userName) => UserManager.FindUserByName(userName);
        public static User UserByEcoID(int userID) => UserManager.FindUserByID(userID);
        public static User UserByNameOrEcoID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? UserByEcoID(ID) : UserByName(userNameOrID);
        public static User UserBySteamOrSLGID(string steamID, string slgID) => UserManager.FindUserById(steamID, slgID);
        public static IEnumerable<User> OnlineUsers => UserManager.OnlineUsers.NonNull().Where(user => user.Client != null && user.Client.Connected);
        public static IEnumerable<User> OnlineUsersAlphabetical => OnlineUsers.OrderBy(user => user.Name);
        public static User OnlineUserByName(string userName) => OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(userName));
        public static User OnlineUserByEcoID(int userID) => OnlineUsers.FirstOrDefault(user => user.Id == userID);
        public static User OnlineUserByNameEcoID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? OnlineUserByEcoID(ID) : OnlineUserByName(userNameOrID);
        public static User OnlineUserBySteamOrSLGDID(string steamID, string slgID) => OnlineUsers.FirstOrDefault(user => user.SteamId.Equals(steamID) || user.SlgId.Equals(slgID));

        public static IEnumerable<Election> ActiveElections => ElectionManager.Obj.CurrentElections(null).Where(election => election.Valid() && election.State == Shared.Items.ProposableState.Active);
        public static Election ActiveElectionByName(string electionName) => ActiveElections.FirstOrDefault(election => election.Name.EqualsCaseInsensitive(electionName));
        public static Election ActiveElectionByID(int electionID) => ActiveElections.FirstOrDefault(election => election.Id == electionID);
        public static Election ActiveElectionByNameOrID(string electionNameOrID) => int.TryParse(electionNameOrID, out int ID) ? ActiveElectionByID(ID) : ActiveElectionByName(electionNameOrID);

        public static IEnumerable<Law> ActiveLaws => CivicsData.Obj.Laws.NonNull().Where(law => law.State == ProposableState.Active);
        public static Law ActiveLawByName(string lawName) => ActiveLaws.FirstOrDefault(law => law.Name.EqualsCaseInsensitive(lawName));
        public static Law ActiveLawByID(int lawID) => ActiveLaws.FirstOrDefault(law => law.Id == lawID);
        public static Law ActiveLawByNameByNameOrID(string lawNameOrID) => int.TryParse(lawNameOrID, out int ID) ? ActiveLawByID(ID) : ActiveLawByName(lawNameOrID);

        public static IEnumerable<WorkParty> ActiveWorkParties => Registrars.Get<WorkParty>().NonNull().Where(wp => wp.State == ProposableState.Active);
        public static WorkParty ActiveWorkPartyByName(string workPartyName) => ActiveWorkParties.FirstOrDefault(wp => wp.Name.EqualsCaseInsensitive(workPartyName));
        public static WorkParty ActiveWorkPartyByID(int workPartyID) => ActiveWorkParties.FirstOrDefault(wp => wp.Id == workPartyID);
        public static WorkParty ActiveWorkPartyByNameOrID(string workPartyNameOrID) => int.TryParse(workPartyNameOrID, out int ID) ? ActiveWorkPartyByID(ID) : ActiveWorkPartyByName(workPartyNameOrID);

        public static IEnumerable<Demographic> ActiveDemographics => DemographicManager.Obj.ActiveAndValidDemographics(null);
        public static Demographic ActiveDemographicByName(string demographicName) => ActiveDemographics.FirstOrDefault(demographic => demographic.Name.EqualsCaseInsensitive(demographicName));
        public static Demographic ActiveDemographicByID(int demographicID) => ActiveDemographics.FirstOrDefault(demographic => demographic.Id == demographicID);
        public static Demographic ActiveDemographicByNameOrID(string demographicNameOrID) => int.TryParse(demographicNameOrID, out int ID) ? ActiveDemographicByID(ID) : ActiveDemographicByName(demographicNameOrID);

        public static IEnumerable<Title> ActiveTitles => TitleManager.Obj.ActiveTitles;
        public static Title ActiveTitleByName(string titleName) => ActiveTitles.FirstOrDefault(title => title.Name.EqualsCaseInsensitive(titleName));
        public static Title ActiveTitleByID(int titleID) => ActiveTitles.FirstOrDefault(title => title.Id == titleID);
        public static Title ActiveTitleByNameOrID(string titleNameOrID) => int.TryParse(titleNameOrID, out int ID) ? ActiveTitleByID(ID) : ActiveTitleByName(titleNameOrID);

        public static IEnumerable<Currency> Currencies => CurrencyManager.Currencies;
        public static Currency CurrencyByName(string currencyName) => Currencies.FirstOrDefault(c => c.Name.EqualsCaseInsensitive(currencyName));
        public static Currency CurrencyByID(int currencyID) => Currencies.FirstOrDefault(c => c.Id == currencyID);
        public static Currency CurrencyByNameOrID(string currencyNameOrID) => int.TryParse(currencyNameOrID, out int ID) ? CurrencyByID(ID) : CurrencyByName(currencyNameOrID);

        public static IEnumerable<Deed> Deeds => PropertyManager.Obj.Deeds;
        public static Deed DeedByName(string deedName) => Deeds.FirstOrDefault(deed => deed.Name.EqualsCaseInsensitive(deedName));
        public static Deed DeedByID(int deedID) => Deeds.FirstOrDefault(deed => deed.Id == deedID);
        public static Deed DeedByNameOrID(string deedNameOrID) => int.TryParse(deedNameOrID, out int ID) ? DeedByID(ID) : DeedByName(deedNameOrID);

        public static IEnumerable<Skill> Specialties => SkillTree.AllSkillTrees.SelectMany(skilltree => skilltree.ProfessionChildren).Select(skilltree => skilltree.StaticSkill);
        public static Skill SpecialtyByName(string specialtyName) => Specialties.FirstOrDefault(specialty => specialty.Name.EqualsCaseInsensitive(specialtyName));

        public static IEnumerable<Skill> Professions => SkillTree.ProfessionSkillTrees.Select(skilltree => skilltree.StaticSkill);
        public static Skill ProfessionByName(string professionName) => Professions.FirstOrDefault(profession => profession.Name.EqualsCaseInsensitive(professionName));

        #endregion

        #region Calculations & Enumerations

        public static double SecondsPassedOnDay => Simulation.Time.WorldTime.Seconds % DLConstants.SECONDS_PER_DAY;
        public static double SecondsLeftOnDay => DLConstants.SECONDS_PER_DAY - SecondsPassedOnDay;

        public static int NumTotalPlayers => Users.Count();
        public static int NumOnlinePlayers => OnlineUsers.Count();
        public static int NumExhaustedPlayers => Users.Count(user => user.ExhaustionMonitor?.IsExhausted ?? false);

        #endregion

        #region Message Sending

        public static void EnsureChatChannelExists(string channelName)
        {
            if (!ChatChannelExists(channelName))
            {
                CreateChannel(channelName);
            }
        }

        public static bool ChatChannelExists(string channelName)
        {
            return ChannelManager.Obj.Registrar.GetByName(channelName) != null;
        }

        public static Channel CreateChannel(string channelName)
        {
            Channel newChannel = new Channel();
            newChannel.Managers.Add(DemographicManager.Obj.Get(SpecialDemographics.Admins));
            newChannel.Users.Add(DemographicManager.Obj.Get(SpecialDemographics.Everyone));
            newChannel.Name = channelName;
            ChannelManager.Obj.Registrar.Insert(newChannel);

            var channelUsers = newChannel.ChatRecipients;
            foreach (User user in channelUsers)
            {
                var tabSettings = GlobalData.Obj.ChatSettings(user).ChatTabSettings;
                var chatTab = tabSettings.OfType<ChatTabSettingsCommon>().First(tabSettings => tabSettings.Channels.Contains(ChannelManager.Obj.Get(SpecialChannel.General)));
                chatTab.Channels.Add(newChannel);
            }

            return newChannel;
        }

        public static bool SendChatRaw(User sender, string targetAndMessage) // NOTE: Does not trigger ChatMessageSent GameAction
        {
            // TODO: Handle profanity filter
            // TODO: Handle muting
            // TODO: Handle tab opening for DMs
            // TODO: Handle tab opening for channels
            // TODO: Handle access to channels
            // TODO: Handle adding to chat log

            var to = ChatParsingUtils.ResolveReceiver(targetAndMessage, out var messageContent);
            if (to.Failed)
            {
                Logger.Error($"Failed to resolve receiver of message: \"{targetAndMessage}\"");
                return false;
            }
            IChatReceiver receiver = to.Val;

            ChatMessage chatMessage = new ChatMessage(sender, receiver, messageContent);
            foreach (INetClient client in chatMessage.Receiver.ChatRecipients.Select(u => u.Player?.Client).NonNull())
                ChatManager.Obj.RPC("DisplayChatMessage", client, chatMessage.ToBson(client));

            ChatManager.MessageSent.Invoke(chatMessage);
            return true;
        }

        public static bool SendChatToChannel(User sender, string channel, string message)
        {
            return SendChatRaw(sender, $"#{channel} {message}");
        }

        public static bool SendChatToDefaultChannel(User sender, string message)
        {
            return SendChatRaw(sender, $"#{DefaultChatChannelName} {message}");
        }

        public static bool SendChatToUser(User sender, User receiver, string message)
        {
            return SendChatRaw(sender, $"@{receiver.Name} {message}");
        }

        public static bool SendOKBoxToUser(User receiver, string message)
        {
            return ChatBaseExtended.CBOkBox(message, receiver);
        }

        public static bool SendOKBoxToAll(string message)
        {
            return ChatBaseExtended.CBOkBox(message);
        }

        public static bool SendInfoBoxToUser(User receiver, string message)
        {
            return ChatBaseExtended.CBInfoBox(message, receiver, sendToChat: true);
        }

        public static bool SendInfoBoxToAll(string message)
        {
            return ChatBaseExtended.CBInfoBox(message, sendToChat: true);
        }

        public static bool SendWarningBoxToUser(User receiver, string message)
        {
            return ChatBaseExtended.CBWarning(message, receiver, sendToChat: true);
        }

        public static bool SendWarningBoxToAll(string message)
        {
            return ChatBaseExtended.CBWarning(message, sendToChat: true);
        }

        public static bool SendErrorBoxToUser(User receiver, string message)
        {
            return ChatBaseExtended.CBError(message, receiver, sendToChat: true);
        }

        public static bool SendErrorBoxToAll(string message)
        {
            return ChatBaseExtended.CBError(message, sendToChat: true);
        }

        public static bool SendNotificationToUser(User receiver, string message)
        {
            return ChatBaseExtended.CBMail(message, receiver);
        }

        public static bool SendNotificationToAll(string message)
        {
            return ChatBaseExtended.CBMail(message);
        }

        public static bool SendInfoPanelToUser(User receiver, string instance, string title, string message)
        {
            return ChatBaseExtended.CBInfoPane(title, message, instance, receiver, ChatBase.PanelType.InfoPanel);
        }

        public static bool SendInfoPanelToAll(string instance, string title, string message)
        {
            return ChatBaseExtended.CBInfoPane(title, message, instance, ChatBase.PanelType.InfoPanel);
        }

        #endregion
    }
}
