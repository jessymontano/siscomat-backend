using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Siscomat.Core.Entities;

namespace Siscomat.Repositories
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Gestor> Gestores { get; set; }
        public DbSet<Participante> Participantes { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Plantilla> Plantillas { get; set; }
        public DbSet<Constancia> Constancias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // propiedades de los campos de la bd
            modelBuilder.Entity<Participante>(entity =>
            {
                entity.HasKey(p => p.Folio);
                entity.Property(p => p.Nombre)
                    .IsRequired()
                    .HasMaxLength(150);
                entity.Property(p => p.Apellido1)
                    .IsRequired()
                    .HasMaxLength(150);
                entity.Property(p => p.Apellido2)
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<Gestor>(entity =>
            {
                entity.Property(g => g.Nombre)
                    .IsRequired()
                    .HasMaxLength(150);
                entity.Property(g => g.Apellido1)
                    .IsRequired()
                    .HasMaxLength(150);
                entity.Property(g => g.Apellido2)
                    .HasMaxLength(150);
                entity.Property(g => g.Correo)
                    .IsRequired()
                    .HasMaxLength(150);
                entity.HasIndex(g => g.Correo)
                    .IsUnique();
                entity.Property(g => g.PasswordHash)
                    .IsRequired();
            });

            modelBuilder.Entity<Plantilla>(entity =>
            {
                entity.Property(p => p.Nombre)
                .IsRequired()
                .HasMaxLength(150);
                entity.Property(p => p.Path)
                .IsRequired();
            });

            modelBuilder.Entity<Curso>()
                .Property(c => c.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<Constancia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
                entity.HasOne(c => c.Curso)
                    .WithMany(curso => curso.Constancias)
                    .HasForeignKey(c => c.CursoId);
                entity.HasOne(c => c.Plantilla)
                    .WithMany(plantilla => plantilla.Constancias)
                    .HasForeignKey(p => p.PlantillaId);
                entity.HasOne(c => c.Participante)
                    .WithMany(participante => participante.Constancias)
                    .HasForeignKey(p => p.FolioParticipante);
            });
        }
    }
}
