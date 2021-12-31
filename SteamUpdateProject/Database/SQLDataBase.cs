using SteamUpdateProject.Entities;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace SteamUpdateProject
{
    /// <summary>
    /// The main <see cref="DbContext"/> class that we get/set any information related to <see cref="AppInfo"/> or <see cref="GuildInfo"/>.
    /// </summary>
    public class SQLDataBase : DbContext
    {
        public DbSet<AppInfo> AppInfoData { get; set; }
        public DbSet<GuildInfo> GuildInformation { get; set; }
        public SQLDataBase(string connection) : base(connection)
        {
            Configuration.AutoDetectChangesEnabled = false;
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
