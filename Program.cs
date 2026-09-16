using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Repositories;
using EmployeeAttendanceWithHealthDeclaration.ExternalApis.Ldap.Services;
using EmployeeAttendanceWithHealthDeclaration.Hubs;
using EmployeeAttendanceWithHealthDeclaration.Models.Domain;
using EmployeeAttendanceWithHealthDeclaration.Repositories;
using EmployeeAttendanceWithHealthDeclaration.Services;
using Dapper;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Use snake_case for all JSON property names
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30-minute session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS protection
});

// Configure Database Connection (Dapper)
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new MySqlConnection(connectionString);
});

// Register Repositories
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITimeLogsRepository, TimeLogsRepository>();
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Register Services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IHistoryLogsService, HistoryLogsService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<ITimeLogsManagementService, TimeLogsManagementService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Register LDAP Services
builder.Services.AddScoped<ILdapRepository, LdapRepository>();
builder.Services.AddScoped<ILdapService, LdapService>();

// Add HttpClient for LDAP
builder.Services.AddHttpClient();

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Vanity URL for the QA requirements document (public, not linked in the UI)
app.UseRewriter(new RewriteOptions()
    .AddRewrite(@"(?i)^system-requirement$", "docs/system-requirements-qa.html", skipRemainingRules: true));

app.UseStaticFiles();

app.UseRouting();

// Use Session before UseAuthorization
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<AttendanceHub>("/attendanceHub");

app.Run();
