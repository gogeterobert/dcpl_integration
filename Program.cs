using DCPLInterpreterV2.Infrastructure;
using DCPLInterpreterV2.Interfaces;
using DCPLInterpreterV2.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    // .AddNewtonsoftJson(options =>
    // {
    //     // options.SerializerSettings.Converters.Add(new EventConverter());
    //     options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    // })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FrameJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new EventJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new TransformationalFrameJsonConverter());
    });
    // Register FrameJsonConverter globally
    // builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SchemaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<ISchemaService, SchemaService>();
builder.Services.AddTransient<IEntityService, EntityService>();
builder.Services.AddTransient<IActionService, ActionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
