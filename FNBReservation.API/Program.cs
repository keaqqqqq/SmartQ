var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Comment out HTTPS redirection for development
//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

//in production, need to handle both HTTP and HTTPS
//can consider using reverse proxy as well in production