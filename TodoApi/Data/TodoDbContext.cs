using Microsoft.EntityFrameworkCore;

// Puts everything in this file under he `TodoApi.Data` namespace
namespace TodoApi.Data;

// This class is accessible from different parts of the application but it is 
// sealed which means that no class can inherit from TodoDbContext. It basically 
// means that the context is not to be extended through child classes in any way.
//
// This line is roughly equivalent to:
// public sealed class TodoDbContext: DbContext
// {
//    public TodoDbContext(DbContextOptions<TodoDbContext> options) 
//    {
//      ...
//    }
// }
public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>(entity =>
            {
                entity.ToTable("todos");
                entity.HasKey(todo => todo.Id);
                entity.Property(todo => todo.Title).HasMaxLength(200);
            });
    }
}
