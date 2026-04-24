using Microsoft.EntityFrameworkCore;
using PolizasBimbo.Infrastructure.Persistence.Records;

namespace PolizasBimbo.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<PolicyRecord> Policies => Set<PolicyRecord>();
    public DbSet<DownloadAuditRecord> Audits => Set<DownloadAuditRecord>();
    public DbSet<DownloadTokenRecord> DownloadTokens => Set<DownloadTokenRecord>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var p = b.Entity<PolicyRecord>();
        p.ToTable("PolizasBimboTraspaso");
        p.HasKey(x => x.Id);
        p.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        p.Property(x => x.NumColaborador).HasColumnName("NumColaborador").IsRequired();
        p.Property(x => x.FileName).HasColumnName("NomArchivo").HasMaxLength(100).IsRequired();
        p.Property(x => x.FullName).HasColumnName("NombreCompleto").HasMaxLength(200);
        p.Property(x => x.Email).HasColumnName("Email").HasMaxLength(100);
        p.Property(x => x.Phone).HasColumnName("Telefono").HasMaxLength(50);
        p.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");

        var a = b.Entity<DownloadAuditRecord>();
        a.ToTable("ConsultaPolizasBimboTraspaso");
        a.HasKey(x => x.Id);
        a.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        a.Property(x => x.PolicyId).HasColumnName("PolizaId");
        a.Property(x => x.NumColaborador).HasColumnName("NumColaborador").IsRequired();
        a.Property(x => x.Email).HasColumnName("Email").HasMaxLength(100).IsRequired();
        a.Property(x => x.Phone).HasColumnName("Telefono").HasMaxLength(50).IsRequired();
        a.Property(x => x.FileName).HasColumnName("NomArchivo").HasMaxLength(100).IsRequired();
        a.Property(x => x.Country).HasColumnName("PaisOrigen").HasMaxLength(100).IsRequired();
        a.Property(x => x.City).HasColumnName("CiudadOrigen").HasMaxLength(100).IsRequired();
        a.Property(x => x.CreatedAt).HasColumnName("FechaCreacion").HasColumnType("datetime");

        var t = b.Entity<DownloadTokenRecord>();
        t.ToTable("DownloadTokens");
        t.HasKey(x => x.Jti);
        t.Property(x => x.Jti).HasColumnName("jti").HasColumnType("char(36)");
        t.Property(x => x.PolicyId).HasColumnName("PolizaId");
        t.Property(x => x.IssuedAt).HasColumnName("IssuedAt").HasColumnType("datetime2(0)");
        t.Property(x => x.ConsumedAt).HasColumnName("ConsumedAt").HasColumnType("datetime2(0)");
    }
}
