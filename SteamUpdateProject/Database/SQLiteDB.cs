using System;
using System.Collections.Generic;
using System.Text;
//using Microsoft.EntityFrameworkCore;
using System.Data.Entity;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Linq;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace SteamUpdateProject
{
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
