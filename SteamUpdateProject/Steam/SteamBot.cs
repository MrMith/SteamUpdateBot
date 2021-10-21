using System;
using System.Collections.Generic;
using SteamKit2;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteamUpdateProject.DiscordLogic;

namespace SteamUpdateProject.Steam
{
	class SteamBot
	{
		private readonly DiscordBot _discordClient;
		private readonly SteamClient _steamClient;
		private readonly SteamUser _steamUser;
		private readonly System.Timers.Timer _mainChangeTimer = new System.Timers.Timer(250);
		private uint _lastChangeNumber = 0;
		private readonly string _user;
		private readonly string _pass;
		private string _authCode, _twoFactorAuth;
		private SteamApps _apps { get; set; }

		public CallbackManager Manager;
		public bool IsRunning;

		public SteamBot(string[] args, DiscordBot bot)
		{
			_discordClient = bot;
			_user = args[0]; //This is a fodder steam account so I snooze
			_pass = args[1];
			_steamClient = new SteamClient();

			_steamUser = _steamClient.GetHandler<SteamUser>();

			IsRunning = true;

			Console.WriteLine("Connecting to Steam...");

			_steamClient.Connect();

			Manager = new CallbackManager(_steamClient);

			Manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
			Manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
			Manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
			Manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
			Manager.Subscribe<SteamApps.PICSChangesCallback>(AppChanges);
		}

		async void MainChangeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				await _apps.PICSGetChangesSince(_lastChangeNumber, true, false);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error:" + ex.Message);
			}
		}

		/// <summary>
		/// Debug logging, comment this out and I know where all of my temp logging is and can remove it.
		/// </summary>
		public void LogShit(string Log) => Console.WriteLine(Log);

		/// <summary>
		/// Main logic to loop through all of steam's changes.
		/// </summary>
		/// <param name="callback"></param>
		async void AppChanges(SteamApps.PICSChangesCallback callback)
		{
			if (_lastChangeNumber == callback.CurrentChangeNumber) return;
			_lastChangeNumber = callback.CurrentChangeNumber;

			foreach (KeyValuePair<uint, SteamApps.PICSChangesCallback.PICSChangeData> AppsThatUpdated in callback.AppChanges)
			{
				AppUpdate AppUpdate = new AppUpdate
				{
					AppID = AppsThatUpdated.Key,
					ChangeNumber = callback.CurrentChangeNumber,
				};

				CustomProductInfo FullProductInfo = null;
				try
				{
					FullProductInfo = await GetFullProductInfo(AppsThatUpdated.Key);
				}
				catch
				{
					SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_AppInfoGet, LoggingAndErrorHandler.Platform.Steam);
					return;
				}

				//FullProductInfo = GetFullProductInfo(AppsThatUpdated.Key).Result;

				if (FullProductInfo == null) 
					continue;

				foreach (var CallBackInfo in FullProductInfo.ProductInfo)
				{
					foreach (var CallBackInfoApps in CallBackInfo.Apps)
					{
						KeyValue depotKV = CallBackInfoApps.Value.KeyValues.Children.Find(child => child.Name == "depots");

						AppUpdate = ParseUpdateInformation(AppUpdate, FullProductInfo.IsPublic, depotKV);

						AppUpdate.LastUpdated = DateTime.UtcNow;
						AppUpdate.Name = CallBackInfoApps.Value.KeyValues["common"]["name"].AsString();
						//Console.WriteLine(AppUpdate.Content ? "Content Update for " + AppUpdate.AppID : "Update for " + AppUpdate.AppID);

						using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
						{
							context.AppInfoData.RemoveRange(context.AllApps.FindAll(SubbedApp => SubbedApp == AppUpdate));

							AppInfo appinfo = new AppInfo()
							{
								AppID = AppUpdate.AppID,
								Name = AppUpdate.Name,
								LastUpdated = AppUpdate.LastUpdated
							};

							context.AppInfoData.Add(appinfo);
							context.SaveChanges();
						}
					}
				}

				LoggingAndErrorHandler.Updates++;
				_discordClient.AppUpdated(AppUpdate);
			}
		}

		/// <summary>
		/// See if a KeyValuePair of a steam application update contains information like if its a content update (Aka not store tag updates)
		/// </summary>
		/// <param name="AppUpdate">App being updated</param>
		/// <param name="_isPublic">If this steam application data is public. If false we don't get to know whats happening.</param>
		/// <param name="_depotKV">Steam application's information</param>
		/// <returns>AppUpdate with hopefully updated information like which branch updated.</returns>
		private static AppUpdate ParseUpdateInformation(AppUpdate AppUpdate, bool _isPublic, KeyValue _depotKV)
		{
			if (_depotKV == null || !_isPublic)
				return AppUpdate;

			if (_depotKV["branches"] == null)
				return AppUpdate;

			foreach (KeyValue branchKV in _depotKV["branches"].Children) //One single branch
			{
				foreach (KeyValue timeUpdatedKV in branchKV.Children) //Time updated Key value pair (KVP)
				{
					if (timeUpdatedKV.Name != "timeupdated")
						continue;

					TimeSpan t = DateTime.UtcNow - DateTime.UnixEpoch;

					if ((t.TotalSeconds - double.Parse(timeUpdatedKV.Value)) > 10) // Needed because it can take a couple of seconds to go through the steam pipeline.
						continue;

					AppUpdate.DepoName = branchKV.Name;
					AppUpdate.Content = true;
					LoggingAndErrorHandler.ContentUpdates++;
				}
			}

			return AppUpdate;
		}

		/// <summary>
		/// Gets app's name.
		/// </summary>
		/// <param name="appid"></param>
		/// <returns></returns>
		public async Task<string> GetAppName(uint appid)
		{
			AppInfo CachedInfo = DiscordBot.GetCachedAppInfo(appid, true);

			if (CachedInfo != null)
			{
				return CachedInfo.Name;
			}

			var ProductInfo = await _apps.PICSGetProductInfo(appid, null);

			AppInfo appInfo = new AppInfo()
			{
				AppID = appid,
				Name = "Unknown App"
			};

			if (!ProductInfo.Complete)
				return "Unknown App";

			foreach (var CallBackInfo in ProductInfo.Results)
			{
				foreach (var CallBackInfoApps in CallBackInfo.Apps)
				{
					appInfo.Name = CallBackInfoApps.Value.KeyValues["common"]["name"].AsString();

					using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.AppInfoData.Add(appInfo);
						context.SaveChanges();
					}
				}
			}

			return appInfo.Name;
		}

		/// <summary>
		/// If steam is currently working as expected.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> IsSteamDown()
		{
			var ProductInfo = await _apps.PICSGetProductInfo(570, null); //570 is Dota 2.
			if (ProductInfo.Failed)
			{
				return true;
			}

			return !ProductInfo.Complete;
		}

		public static async Task<ulong> GetAccessToken(uint appid)
		{
			if (!SteamUpdateBot.DiscordClient.IsAppSubscribed(appid))
				return 0; //This helps with rate limiting.

			SteamApps.PICSTokensCallback AppTokenInfo;

			try
			{
				AppTokenInfo = await SteamUpdateBot.SteamClient._apps.PICSGetAccessTokens(appid, null);

				if (AppTokenInfo.AppTokensDenied.Contains(appid) || !AppTokenInfo.AppTokens.ContainsKey(appid)) return 0;

				return AppTokenInfo.AppTokens[appid];
			}
			catch
			{
				SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_AppInfoToken, LoggingAndErrorHandler.Platform.Steam);
				return 0;
			}
		}

		public static async Task<CustomProductInfo> GetFullProductInfo(uint appid)
		{
			CustomProductInfo customProductInfo = new CustomProductInfo();

			SteamApps.PICSRequest request = new SteamApps.PICSRequest(appid);

			ulong AccessToken = await GetAccessToken(appid);

			if (AccessToken == 0)
			{
				try
				{
					customProductInfo.ProductInfo = (await SteamUpdateBot.SteamClient._apps.PICSGetProductInfo(appid, null, false)).Results;
					customProductInfo.IsPublic = true;
				}
				catch
				{
					SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_ProductReqNoToken, LoggingAndErrorHandler.Platform.Steam);
					return null;
				}
			}
			else
			{
				try
				{
					request.AccessToken = AccessToken;
					customProductInfo.IsPublic = false;
					customProductInfo.ProductInfo = (await SteamUpdateBot.SteamClient._apps.PICSGetProductInfo(new List<SteamApps.PICSRequest>() { request }, new List<SteamApps.PICSRequest>() { })).Results;
				}
				catch
				{
					SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_ProductReqYesToken, LoggingAndErrorHandler.Platform.Steam);
					return null;
				}
			}
			return customProductInfo;
		}

		#region CodeThat100PercentIsntFromOtherProjects👀
		void OnConnected(SteamClient.ConnectedCallback callback)
		{
			Console.WriteLine("Connected to Steam! Logging in '{0}'...", _user);

			byte[] sentryHash = null;
			if (File.Exists("sentry.bin"))
			{
				byte[] sentryFile = File.ReadAllBytes("sentry.bin");
				sentryHash = CryptoHelper.SHAHash(sentryFile);
			}

			_steamUser.LogOn(new SteamUser.LogOnDetails
			{
				Username = _user,
				Password = _pass,
				AuthCode = _authCode,
				TwoFactorCode = _twoFactorAuth,
				SentryFileHash = sentryHash,
			});

		}

		void OnDisconnected(SteamClient.DisconnectedCallback callback)
		{
			// after recieving an AccountLogonDenied, we'll be disconnected from steam
			// so after we read an authcode from the user, we need to reconnect to begin the logon flow again

			Console.WriteLine("Disconnected from Steam, reconnecting in 5...");

			Thread.Sleep(TimeSpan.FromSeconds(5));

			_steamClient.Connect();
		}
		void OnLoggedOn(SteamUser.LoggedOnCallback callback)
		{
			bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
			bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

			if (isSteamGuard || is2FA)
			{
				Console.WriteLine("This account is SteamGuard protected!");

				if (is2FA)
				{
					Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
					_twoFactorAuth = Console.ReadLine();
				}
				else
				{
					Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
					_authCode = Console.ReadLine();
				}

				return;
			}

			if (callback.Result != EResult.OK)
			{
				Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

				//isRunning = false;
				return;
			}

			// at this point, we'd be able to perform actions on Steam
			//Console.WriteLine("Test");
			_apps = _steamClient.GetHandler<SteamApps>();

			_mainChangeTimer.Elapsed += (sender, args) => MainChangeTimer_Elapsed(sender, args);
			_mainChangeTimer.AutoReset = true;
			_mainChangeTimer.Interval = 500;
			_mainChangeTimer.Start();
		}
		void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
		{
			int fileSize;
			byte[] sentryHash;
			using (FileStream fs = File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				fs.Seek(callback.Offset, SeekOrigin.Begin);
				fs.Write(callback.Data, 0, callback.BytesToWrite);
				fileSize = (int)fs.Length;

				fs.Seek(0, SeekOrigin.Begin);
				using SHA1 sha = SHA1.Create();
				sentryHash = sha.ComputeHash(fs);
			}
			_steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
			{
				JobID = callback.JobID,

				FileName = callback.FileName,

				BytesWritten = callback.BytesToWrite,
				FileSize = fileSize,
				Offset = callback.Offset,

				Result = EResult.OK,
				LastError = 0,

				OneTimePassword = callback.OneTimePassword,

				SentryFileHash = sentryHash,
			});

		}
		#endregion
	}
}
