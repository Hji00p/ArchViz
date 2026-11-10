using ArchViz.Application.Auth;
using ArchViz.Application.Communication;
using ArchViz.Application.Estimation;
using ArchViz.Application.Orders;
using ArchViz.Application.Scenes;
using ArchViz.Application.Users;
using ArchViz.Infrastructure.Identity;
using ArchViz.Infrastructure.MachineLearning;
using ArchViz.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<ArchVizDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ArchViz")
        ?? throw new InvalidOperationException("Failed connection string")));

builder.Services.AddDataProtection();
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<ArchVizDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<SubmitOrderHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing. Configure Jwt:Key in User Secrets.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    });


builder.Services.AddAuthorization();



builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();

builder.Services.AddScoped<GetOrderQueueHandler>();
builder.Services.AddScoped<ClaimOrderHandler>();

builder.Services.AddScoped<GetManagedOrdersHandler>();

builder.Services.AddScoped<GetManagedOrderByIdHandler>();

builder.Services.AddScoped<ISceneRepository, SceneRepository>();

builder.Services.AddScoped<CreateSceneHandler>();

builder.Services.AddScoped<GetOrderScenesHandler>();

builder.Services.AddScoped<IUserDirectory, UserDirectory>();

builder.Services.AddScoped<AssignVisualizerHandler>();

builder.Services.AddScoped<GetMyScenesHandler>();

builder.Services.AddScoped<AddSceneRequirementsHandler>();

builder.Services.AddScoped<AddSceneRequirementsHandler>();

builder.Services.AddScoped<GetMySceneRequirementsHandler>();

builder.Services.AddScoped<SubmitSceneAssessmentHandler>();

builder.Services.AddScoped<GetSceneAssessmentHandler>();

builder.Services.AddSingleton<ISceneHoursPredictor, MlSceneHoursPredictor>();

builder.Services.AddScoped<IEstimationRepository, EstimationRepository>();

builder.Services.AddScoped<SaveScenePredictionHandler>();

builder.Services.AddScoped<ApproveScenePlanHandler>();

builder.Services.AddScoped<StartSceneWorkHandler>();

builder.Services.AddScoped<ISceneTimeRepository, SceneTimeRepository>();

builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();

builder.Services.AddScoped<IOrderWorkflowRepository, OrderWorkflowRepository>();

builder.Services.AddScoped<ICommunicationRepository, CommunicationRepository>();

var app = builder.Build();





if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedAsync(roleManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
