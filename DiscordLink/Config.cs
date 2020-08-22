﻿using Eco.Core.Plugins;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLConfig
    {
        public enum VerificationFlags
        {
            Static = 1 << 0,
            ChannelLinks = 1 << 1,
            All = ~0
        }

        public static class DefaultValues
        {
            public const string DiscordCommandPrefix = "?";
            public const string EcoCommandChannel = "General";
            public const string InviteMessage = "Join us on Discord!\n" + InviteCommandLinkToken;
        }

        public static readonly DLConfig Instance = new DLConfig();
        public static DLConfigData Data { get { return Instance._config.Config; } }
        public PluginConfig<DLConfigData> PluginConfig { get { return Instance._config; } }

        public event EventHandler OnConfigSaved;
        public event EventHandler OnChatlogEnabled;
        public event EventHandler OnChatlogDisabled;
        public event EventHandler OnChatlogPathChanged;
        public event EventHandler OnTokenChanged;

        public const string InviteCommandLinkToken = "[LINK]";

        public const int LINK_VERIFICATION_TIMEOUT_MS = 15000;
        public const int STATIC_VERIFICATION_OUTPUT_DELAY_MS = 2000;
        public const int GUILD_VERIFICATION_OUTPUT_DELAY_MS = 3000;

        private readonly List<String> _verifiedLinks = new List<string>();
        private DLConfigData _prevConfig; // Used to detect differences when the config is saved
        private Timer _linkVerificationTimeoutTimer = null;
        private Timer _guildVerificationOutputTimer = null;
        private Timer _staticVerificationOutputDelayTimer = null;

        private PluginConfig<DLConfigData> _config;

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLConfig()
        {
        }

        private DLConfig()
        {
        }

        public void Initialize()
        {
            _config = new PluginConfig<DLConfigData>("DiscordLink");
            _prevConfig = (DLConfigData)Data.Clone();
            Data.PlayerConfigs.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            Data.ChatChannelLinks.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            Data.EcoStatusDiscordChannels.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            
            Data.PlayerConfigs.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            Data.ChatChannelLinks.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            Data.EcoStatusDiscordChannels.CollectionChanged += (obj, args) => { OnConfigChanged(); };

            DiscordLink.Obj.OnClientStopped += (obj, args) =>
            {
                _verifiedLinks.Clear(); // If we were waiting to verify channel links, we need to clear this list or risk false positives
            };
        }

        public void OnDiscordClientStopped()
        {
            _verifiedLinks.Clear(); // If we were waiting to verify channel links, we need to clear this list or risk false positives
        }

        public void OnConfigChanged()
        {
            // Do not verify if change occurred as this function is going to be called again in that case
            // Do not verify the config in case the bot token has been changed, as the client will be restarted and that will trigger verification
            bool tokenChanged = Data.BotToken != _prevConfig.BotToken;
            bool correctionMade = !Save();

            if (tokenChanged)
            {
                OnTokenChanged?.Invoke(this, EventArgs.Empty);
            }

            if (!correctionMade && !tokenChanged)
            {
                VerifyConfig();
            }
        }

        public void EnqueueFullVerification()
        {
            // Queue up the check for unverified channels
            _linkVerificationTimeoutTimer = new Timer(innerArgs =>
            {
                _linkVerificationTimeoutTimer = null;
                ReportUnverifiedChannels();
                _verifiedLinks.Clear();
            }, null, LINK_VERIFICATION_TIMEOUT_MS, Timeout.Infinite);

            // Avoid writing async while the server is still outputting initialization info
            _staticVerificationOutputDelayTimer = new Timer(innerArgs =>
            {
                _staticVerificationOutputDelayTimer = null;
                VerifyConfig(VerificationFlags.Static);
            }, null, STATIC_VERIFICATION_OUTPUT_DELAY_MS, Timeout.Infinite);
        }

        public void EnqueueGuildVerification()
        {
            _guildVerificationOutputTimer = new Timer(innerArgs =>
            {
                _guildVerificationOutputTimer = null;
                VerifyConfig(VerificationFlags.ChannelLinks);
            }, null, GUILD_VERIFICATION_OUTPUT_DELAY_MS, Timeout.Infinite);
        }

        public void DequeueAllVerification()
        {
            SystemUtil.StopAndDestroyTimer(ref _linkVerificationTimeoutTimer);
            SystemUtil.StopAndDestroyTimer(ref _staticVerificationOutputDelayTimer);
            SystemUtil.StopAndDestroyTimer(ref _guildVerificationOutputTimer);
        }

        public bool Save() // Returns true if no correction was needed
        {
            bool correctionMade = false;

            // Discord Command Prefix
            if (Data.DiscordCommandPrefix != _prevConfig.DiscordCommandPrefix)
            {
                if (string.IsNullOrEmpty(Data.DiscordCommandPrefix))
                {
                    Data.DiscordCommandPrefix = DefaultValues.DiscordCommandPrefix;
                    correctionMade = true;

                    Logger.Info("Command prefix found empty - Resetting to default.");
                }
                Logger.Info("Command prefix changed - Restart required to take effect.");
            }

            // Chat channel links
            foreach (ChannelLink link in Data.ChatChannelLinks)
            {
                if (string.IsNullOrWhiteSpace(link.DiscordChannel)) continue;

                string original = link.DiscordChannel;
                if (link.DiscordChannel != link.DiscordChannel.ToLower()) // Discord channels are always lowercase
                {
                    link.DiscordChannel = link.DiscordChannel.ToLower();
                }

                if (link.DiscordChannel.Contains(" ")) // Discord channels always replace spaces with dashes
                {
                    link.DiscordChannel = link.DiscordChannel.Replace(' ', '-');
                }

                if (link.DiscordChannel != original)
                {
                    correctionMade = true;
                    Logger.Info("Corrected Discord channel name in Channel Link with Guild \"" + link.DiscordGuild + "\" from \"" + original + "\" to \"" + link.DiscordChannel + "\"");
                }
            }

            // Eco status Discord channels
            foreach (EcoStatusChannel statusChannel in Data.EcoStatusDiscordChannels) // TODO[MonzUn] Create a reusable way to fix erronous channel links
            {
                if (string.IsNullOrWhiteSpace(statusChannel.DiscordChannel)) continue;

                string original = statusChannel.DiscordChannel;
                if (statusChannel.DiscordChannel != statusChannel.DiscordChannel.ToLower())
                {
                    statusChannel.DiscordChannel = statusChannel.DiscordChannel.ToLower();
                }

                if (statusChannel.DiscordChannel.Contains(" "))
                {
                    statusChannel.DiscordChannel = statusChannel.DiscordChannel.Replace(' ', '-');
                }

                if (statusChannel.DiscordChannel != original)
                {
                    correctionMade = true;
                    Logger.Info("Corrected Discord channel name in Eco Status Channel with Guild name/ID \"" + statusChannel.DiscordGuild + "\" from \"" + original + "\" to \"" + statusChannel.DiscordChannel + "\"");
                }
            }

            // Chatlog toggle
            if (Data.LogChat && !_prevConfig.LogChat)
            {
                Logger.Info("Chatlog enabled");
                OnChatlogEnabled?.Invoke(this, EventArgs.Empty);
            }
            else if (!Data.LogChat && _prevConfig.LogChat)
            {
                Logger.Info("Chatlog disabled");
                OnChatlogDisabled?.Invoke(this, EventArgs.Empty);
            }

            // Chatlog path
            if (string.IsNullOrEmpty(Data.ChatlogPath))
            {
                Data.ChatlogPath = Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\Chatlog.txt";
                correctionMade = true;
            }

            if (Data.ChatlogPath != _prevConfig.ChatlogPath)
            {
                Logger.Info("Chatlog path changed. New path: " + Data.ChatlogPath);
                OnChatlogPathChanged?.Invoke(this, EventArgs.Empty);
            }

            // Eco command channel
            if (string.IsNullOrEmpty(Data.EcoCommandChannel))
            {
                Data.EcoCommandChannel = DefaultValues.EcoCommandChannel;
                correctionMade = true;
            }

            // Invite Message
            if (string.IsNullOrEmpty(Data.InviteMessage))
            {
                Data.InviteMessage = DefaultValues.InviteMessage;
                correctionMade = true;
            }

            OnConfigSaved?.Invoke(this, EventArgs.Empty);
            _prevConfig = (DLConfigData)Data.Clone();

            return !correctionMade;
        }

        public void VerifyConfig(VerificationFlags verificationFlags = VerificationFlags.All)
        {
            List<string> errorMessages = new List<string>();
            if (DiscordLink.Obj.DiscordClient == null)
            {
                errorMessages.Add("[General Verification] No Discord client connected.");
            }

            if (verificationFlags.HasFlag(VerificationFlags.Static))
            {
                // Bot Token
                if (String.IsNullOrWhiteSpace(Data.BotToken))
                {
                    errorMessages.Add("[Bot Token] Bot token not configured. See Github page for install instructions.");
                }

                // Player configs
                foreach (DiscordPlayerConfig playerConfig in Data.PlayerConfigs)
                {
                    if (string.IsNullOrWhiteSpace(playerConfig.Username)) continue;

                    bool found = false;
                    foreach (User user in UserManager.Users)
                    {
                        if (user.Name == playerConfig.Username)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        errorMessages.Add("[Player Configs] No user with name \"" + playerConfig.Username + "\" was found");
                    }
                }

                // Eco command channel
                if (!string.IsNullOrWhiteSpace(Data.EcoCommandChannel) && Data.EcoCommandChannel.Contains("#"))
                {
                    errorMessages.Add("[Eco Command Channel] Channel name contains a channel indicator (#). The channel indicator will be added automatically and adding one manually may cause message sending to fail");
                }

                if (!string.IsNullOrWhiteSpace(Data.InviteMessage) && !Data.InviteMessage.Contains(InviteCommandLinkToken))
                {
                    errorMessages.Add("[Invite Message] Message does not contain the invite link token " + InviteCommandLinkToken + ". If the invite link has been added manually, consider adding it to the network config instead");
                }

                // Report errors
                if (errorMessages.Count <= 0)
                {
                    Logger.Info("Static configuration verification completed without errors");
                }
                else
                {
                    string concatenatedMessages = "";
                    foreach (string message in errorMessages)
                    {
                        concatenatedMessages += message + "\n";
                    }
                    Logger.Error("Static configuration errors detected!\n" + concatenatedMessages.Trim());
                }
            }

            if (verificationFlags.HasFlag(VerificationFlags.ChannelLinks) && DiscordLink.Obj.DiscordClient != null) // Discord guild and channel information isn't available the first time this function is called
            {
                // Channel links
                foreach (ChannelLink chatLink in Data.ChatChannelLinks)
                {
                    if (string.IsNullOrWhiteSpace(chatLink.DiscordGuild) || string.IsNullOrWhiteSpace(chatLink.DiscordChannel) || string.IsNullOrWhiteSpace(chatLink.EcoChannel)) continue;

                    var guild = DiscordLink.Obj.GuildByNameOrId(chatLink.DiscordGuild);
                    if (guild == null)
                    {
                        continue; // The channel will always fail if the guild fails
                    }
                    var channel = guild.ChannelByNameOrId(chatLink.DiscordChannel);
                    if (channel == null)
                    {
                        continue;
                    }

                    string linkID = chatLink.ToString();
                    if (!_verifiedLinks.Contains(linkID))
                    {
                        _verifiedLinks.Add(linkID);
                        Logger.Info("Channel Link Verified: " + linkID);
                    }
                }

                // Eco status Discord channels
                foreach (EcoStatusChannel statusLink in Data.EcoStatusDiscordChannels)
                {
                    if (string.IsNullOrWhiteSpace(statusLink.DiscordGuild) || string.IsNullOrWhiteSpace(statusLink.DiscordChannel)) continue;

                    var guild = DiscordLink.Obj.GuildByNameOrId(statusLink.DiscordGuild);
                    if (guild == null)
                    {
                        continue; // The channel will always fail if the guild fails
                    }
                    var channel = guild.ChannelByNameOrId(statusLink.DiscordChannel);
                    if (channel == null)
                    {
                        continue;
                    }

                    string linkID = statusLink.ToString();
                    if (!_verifiedLinks.Contains(linkID))
                    {
                        _verifiedLinks.Add(linkID);
                        Logger.Info("Channel Link Verified: " + linkID);
                    }
                }

                if (_verifiedLinks.Count >= Data.ChatChannelLinks.Count + Data.EcoStatusDiscordChannels.Count)
                {
                    Logger.Info("All channel links sucessfully verified");
                }
                else if (_linkVerificationTimeoutTimer == null) // If no timer is used, then the discord guild info should already be set up
                {
                    ReportUnverifiedChannels();
                }
            }
        }

        private void ReportUnverifiedChannels()
        {
            if (_verifiedLinks.Count >= Data.ChatChannelLinks.Count + Data.EcoStatusDiscordChannels.Count) return; // All are verified; nothing to report.

            List<string> unverifiedLinks = new List<string>();
            foreach (ChannelLink chatLink in Data.ChatChannelLinks)
            {
                if (string.IsNullOrWhiteSpace(chatLink.DiscordGuild) || string.IsNullOrWhiteSpace(chatLink.DiscordChannel) || string.IsNullOrWhiteSpace(chatLink.EcoChannel)) continue;

                string linkID = chatLink.ToString();
                if (!_verifiedLinks.Contains(linkID))
                {
                    unverifiedLinks.Add(linkID);
                }
            }

            foreach (EcoStatusChannel statusLink in Data.EcoStatusDiscordChannels)
            {
                if (string.IsNullOrWhiteSpace(statusLink.DiscordGuild) || string.IsNullOrWhiteSpace(statusLink.DiscordChannel)) continue;

                string linkID = statusLink.ToString();
                if (!_verifiedLinks.Contains(linkID))
                {
                    unverifiedLinks.Add(linkID);
                }
            }

            if (unverifiedLinks.Count > 0)
            {
                Logger.Info("Unverified channels detected:\n" + String.Join("\n", unverifiedLinks));
            }
        }

        public ChannelLink GetChannelLinkFromDiscordChannel(string guild, string channelName)
        {
            foreach (ChannelLink channelLink in Data.ChatChannelLinks)
            {
                if (channelLink.DiscordGuild.ToLower() == guild.ToLower() && channelLink.DiscordChannel.ToLower() == channelName.ToLower())
                {
                    return channelLink;
                }
            }
            return null;
        }

        public ChannelLink GetChannelLinkFromEcoChannel(string channelName)
        {
            foreach (ChannelLink channelLink in Data.ChatChannelLinks)
            {
                if (channelLink.EcoChannel.ToLower() == channelName.ToLower())
                {
                    return channelLink;
                }
            }
            return null;
        }
    }

    public class DiscordChannelIdentifier : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public string Guild { get; set; }
        public string Channel { get; set; }
    }

    public class DiscordPlayerConfig : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [Description("ID of the user")]
        public string Username { get; set; }

        private DiscordChannelIdentifier _defaultChannel = new DiscordChannelIdentifier();
        public DiscordChannelIdentifier DefaultChannel
        {
            get { return _defaultChannel; }
            set { _defaultChannel = value; }
        }

        public class DiscordChannelIdentifier
        {
            public string Guild { get; set; }
            public string Channel { get; set; }
        }
    }

    public class DLConfigData : ICloneable
    {
        public object Clone() // Be careful not to change the original object here as that will trigger endless recursion.
        {
            return new DLConfigData
            {
                BotToken = this.BotToken,
                DiscordCommandPrefix = this.DiscordCommandPrefix,
                ServerName = this.ServerName,
                ServerDescription = this.ServerDescription,
                ServerLogo = this.ServerLogo,
                ServerAddress = this.ServerAddress,
                Debug = this.Debug,
                LogChat = this.LogChat,
                ChatlogPath = this.ChatlogPath,
                PlayerConfigs = new ObservableCollection<DiscordPlayerConfig>(this.PlayerConfigs.Select(t => t.Clone()).Cast<DiscordPlayerConfig>()),
                ChatChannelLinks = new ObservableCollection<ChannelLink>(this.ChatChannelLinks.Select(t => t.Clone()).Cast<ChannelLink>()),
                EcoStatusDiscordChannels = new ObservableCollection<EcoStatusChannel>(this.EcoStatusDiscordChannels.Select(t => t.Clone()).Cast<EcoStatusChannel>())
            };
        }

        [Description("The token provided by the Discord API to allow access to the bot. This setting can be changed while the server is running and will in that case trigger a reconnection to Discord."), Category("Bot Configuration")]
        public string BotToken { get; set; }

        [Description("The prefix to put before commands in order for the Discord bot to recognize them as such. This setting requires a restart to take effect."), Category("Command Settings")]
        public string DiscordCommandPrefix { get; set; } = DLConfig.DefaultValues.DiscordCommandPrefix;

        [Description("Discord channels in which to display the Eco status view. WARNING - Any messages in these channels will be deleted. This setting can be changed while the server is running."), Category("Channel Configuration")]
        public ObservableCollection<EcoStatusChannel> EcoStatusDiscordChannels { get; set; } = new ObservableCollection<EcoStatusChannel>();

        [Description("The name of the Eco server, overriding the name configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerName { get; set; }

        [Description("The description of the Eco server, overriding the description configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerDescription { get; set; }

        [Description("The logo of the server as a URL. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerLogo { get; set; }

        [Description("The address (URL or IP) of the server. Overrides the automatically detected IP. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerAddress { get; set; }

        [Description("A mapping from user to user config parameters. This setting can be changed while the server is running.")]
        public ObservableCollection<DiscordPlayerConfig> PlayerConfigs = new ObservableCollection<DiscordPlayerConfig>();

        [Description("Channels to connect together. This setting can be changed while the server is running."), Category("Channel Configuration")]
        public ObservableCollection<ChannelLink> ChatChannelLinks { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Enables debugging output to the console. This setting can be changed while the server is running."), Category("Debugging")]
        public bool Debug { get; set; } = false;

        [Description("Enables logging of chat messages into the file at Chatlog Path. This setting can be changed while the server is running."), Category("Chatlog Configuration")]
        public bool LogChat { get; set; } = false;

        [Description("The path to the chatlog file, including file name and extension. This setting can be changed while the server is running, but the existing chatlog will not transfer."), Category("Chatlog Configuration")]
        public string ChatlogPath { get; set; } = Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\Chatlog.txt";

        [Description("The Eco chat channel to use for commands that outputs public messages, excluding the initial # character. This setting can be changed while the server is running."), Category("Command Settings")]
        public string EcoCommandChannel { get; set; } = DLConfig.DefaultValues.EcoCommandChannel;

        [Description("The message to use for the /DiscordInvite command. The invite link is fetched from the network config and will replace the token " + DLConfig.InviteCommandLinkToken + ". This setting can be changed while the server is running."), Category("Command Settings")]
        public string InviteMessage { get; set; } = DLConfig.DefaultValues.InviteMessage;
    }

    public class ChannelLink : ICloneable
    {
        public object Clone()
        {
            return MemberwiseClone();
        }
    
        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel + " <--> " + EcoChannel + " (Chat Link)";
        }
    
        [Description("Discord Guild (Server) by name or ID.")]
        public string DiscordGuild { get; set; }
    
        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; }
    
        [Description("Eco Channel to use.")]
        public string EcoChannel { get; set; }
    
        [Description("Allow mentions of usernames to be forwarded from Eco to the Discord channel")]
        public bool AllowUserMentions { get; set; } = true;
    
        [Description("Allow mentions of roles to be forwarded from Eco to the Discord channel")]
        public bool AllowRoleMentions { get; set; } = true;
    
        [Description("Allow mentions of channels to be forwarded from Eco to the Discord channel")]
        public bool AllowChannelMentions { get; set; } = true;
    }
    
    public class EcoStatusChannel : ICloneable
    {
        public object Clone()
        {
            return MemberwiseClone();
        }
    
        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel + " (Eco Status)";
        }
    
        [Description("Discord Guild (Server) by name or ID.")]
        public string DiscordGuild { get; set; }
    
        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; }
    
        [Description("Display the server name in the status message.")]
        public bool UseName { get; set; } = true;
    
        [Description("Display the server description in the status message.")]
        public bool UseDescription { get; set; } = false;
    
        [Description("Display the server logo in the status message.")]
        public bool UseLogo { get; set; } = true;
    
        [Description("Display the server IP address in the status message.")]
        public bool UseAddress { get; set; } = true;
    
        [Description("Display the number of online players in the status message.")]
        public bool UsePlayerCount { get; set; } = true;
    
        [Description("Display the list of online players in the status message.")]
        public bool UsePlayerList { get; set; } = true;
    
        [Description("Display the time since the world was created in the status message.")]
        public bool UseTimeSinceStart { get; set; } = true;
    
        [Description("Display the time remaining until meteor impact in the status message.")]
        public bool UseTimeRemaining { get; set; } = true;
    
        [Description("Display a boolean for if the metoer has hit yet or not, in the status message.")]
        public bool UseMeteorHasHit { get; set; } = false;
    }
}
