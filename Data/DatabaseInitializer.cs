using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PhotoArchive.Data
{
    /// <summary>
    /// Applies EF Core migrations at startup.
    /// Databases created by v1.0 (via EnsureCreated) have the full schema but no migration history,
    /// so the first (baseline) migration is marked as applied for them instead of being run.
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task MigrateAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

            // the migrations are SQL Server specific; other providers (SQLite in the tests) get the schema from the model
            if (!db.Database.IsSqlServer())
            {
                await db.Database.EnsureCreatedAsync();
                return;
            }

            if (await db.Database.CanConnectAsync())
            {
                var baseline = db.Database.GetMigrations().First();
                var rows = await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[Films]') IS NOT NULL AND OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1});
END", baseline, ProductInfo.GetVersion());

                if (rows > 0)
                    logger.LogInformation("Existing v1.0 database detected; marked baseline migration {Migration} as applied.", baseline);
            }

            try
            {
                await db.Database.MigrateAsync();
            }
            catch (SqlException ex) when (ex.Number == 5170 || ex.Number == 1802)
            {
                // 5170: "Cannot create file ... because it already exists" - the .mdf/.ldf are on disk
                // but this SQL Server instance doesn't know the database (e.g. restored from another instance).
                throw new InvalidOperationException(
                    "The database files already exist but the database is not attached to this SQL Server instance. " +
                    "Attach them instead of creating a new database, e.g.:\n" +
                    "  sqlcmd -S \"(localdb)\\MSSQLLocalDB\" -E -Q \"CREATE DATABASE PhotoArchiveDB ON " +
                    "(FILENAME='<path>\\PhotoArchiveDB.mdf'), (FILENAME='<path>\\PhotoArchiveDB_log.ldf') FOR ATTACH\"\n" +
                    "Details: " + ex.Message, ex);
            }
        }
    }
}
