using System;
using Microsoft.EntityFrameworkCore;
using RentAllPro.Models;
using System.IO;

namespace RentAllPro.Data
{
    public class RentAllProContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Rental> Rentals { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Az adatbázis fájl helye
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RentAllPro",
                "rentallpro.db"
            );

            // Mappa létrehozása, ha nem létezik
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customer tábla beállításai
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IdNumber).IsRequired().HasMaxLength(50);
            });

            // Rental tábla beállításai
            modelBuilder.Entity<Rental>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");

                // Kapcsolat a Customer táblával
                entity.HasOne(e => e.Customer)
                      .WithMany()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}