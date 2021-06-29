using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

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
	}
}
