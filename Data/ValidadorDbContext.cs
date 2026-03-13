using Microsoft.EntityFrameworkCore;
using RipsValidatorApi.Data.Entities;

namespace RipsValidatorApi.Data;

public class ValidadorDbContext : DbContext
{
    public ValidadorDbContext(DbContextOptions<ValidadorDbContext> options) : base(options) { }

    public DbSet<SisproCie10> CIE10 { get; set; }
    public DbSet<SisproTipoUsuario> TipoUsuario { get; set; }
    public DbSet<SisproCoberturaPlan> CoberturaPlan { get; set; }
    public DbSet<SisproModalidadPago> ModalidadPago { get; set; }
    public DbSet<SisproConceptoRecaudo> ConceptoRecaudo { get; set; }
    public DbSet<SisproViaIngreso> ViaIngreso { get; set; }
    public DbSet<SisproFinalidadConsulta> FinalidadConsulta { get; set; }
    public DbSet<SisproCoberturaModalidadCruzada> CoberturaModalidadCruzada { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SisproCoberturaModalidadCruzada>()
            .HasIndex(x => new { x.CodigoCoberturaplan, x.CodigoModalidadPago, x.CodigoTipoUsuario, x.CodigoConceptoRecaudo })
            .HasDatabaseName("IX_CoberturaModalidadCruzada_Combinacion");
    }
}
