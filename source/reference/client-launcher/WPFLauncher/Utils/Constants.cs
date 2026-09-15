namespace WPFLauncher
{
    internal class Constants
    {
        public const string LauncherVersion = "1.0.7";

        #region Patcher

        public const string LauncherUpdaterName = "AtlasLauncher_Update.exe";
        public const string AppData = @"%AppData%\";
        public const string UserPath = "\\Electronic Arts\\Dark Age of Camelot\\Atlas\\user.dat";
        
        public const string
            RemoteVersionUrl = "https://patch.atlasfreeshard.com/version-new.txt";
        public const string
            RemoteFileList = "https://patch.atlasfreeshard.com/patchlist-new.txt"; 
        
        public static string RemoteFilePath;

        #endregion

        #region gameserver

        public const string LiveIP = "play.atlasfreeshard.com";
        public const string PtrIP = "ptr.atlasfreeshard.com";
        public const string QueueApiIP = "https://queue.atlasfreeshard.com";

        #endregion

        #region Player Urls

        public const string RegisterUrl = "https://atlasl.ink/register";
        public const string LinkUrl = "https://atlasl.ink/link-discord";
        public const string PatchNotesUrl = "https://atlasl.ink/patch-notes";
        
        #endregion
        
        #region Messages

        public const string MessageDownloadError = "Error downloading files. Please try again later.";
        public const string MessageInvalidCredentials = "An account with these credentials could not be found. Invalid account name or password.";
        public const string MessageNotInQueue = "Your account is not in the queue. Please re-open the launcher!";
        public const string MessageQueueError = "Error communicating with Queue Service. Please try again later. If this continues to occur please contact Atlas Staff.";
        public const string MessageNoCredentials = "Please enter your account and password.";
        
        public const string DiscordMessage = "Linking the account to Discord is now required to play on Atlas. Would you like to do this now?";
        public const string DiscordCaption = "Game account not linked to Discord";
        public const string DiscordError = "You won't be able to play on Atlas without linking your account to Discord";

        public const string MessageReviewInstallation =
            "There was an error launching the game; please review your installation.";

        #endregion
    }
}