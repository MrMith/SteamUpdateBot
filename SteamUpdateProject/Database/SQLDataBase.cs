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

        /// <summary>
        /// Returning this as a list seems to help with getting less errors so I think its worth to keep the extra headache.
        /// </summary>
        public List<AppInfo> AllApps => new List<AppInfo>(AppInfoData);

        /// <summary>
        /// Returning this as a list seems to help with getting less errors so I think its worth to keep the extra headache.
        /// </summary>
        public List<GuildInfo> AllGuilds => new List<GuildInfo>(GuildInformation);
    }
}
