using Microsoft.EntityFrameworkCore;
using Proposta.Api.Data;
using Proposta.Api.Messaging;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();






// banco SQLite
builder.Services.AddDbContext<PropostaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PropostaDb")));

// RabbitMQ: uma conexão para a aplicação inteira
ConnectionFactory fabrica = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost"
};
IConnection conexaoRabbit = await fabrica.CreateConnectionAsync();

builder.Services.AddSingleton(conexaoRabbit);
builder.Services.AddSingleton<RabbitMqPublisher>();

// consumidor que roda em segundo plano escutando a fila
builder.Services.AddHostedService<ClienteCadastradoConsumer>();







var app = builder.Build();



// cria o banco e a tabela na primeira execução
using (var scope = app.Services.CreateScope())
{
    PropostaDbContext db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
    db.Database.EnsureCreated();
}




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
