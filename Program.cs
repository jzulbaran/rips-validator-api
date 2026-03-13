using Microsoft.EntityFrameworkCore;
using RipsValidatorApi.Data;
using RipsValidatorApi.Services;
using RipsValidatorApi.Validators;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// EF Core + SQL Server
builder.Services.AddDbContext<ValidadorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Validators (transient porque no tienen estado persistente)
builder.Services.AddTransient<EstructuraValidator>();
builder.Services.AddTransient<ContenidoValidator>();
builder.Services.AddTransient<RelacionValidator>();

// Service
builder.Services.AddScoped<IValidadorRipsService, ValidadorRipsService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RIPS Validator API",
        Version = "v1",
        Description = "API para validación de RIPS según Resolución 2275/2023 (SISPRO)"
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// CORS (para llamadas desde SaludSystem en Windows)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RIPS Validator API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
