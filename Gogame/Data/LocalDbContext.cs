using Gogame.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Data
{
    internal class LocalDbContext : DbContext
    {
        public DbSet<GameRecordEntity> Games => Set<GameRecordEntity>();
        public DbSet<LocalUserStatsEntity> UserStats => Set<LocalUserStatsEntity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Save file into AppData
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Combine(path, "gogame.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            Debug.WriteLine($"DB path: {dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<GameRecordEntity>().HasIndex(g => g.ClientId).IsUnique();
            b.Entity<GameRecordEntity>().HasIndex(g => g.IsSynced);
        }
    }
}
