using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args); //Inicialização da api

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); //Servico do swagger
builder.Services.AddSwaggerGen();//Servico do swagger

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TarefasDB")); //Registro do contexto EFCORE como servico em memoria, ou seja, os dados persistem apenas enquanto a aplicacao esta em execucao

var app = builder.Build(); //Build da aplicacao

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) //Condicao para incluir o swagger apenas em ambiente de desenvolvimento
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//Minimal apis usando os metodos .MapGet, .MapPost, .MapPut, .MapDelete da classe WebApplication, assim podemos criar endpoints em seus parametros 

app.MapGet("/", () => "Ola Mundo"); //criando um enpoint de exemplo


app.MapGet("frases", async () => await new HttpClient().GetStringAsync("https://ron-swanson-quotes.herokuapp.com/v2/quotes")); //criando um endpoint que acessa uma api externa e retorna seus dados

app.MapGet("/tarefas", async (AppDbContext db) => await db.Tarefas.ToListAsync()); //Listando tarefas

app.MapGet("/tarefas/{id}", async(int id, AppDbContext db) => await db.Tarefas.FindAsync(id) is Tarefa tarefa ? Results.Ok(tarefa) : Results.NotFound()); //Retornando uma tarefa por id 

app.MapGet("/tarefas/concluida", async (AppDbContext db) => await db.Tarefas.Where(t => t.IsConcluida).ToListAsync()); //Retornando tarefas concluidas

app.MapPut("/tarefas/{id}", async (int id, Tarefa inputTarefa, AppDbContext db) => {
    var tarefa = await db.Tarefas.FindAsync(id); //procurando a tarefa, utilizando o find por conta que a contexto esta em memoria

    if(tarefa is null) return Results.NotFound();

    tarefa.Nome = inputTarefa.Nome; //edicao do dado em memoria com o dado recebido por parametro
    tarefa.IsConcluida = inputTarefa.IsConcluida; //edicao do dado em memoria com o dado recebido por parametro

    await db.SaveChangesAsync(); //Persistindo as informacoes na memoria
    return Results.NoContent();
});

app.MapDelete("/tarefas/{id}", async (int id, AppDbContext db) =>
{
    if (await db.Tarefas.FindAsync(id) is Tarefa tarefa) //Procurando se a tarefa do id recebido contem as propriedades da classe tarefa
    {
        db.Tarefas.Remove(tarefa);
        await db.SaveChangesAsync();
        return Results.Ok(tarefa);
    }
    return Results.NotFound();
});

app.MapPost("/tarefas", async (Tarefa tarefa, AppDbContext db) =>
{
    db.Tarefas.Add(tarefa);
    await db.SaveChangesAsync();
    return Results.Created($"/tarefas/{tarefa.Id}", tarefa);
});

app.Run();

class Tarefa
{
    public int Id { get; set; }
    public string? Nome { get; set; }

    public bool IsConcluida { get; set; }
}

class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Tarefa> Tarefas => Set<Tarefa>(); //recurso inspetion body, pois se definirmos como "get, set" iriamos receber um alerta do compilador que a propriedade e non-nullable, pois estamos usando o DbSet...
}