using MongoDB.Driver;
using SteamKit2;
using SteamKit2.Authentication;
using SteamUpdateProject.Discord;
using SteamUpdateProject.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;

namespace SteamUpdateProject.Steam
{
	/// <summary>
	/// This class handles everything related to talking to Steam and using SteamKit2.
	/// </summary>
	internal class SteamBot
	{
		private readonly DiscordBot _discordClient;
		private readonly SteamClient _steamClient;
		private readonly SteamUser _steamUser;
		private readonly System.Timers.Timer _mainChangeTimer = new System.Timers.Timer(250);
		private uint _lastChangeNumber = 0;
		private readonly string _user;
		private readonly string _pass;
		private string _previouslyStoredGuardData;
		private SteamApps Apps { get; set; }

		public CallbackManager Manager;
		public bool IsRunning;

		public SteamBot(string name, string password, DiscordBot bot)
		{
			_discordClient = bot;
			_user = name;
			_pass = password;
			_steamClient = new SteamClient();

			_steamUser = _steamClient.GetHandler<SteamUser>();

			IsRunning = true;

			Console.WriteLine("Connecting to Steam...");

			_steamClient.Connect();

			Manager = new CallbackManager(_steamClient);

			Manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
			Manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
			Manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
			Manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
			Manager.Subscribe<SteamApps.PICSChangesCallback>(AppChanges);
		}

		private async void MainChangeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				await Apps.PICSGetChangesSince(_lastChangeNumber, true, false);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error:" + ex.Message);
			}
		}

		/// <summary>
		/// Debug logging, comment this out and I know where all of my temp logging is and can remove it.
		/// </summary>
		public static void Log(string log)
		{
			Console.WriteLine(log);
		}

		/// <summary>
		/// Main logic to loop through all of steam's changes.
		/// </summary>
		/// <param name="callback"></param>
		private async void AppChanges(SteamApps.PICSChangesCallback callback)
		{
			if (_lastChangeNumber == callback.CurrentChangeNumber)
				return;

			_lastChangeNumber = callback.CurrentChangeNumber;

			///Go through all of the app changes
			foreach (KeyValuePair<uint, SteamApps.PICSChangesCallback.PICSChangeData> appsThatUpdated in callback.AppChanges)
			{
				AppUpdate appUpdate = new AppUpdate
				{
					AppID = appsThatUpdated.Key,
					ChangeNumber = callback.CurrentChangeNumber,
				};

				CustomProductInfo fullProductInfo = null;
				try
				{
					fullProductInfo = await GetFullProductInfo(appsThatUpdated.Key);
				}
				catch
				{
					SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_AppInfoGet, LoggingAndErrorHandler.Platform.Steam);
					return;
				}

				if (fullProductInfo == null)
					continue;

				//Go through steam applications information that we've gathered.
				foreach (SteamApps.PICSProductInfoCallback callBackInfo in fullProductInfo.ProductInfo)
				{
					//Goes through the apps in the product info (This is because you can go through packages which could contain multiple apps but in this case we typically dont)
					foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> callBackInfoApps in callBackInfo.Apps)
					{
						//Get Depos (Where the downloadable content is)
						KeyValue depotKV = callBackInfoApps.Value.KeyValues.Children.Find(child => child.Name == "depots");

						appUpdate = ParseUpdateInformation(appUpdate, fullProductInfo.IsPublic, depotKV);

						appUpdate.LastUpdated = DateTime.UtcNow;
						appUpdate.Name = callBackInfoApps.Value.KeyValues["common"]["name"].AsString();

						IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

						IMongoCollection<AppInfo> aI_Collection = db.GetCollection<AppInfo>(AppInfo.DBName);

						FilterDefinition<AppInfo> aI_Filter = Builders<AppInfo>.Filter.Eq("AppID", appUpdate.AppID);

						//AI_Collection.DeleteMany(AI_Filter);

						AppInfo appinfo = new AppInfo()
						{
							AppID = appUpdate.AppID,
							Name = appUpdate.Name,
							LastUpdated = appUpdate.LastUpdated,
							DepoName = appUpdate.DepoName
						};

						aI_Collection.InsertOne(appinfo);
					}
				}

				LoggingAndErrorHandler.Updates++;
				_discordClient.AppUpdated(appUpdate);
			}
		}

		/// <summary>
		/// See if a KeyValuePair of a steam application update contains information like if its a content update (Aka not store tag updates)
		/// </summary>
		/// <param name="appUpdate">App being updated</param>
		/// <param name="isPublic">If this steam application data is public. If false we don't get to know whats happening.</param>
		/// <param name="depotKV">Steam application's information</param>
		/// <returns>AppUpdate with hopefully updated information like which branch updated.</returns>
		private static AppUpdate ParseUpdateInformation(AppUpdate appUpdate, bool isPublic, KeyValue depotKV)
		{
			if (depotKV == null || !isPublic)
				return appUpdate;

			if (depotKV["branches"] == null)
				return appUpdate;

			foreach (KeyValue branchKV in depotKV["branches"].Children) //One single branch
			{
				foreach (KeyValue timeUpdatedKV in branchKV.Children) //Time updated Key value pair (KVP)
				{
					if (timeUpdatedKV.Name != "timeupdated")
						continue;

					TimeSpan t = DateTime.UtcNow - DateTime.UnixEpoch;

					if ((t.TotalSeconds - double.Parse(timeUpdatedKV.Value)) > 10) // Needed because it can take a couple of seconds to go through the steam pipeline.
						continue;

					appUpdate.DepoName = branchKV.Name;
					appUpdate.Content = true;
					LoggingAndErrorHandler.ContentUpdates++;
				}
			}

			return appUpdate;
		}

		/// <summary>
		/// Gets Steam Application's name.
		/// </summary>
		/// <param name="appid"></param>
		/// <returns>Steam Application's name for given appid.</returns>
		public async Task<string> GetAppName(uint appid)
		{
			AppInfo cachedInfo = DiscordBot.GetCachedAppInfo(appid, true);

			if (cachedInfo != null)
			{
				return cachedInfo.Name;
			}

			AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet productInfo = await Apps.PICSGetProductInfo(new SteamApps.PICSRequest(appid), null);

			AppInfo appInfo = new AppInfo()
			{
				AppID = appid,
				Name = "Unknown App"
			};

			if (!productInfo.Complete)
				return "Unknown App";

			foreach (SteamApps.PICSProductInfoCallback callBackInfo in productInfo.Results)
			{
				foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> callBackInfoApps in callBackInfo.Apps)
				{
					appInfo.Name = callBackInfoApps.Value.KeyValues["common"]["name"].AsString();

					IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

					IMongoCollection<AppInfo> aI_Collection = db.GetCollection<AppInfo>(AppInfo.DBName);

					//FilterDefinition<AppInfo> AI_Filter = Builders<AppInfo>.Filter.Eq("AppID", appid);

					//AI_Collection.DeleteMany(AI_Filter);

					aI_Collection.InsertOne(appInfo);
				}
			}

			return appInfo.Name;
		}

		/// <summary>
		/// If steam is currently working as expected.
		/// </summary>
		/// <returns>True if Steam is currenly experiencing issues and False if everything is working as intended.</returns>
		public async Task<bool> IsSteamDown()
		{
			try
			{
				if (Apps == null)
					return true;

				//570 is Dota 2.
				AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet productInfo = await Apps.PICSGetProductInfo(new SteamApps.PICSRequest(570), null);

				return productInfo.Failed || !productInfo.Complete;
			}
			catch
			{
				return true;
			}
		}

		/// <summary>
		/// Gets a token for when doing a <see cref="SteamKit2.SteamApps.PICSGetProductInfo"/> request.
		/// </summary>
		/// <param name="appid">Steam Application ID.</param>
		/// <returns>Token for <see cref="SteamKit2.SteamApps.PICSGetProductInfo"/> request.</returns>
		public static async Task<ulong> GetAccessToken(uint appid)
		{
			if (!SteamUpdateBot.DiscordClient.IsAppSubscribed(appid))
				return 0; //This helps with rate limiting.

			try
			{
				//This randomly errors and has cost me sleep so therefor slap a try-catch on it.
				SteamApps.PICSTokensCallback appTokenInfo = await SteamUpdateBot.SteamClient.Apps.PICSGetAccessTokens(appid, null);

				return appTokenInfo.AppTokensDenied.Contains(appid) || !appTokenInfo.AppTokens.ContainsKey(appid) ? 0 : appTokenInfo.AppTokens[appid];
			}
			catch
			{
				SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_AppInfoToken, LoggingAndErrorHandler.Platform.Steam);
				return 0;
			}
		}

		/// <summary>
		/// Gets the product information for a steam application which contains information like names and depos for its downloadable contents.
		/// </summary>
		/// <param name="appid">Steam Application ID.</param>
		/// <returns><see cref="SteamApps.PICSProductInfoCallback"/> with if the application needs a token or not.</returns>
		public static async Task<CustomProductInfo> GetFullProductInfo(uint appid)
		{
			CustomProductInfo customProductInfo = new CustomProductInfo();

			SteamApps.PICSRequest request = new SteamApps.PICSRequest(appid);

			ulong accessToken = await GetAccessToken(appid);

			//If the accesstoken is 0 then we don't have a token to get more detailed information so we get the publicly available information
			//Also randomly errors and that causes me physical pain.
			if (accessToken == 0)
			{
				try
				{
					customProductInfo.ProductInfo = (await SteamUpdateBot.SteamClient.Apps.PICSGetProductInfo(new SteamApps.PICSRequest(appid), null, false)).Results;
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
					request.AccessToken = accessToken;
					customProductInfo.IsPublic = false;
					customProductInfo.ProductInfo = (await SteamUpdateBot.SteamClient.Apps.PICSGetProductInfo(new List<SteamApps.PICSRequest>() { request }, new List<SteamApps.PICSRequest>() { })).Results;
				}
				catch
				{
					SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Steam_ProductReqYesToken, LoggingAndErrorHandler.Platform.Steam);
					return null;
				}
			}

			return customProductInfo;
		}

		#region Generic boilerplate steam connection code
		//All code that is either boilerplate or I haven't written myself.
		public async void OnConnected(SteamClient.ConnectedCallback callback)
		{
			Console.WriteLine("Connected to Steam! Logging in '{0}'...", _user);

			bool shouldRememberPassword = false;

			// Begin authenticating via credentials
			CredentialsAuthSession authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
			{
				Username = _user,
				Password = _pass,
				IsPersistentSession = shouldRememberPassword,

				// See NewGuardData comment below
				GuardData = _previouslyStoredGuardData,

				/// <see cref="UserConsoleAuthenticator"/> is the default authenticator implemention provided by SteamKit
				/// for ease of use which blocks the thread and asks for user input to enter the code.
				/// However, if you require special handling (e.g. you have the TOTP secret and can generate codes on the fly),
				/// you can implement your own <see cref="SteamKit2.Authentication.IAuthenticator"/>.
				Authenticator = new UserConsoleAuthenticator(),
			});

			// Starting polling Steam for authentication response
			AuthPollResult pollResponse = await authSession.PollingWaitForResultAsync();

			if (pollResponse.NewGuardData != null)
			{
				// When using certain two factor methods (such as email 2fa), guard data may be provided by Steam
				// for use in future authentication sessions to avoid triggering 2FA again (this works similarly to the old sentry file system).
				// Do note that this guard data is also a JWT token and has an expiration date.
				_previouslyStoredGuardData = pollResponse.NewGuardData;
			}

			// Logon to Steam with the access token we have received
			// Note that we are using RefreshToken for logging on here
			_steamUser.LogOn(new SteamUser.LogOnDetails
			{
				Username = pollResponse.AccountName,
				AccessToken = pollResponse.RefreshToken,
				ShouldRememberPassword = shouldRememberPassword, // If you set IsPersistentSession to true, this also must be set to true for it to work correctly
			});

			// This is not required, but it is possible to parse the JWT access token to see the scope and expiration date.
			ParseJsonWebToken(pollResponse.AccessToken, nameof(pollResponse.AccessToken));
			ParseJsonWebToken(pollResponse.RefreshToken, nameof(pollResponse.RefreshToken));
		}

		private void OnDisconnected(SteamClient.DisconnectedCallback callback)
		{
			// after recieving an AccountLogonDenied, we'll be disconnected from steam
			// so after we read an authcode from the user, we need to reconnect to begin the logon flow again

			Console.WriteLine("Disconnected from Steam, reconnecting in 5...");

			Thread.Sleep(TimeSpan.FromSeconds(5));

			_steamClient.Connect();
		}

		void OnLoggedOn(SteamUser.LoggedOnCallback callback)
		{
			if (callback.Result != EResult.OK)
			{
				Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

				IsRunning = false;
				return;
			}

			Console.WriteLine("Successfully logged on!");

			Apps = _steamClient.GetHandler<SteamApps>();

			_mainChangeTimer.Elapsed += (sender, args) => MainChangeTimer_Elapsed(sender, args);
			_mainChangeTimer.AutoReset = true;
			_mainChangeTimer.Interval = 500;
			_mainChangeTimer.Start();
		}

		void OnLoggedOff(SteamUser.LoggedOffCallback callback)
		{
			Console.WriteLine("Logged off of Steam: {0}", callback.Result);
		}

		// This is simply showing how to parse JWT, this is not required to login to Steam
		void ParseJsonWebToken(string token, string name)
		{
			// You can use a JWT library to do the parsing for you
			string[] tokenComponents = token.Split('.');

			// Fix up base64url to normal base64
			string base64 = tokenComponents[1].Replace('-', '+').Replace('_', '/');

			if (base64.Length % 4 != 0)
			{
				base64 += new string('=', 4 - base64.Length % 4);
			}

			byte[] payloadBytes = Convert.FromBase64String(base64);

			// Payload can be parsed as JSON, and then fields such expiration date, scope, etc can be accessed
			JsonDocument payload = JsonDocument.Parse(payloadBytes);

			// For brevity we will simply output formatted json to console
			string formatted = JsonSerializer.Serialize(payload, new JsonSerializerOptions
			{
				WriteIndented = true,
			});
			Console.WriteLine($"{name}: {formatted}");
			Console.WriteLine();
		}
		#endregion
	}
}
