using Microsoft.EntityFrameworkCore;
using ModBusDevExpress.Service;
using System;

namespace ModBusDevExpress.Models
{
    public class ModBusDbContext : DbContext
    {
        public DbSet<AcquiredData> ModBusData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var settings = SessionService.GetCurrentSettings();
            
            // SQL Server만 지원
            string connectionString = $"Server={settings.Server},{settings.Port};Database={settings.Database};User Id={settings.Username};Password={settings.Password};TrustServerCertificate=true;";
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 테이블명 설정
            modelBuilder.Entity<AcquiredData>().ToTable("ModBusData");
            
            // 기본키 설정
            modelBuilder.Entity<AcquiredData>().HasKey(x => x.ID);
            
            // 필드 매핑 설정 (실제 존재하는 필드만)
            modelBuilder.Entity<AcquiredData>(entity =>
            {
                entity.Property(e => e.ID).ValueGeneratedOnAdd();
                entity.Property(e => e.CheckCompanyObjectID).IsRequired(false); // GUID 타입
                entity.Property(e => e.CompanyObjectID).IsRequired(false); // GUID 타입
                entity.Property(e => e.FacilityCode).HasMaxLength(255);
                entity.Property(e => e.CreateUserId).HasMaxLength(255); // 회사명 저장용
                entity.Property(e => e.NumericData).HasColumnType("float"); // SQL Server의 float는 C#의 double에 매핑됨
                entity.Property(e => e.StringData).HasMaxLength(255);
                entity.Property(e => e.IPAddress).HasMaxLength(100);
            });
        }
    }
}