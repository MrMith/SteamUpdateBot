using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Collections.Generic;

namespace SteamUpdateProject
{
	/// <summary>
	/// The main <see cref="DbContext"/> class, I have very little knowledge of how/why this works. Help.
	/// </summary>
	public class SQLDataBase : DbContext
	{
		public DbSet<AppInfo> AppInfoData { get; set; }
		public DbSet<GuildInfo> GuildInformation { get; set; }
		public SQLDataBase(string connection) : base(connection)
		{
			this.Configuration.AutoDetectChangesEnabled = false;
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();

			modelBuilder.Entity<GuildInfo>()
			.HasMany(p => p.SubscribedApps)
			.WithOptional()
			.WillCascadeOnDelete(true);
		}

		/// <summary>
		/// Returning this as a list seems to help with getting less errors so I think its worth to keep the extra headache.
		/// </summary>
		public List<AppInfo> AllApps
		{
			get
			{
				return new List<AppInfo>(this.AppInfoData);
			}
		}

		/// <summary>
		/// Returning this as a list seems to help with getting less errors so I think its worth to keep the extra headache.
		/// </summary>
		public List<GuildInfo> AllGuilds
		{
			get
			{
				return new List<GuildInfo>(this.GuildInformation);
			}
		}
	}
}
