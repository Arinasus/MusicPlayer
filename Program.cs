using MusicStore.Models;
using MusicStore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISongRepository, InMemorySongRepository>();
builder.Services.AddScoped<IImageService, ImageService>();

builder.Services.AddCors(options => { 
    options.AddPolicy("AllowFrontend", policy => policy 
    .WithOrigins("https://music-store-orcin.vercel.app") 
 .AllowAnyHeader() 
 .AllowAnyMethod()); });
var app = builder.Build();

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
