

using DeliveryOrderAPI.Contexts;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(c =>
{
    c.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(15);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
var app = builder.Build();
app.UseCors("Cors");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseAuthentication();
app.UseAuthorization();
//app.UseResponseCompression();
app.MapControllers();

app.Run();
