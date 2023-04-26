using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1
{
    public class APPContext : DbContext
    {
        public DbSet<DownloadStatus> DownloadStatuses { get; set; }

        public APPContext(DbContextOptions<APPContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DownloadStatus>()
                .HasKey(ds => ds.Id);
        }
    }
}
