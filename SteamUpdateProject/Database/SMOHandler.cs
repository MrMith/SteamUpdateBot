using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles everything related to SMO aka SQL Server Management Objects.
	/// See this for more reading -> https://docs.microsoft.com/en-us/sql/relational-databases/server-management-objects-smo/overview-smo?view=sql-server-ver15
	/// </summary>
	class SMOHandler
	{
		public Database SMODatabase;
		public Server SMOServer;
		private int _BackupIncrement = 0;

		public SMOHandler()
		{
			SMOServer = new Server();

			foreach (var test in SMOServer.Databases)
			{
				if (test.ToString().Contains("STEAMINFORMATION", StringComparison.OrdinalIgnoreCase))
				{
					SMODatabase = test as Database;
				}
			}

			Console.WriteLine(SMODatabase.Name);
		}

		public void BackupDatabase()
		{
			// Define a Backup object variable.   
			Backup bk = new Backup();

			// Specify the type of backup, the description, the name, and the database to be backed up.   
			bk.Action = BackupActionType.Database;
			bk.BackupSetDescription = "Full automated backup of SteamInformation";
			bk.BackupSetName = "SteamInformation Backup";
			bk.Database = SMODatabase.Name;

			// Declare a BackupDeviceItem by supplying the backup device file name in the constructor, and the type of device is a file.   
			BackupDeviceItem bdi = default(BackupDeviceItem);
			bdi = new BackupDeviceItem("Steam_Full_Backup_" + _BackupIncrement, DeviceType.File);
			_BackupIncrement++;

			// Add the device to the Backup object.   
			bk.Devices.Add(bdi);
			// Set the Incremental property to False to specify that this is a full database backup.   
			bk.Incremental = true;

			// Set the expiration date.   
			System.DateTime backupdate = DateTime.Now;
			backupdate.AddDays(2);
			bk.ExpirationDate = backupdate;

			// Specify that the log must be truncated after the backup is complete.   
			bk.LogTruncation = BackupTruncateLogType.Truncate;

			// Run SqlBackup to perform the full database backup on the instance of SQL Server.   
			bk.SqlBackup(SMOServer);

			// Inform the user that the backup has been completed.   
			System.Console.WriteLine("Full Backup complete.");
		}

		public void Restore()
		{
			// Store the current recovery model in a variable.   
			int recoverymod;
			recoverymod = (int)SMODatabase.DatabaseOptions.RecoveryModel;

			// Delete the AdventureWorks2012 database before restoring it  
			// db.Drop();  

			// Define a Restore object variable.  
			Restore rs = new Restore();

			// Set the NoRecovery property to true, so the transactions are not recovered.   
			rs.NoRecovery = true;

			BackupDeviceItem bdi = default(BackupDeviceItem);
			bdi = new BackupDeviceItem("Steam_Full_Backup_" + _BackupIncrement, DeviceType.File);

			// Add the device that contains the full database backup to the Restore object.   
			rs.Devices.Add(bdi);

			// Specify the database name.   
			rs.Database = SMODatabase.Name;

			// Restore the full database backup with no recovery.   
			rs.SqlRestore(SMOServer);

			// Inform the user that the Full Database Restore is complete.   
			Console.WriteLine("Full Database Restore complete.");

			// Remove the device from the Restore object.  
			rs.Devices.Remove(bdi);

			// Set the NoRecovery property to False.   
			rs.NoRecovery = false;

			// Restore the differential database backup with recovery.   
			rs.SqlRestore(SMOServer);

			// Inform the user that the differential database restore is complete.   
			System.Console.WriteLine("Differential Database Restore complete.");

			// Set the database recovery mode back to its original value.  
			SMODatabase.RecoveryModel = (RecoveryModel)recoverymod;
		}
	}
}
