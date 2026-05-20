using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TaskManagement.Web.Data;
using TaskManagement.Web.Middleware;
using TaskManagement.Web.Models;
using TaskManagement.Web.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories cũ
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Repositories mới
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ITaskAssigneeRepository, TaskAssigneeRepository>();
builder.Services.AddScoped<ITaskOutputRepository, TaskOutputRepository>();
builder.Services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = "API quản lý công việc có bảo vệ bằng API Key"
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Nhập API Key"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
    });
});

var app = builder.Build();

// ── SEED DATA ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Chỉ seed nếu chưa có user nào
    if (!context.Users.Any())
    {
        var adminUser = new User
        {
            FullName = "Quản trị viên",
            Email = "admin@taskflow.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            CreatedAt = DateTime.Now
        };
        var leaderUser = new User
        {
            FullName = "Lê Anh Quân",
            Email = "quan@taskflow.com",
            Password = BCrypt.Net.BCrypt.HashPassword("User@123"),
            Role = "User",
            CreatedAt = DateTime.Now
        };
        var memberUser1 = new User
        {
            FullName = "Mai Thành Đạt",
            Email = "dat@taskflow.com",
            Password = BCrypt.Net.BCrypt.HashPassword("User@123"),
            Role = "User",
            CreatedAt = DateTime.Now
        };
        var memberUser2 = new User
        {
            FullName = "Võ Thanh Sơn",
            Email = "son@taskflow.com",
            Password = BCrypt.Net.BCrypt.HashPassword("User@123"),
            Role = "User",
            CreatedAt = DateTime.Now
        };

        context.Users.AddRange(adminUser, leaderUser, memberUser1, memberUser2);
        context.SaveChanges();

        // Tạo nhóm demo
        var group = new Group
        {
            Name = "Nhóm Demo TaskFlow",
            Description = "Nhóm mẫu để test hệ thống",
            CreatedBy = leaderUser.UserId,
            CreatedAt = DateTime.Now
        };
        context.Groups.Add(group);
        context.SaveChanges();

        // Phân vai trong nhóm
        context.GroupMembers.AddRange(
            new GroupMember { GroupId = group.GroupId, UserId = leaderUser.UserId, Role = "Leader", JoinedAt = DateTime.Now },
            new GroupMember { GroupId = group.GroupId, UserId = memberUser1.UserId, Role = "Member", JoinedAt = DateTime.Now },
            new GroupMember { GroupId = group.GroupId, UserId = memberUser2.UserId, Role = "Member", JoinedAt = DateTime.Now }
        );
        context.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();