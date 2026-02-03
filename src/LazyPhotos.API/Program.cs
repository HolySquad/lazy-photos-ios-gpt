using System.Text;
using LazyPhotos.Core.Interfaces;
using LazyPhotos.Infrastructure.Data;
using LazyPhotos.Infrastructure.Repositories;
using LazyPhotos.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, config) =>
{
	config.ReadFrom.Configuration(context.Configuration)
		  .Enrich.FromLogContext()
		  .WriteTo.Console()
		  .WriteTo.File(
			  path: "logs/lazyphotos-.txt",
			  rollingInterval: RollingInterval.Day,
			  retainedFileCountLimit: 7);
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<LazyPhotosDbContext>(options =>
{
	if (builder.Environment.IsDevelopment())
	{
		options.UseSqlite(connectionString);
	}
	else
	{
		options.UseNpgsql(connectionString);
	}
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
	?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "LazyPhotosAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "LazyPhotosClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtAudience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
			ClockSkew = TimeSpan.Zero
		};
	});

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAlbumRepository, AlbumRepository>();
builder.Services.AddScoped<IUploadSessionRepository, UploadSessionRepository>();

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();

// Controllers
builder.Services.AddControllers();

// API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for mobile app
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
	status = "healthy",
	timestamp = DateTime.UtcNow,
	environment = app.Environment.EnvironmentName
}))
.WithName("HealthCheck")
.WithTags("Health");

// Run database migrations on startup in development
if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var dbContext = scope.ServiceProvider.GetRequiredService<LazyPhotosDbContext>();
	dbContext.Database.EnsureCreated();
	Log.Information("Database initialized");
}

Log.Information("Lazy Photos API starting up");

app.Lifetime.ApplicationStarted.Register(() =>
{
	var server = app.Services.GetRequiredService<IServer>();
	var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;

	var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Bindings");
	if (addresses is null || addresses.Count == 0)
	{
		logger.LogInformation("No IServerAddressesFeature addresses (might be behind IIS/IIS Express).");
		return;
	}

	foreach (var a in addresses)
		logger.LogInformation("Listening on: {Address}", a);
});


app.Run();
