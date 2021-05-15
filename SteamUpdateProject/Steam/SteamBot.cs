using System;
using System.Collections;
using System.Collections.Generic;
using SteamKit2;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteamUpdateProject.Discord;
using System.Linq;
using System.Data.Entity;

namespace SteamUpdateProject.Steam
{
	class SteamBot
	{
		readonly DiscordBot DiscordClient;
		readonly System.Timers.Timer MainChangeTimer = new System.Timers.Timer(50);
		uint LastChangeNumber = 0;
		public double UpdatesProcessed;

		readonly SteamClient steamClient;
		public CallbackManager manager;
		SteamApps Apps { get; set; }
		readonly SteamUser steamUser;

		public bool isRunning;
		private readonly string user;
		private readonly string pass;
		string authCode, twoFactorAuth;
		public SteamBot(string[] args, DiscordBot bot)
		{
			DiscordClient = bot;
			user = "lobby_creator3";//args[0]; //This is a fodder steam account so I snooze
			pass = "WHkAeZqzfFHE6Qd";//args[1];
			steamClient = new SteamClient();

			steamUser = steamClient.GetHandler<SteamUser>();

			isRunning = true;

			Console.WriteLine("Connecting to Steam...");

			steamClient.Connect();

			manager = new CallbackManager(steamClient);

			manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
			manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
			manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
			manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
			manager.Subscribe<SteamApps.PICSChangesCallback>(AppChanges);
		}

		async void MainChangeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				await Apps.PICSGetChangesSince(LastChangeNumber, true, false);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error:" + ex.Message);
			}
		}

		/// <summary>
		/// Debug logging, comment this out and I know where all of my temp logging is and can remove them since 99.9999% of the time they're only temp.
		/// </summary>
		public void LogShit(string lol) => Console.WriteLine(lol);


		/// <summary>
		/// Main logic to loop through all of steam's changes.
		/// </summary>
		/// <param name="callback"></param>
		async void AppChanges(SteamApps.PICSChangesCallback callback)
		{
			if (LastChangeNumber == callback.CurrentChangeNumber) return;
			LastChangeNumber = callback.CurrentChangeNumber;

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
					FullProductInfo = GetFullProductInfo(AppsThatUpdated.Key).Result;
				}
				catch
				{
					Console.WriteLine("ERROR: Steam is down or you need to check your connection. Code 0");
					return;
				}

				FullProductInfo = GetFullProductInfo(AppsThatUpdated.Key).Result;

				AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet ProductInfo = FullProductInfo.ProductInfo;

				if (ProductInfo.Complete)
				{
					foreach (SteamApps.PICSProductInfoCallback CallBackInfo in ProductInfo.Results)
					{
						foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> CallBackInfoApps in CallBackInfo.Apps)
						{
							string DepoChanged = null;
							KeyValue depotKV = CallBackInfoApps.Value.KeyValues.Children.Where(c => c.Name == "depots").FirstOrDefault();
							if (depotKV != null && FullProductInfo.IsPublic)
							{
								KeyValue depotInfo = depotKV["branches"];
								if (depotInfo == null) continue;
								foreach (KeyValue test in depotInfo.Children)
								{
									foreach (KeyValue test2 in test.Children)
									{
										if (test2.Name == "timeupdated")
										{
											TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
											if (((int)t.TotalSeconds - int.Parse(test2.Value)) < 10) // Needed because it can take a couple of seconds to go through the steam pipeline.
											{
												DepoChanged = test.Name;
												AppUpdate.Content = true;
											}
										}
									}
								}
							}
							AppUpdate.LastUpdated = DateTime.UtcNow;
							AppUpdate.Name = CallBackInfoApps.Value.KeyValues["common"]["name"].AsString();
							
							AppUpdate.DepoName = DepoChanged;
							Console.WriteLine(AppUpdate.Content ? "Content Update for " + AppUpdate.AppID : "Update for " + AppUpdate.AppID);

							using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
							{
								context.AppInfoData.RemoveRange(context.AppInfoData.ToList().Where(x => x.AppID == AppUpdate.AppID));

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
				}
				UpdatesProcessed++;
				DiscordClient.AppUpdated(AppUpdate);
			}
		}

		/// <summary>
		/// Gets app's name.
		/// </summary>
		/// <param name="appid"></param>
		/// <returns></returns>
		public async Task<string> GetAppName(uint appid)
		{
			AppInfo test = DiscordBot.GetCachedInfo(appid, true);
			if (test != null)
			{
				return test.Name;
			}

			AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet ProductInfo = await Apps.PICSGetProductInfo(appid, null);

			AppInfo appInfo = new AppInfo()
			{
				AppID = appid
			};

			if (ProductInfo.Complete)
			{
				foreach (SteamApps.PICSProductInfoCallback CallBackInfo in ProductInfo.Results)
				{
					foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> CallBackInfoApps in CallBackInfo.Apps)
					{
						appInfo.Name = CallBackInfoApps.Value.KeyValues["common"]["name"].AsString();

						using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
						{
							context.AppInfoData.Add(appInfo);
							context.SaveChanges();
						}

						return CallBackInfoApps.Value.KeyValues["common"]["name"].AsString();
					}
				}
			}

			return "Unknown App";
		}

		public async Task<bool> IsSteamDown()
		{

			AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet ProductInfo = await Apps.PICSGetProductInfo(570, null);
			if (ProductInfo.Failed)
			{
				return false;
			}

			return ProductInfo.Complete;
		}

		public static async Task<ulong> GetAccessToken(uint appid)
		{
			SteamApps.PICSTokensCallback AppTokenInfo;
			try
			{
				AppTokenInfo = await SteamUpdateBot.SteamClient.Apps.PICSGetAccessTokens(appid, null);

				if (AppTokenInfo.AppTokensDenied.Contains(appid)) return 0;

				return AppTokenInfo.AppTokens.Where(c => c.Key == appid).FirstOrDefault().Value;
			}
			catch
			{
				Console.WriteLine("ERROR: Steam is down or you need to check your connection. Code 0");
				return 0;
			}
		}

		public static async Task<CustomProductInfo> GetFullProductInfo(uint appid)
		{
			CustomProductInfo customProductInfo = new CustomProductInfo();

			SteamApps.PICSRequest request = new SteamApps.PICSRequest(appid);

			ulong AccessToken = GetAccessToken(appid).Result;

			if (AccessToken == 0)
			{
				try
				{
					customProductInfo.ProductInfo = await SteamUpdateBot.SteamClient.Apps.PICSGetProductInfo(appid, null, false, false);
					customProductInfo.IsPublic = true;
				}
				catch
				{
					Console.WriteLine("ERROR: Steam is down or you need to check your connection. Code 1");
					return null;
				}
			}
			else
			{
				request.AccessToken = AccessToken;
				request.Public = false;
				customProductInfo.IsPublic = false;
				try
				{
					customProductInfo.ProductInfo = await SteamUpdateBot.SteamClient.Apps.PICSGetProductInfo(new List<SteamApps.PICSRequest>() { request }, new List<SteamApps.PICSRequest>() { });
				}
				catch
				{
					Console.WriteLine("ERROR: Steam is down or you need to check your connection. Code 2");
					return null;
				}
			}
			return customProductInfo;
		}

		#region CodeThat100PercentIsntFromOtherProjects👀
		void OnConnected(SteamClient.ConnectedCallback callback)
		{
			Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);

			byte[] sentryHash = null;
			if (File.Exists("sentry.bin"))
			{
				byte[] sentryFile = File.ReadAllBytes("sentry.bin");
				sentryHash = CryptoHelper.SHAHash(sentryFile);
			}

			steamUser.LogOn(new SteamUser.LogOnDetails
			{
				Username = user,
				Password = pass,
				AuthCode = authCode,
				TwoFactorCode = twoFactorAuth,
				SentryFileHash = sentryHash,
			});

		}

		void OnDisconnected(SteamClient.DisconnectedCallback callback)
		{
			// after recieving an AccountLogonDenied, we'll be disconnected from steam
			// so after we read an authcode from the user, we need to reconnect to begin the logon flow again

			Console.WriteLine("Disconnected from Steam, reconnecting in 5...");

			Thread.Sleep(TimeSpan.FromSeconds(5));

			steamClient.Connect();
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
					twoFactorAuth = Console.ReadLine();
				}
				else
				{
					Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
					authCode = Console.ReadLine();
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
			Apps = steamClient.GetHandler<SteamApps>();

			MainChangeTimer.Elapsed += (sender, args) => MainChangeTimer_Elapsed(sender, args);
			MainChangeTimer.AutoReset = true;
			MainChangeTimer.Interval = 500;
			MainChangeTimer.Start();
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
			steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
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
