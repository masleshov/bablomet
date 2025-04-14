using System.Threading;
using Bablomet.AI.ML.Kafka;
using Bablomet.Common.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5002);
});

// Add services to the container.
builder.Services.AddTransient<UnitOfWork>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await InstrumentsCache.Init(app.Services.GetRequiredService<UnitOfWork>());

var cancellationTokenSource = new CancellationTokenSource();
var kafkaConsumer = new KafkaConsumer();
await kafkaConsumer.Start(cancellationTokenSource.Token);

await app.RunAsync(cancellationTokenSource.Token);