
using DeliveryOrderAPI;
using DeliveryOrderAPI.Contexts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(c =>
{
    c.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(15);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Add services to the container.S
builder.Services.AddControllers();
builder.Services.AddDbContext<DBSCM>();
builder.Services.AddDbContext<DBHRM>();
builder.Services.AddCors(options => options.AddPolicy("Cors", builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
}));
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
//opt =>
//{
//    opt.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
//        .GetBytes("scm.daikin.co.jp")),
//        ValidateIssuer = false,
//        ValidateAudience = false
//    };
//}
//);
var app = builder.Build();
app.UseCors("Cors");
app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();
