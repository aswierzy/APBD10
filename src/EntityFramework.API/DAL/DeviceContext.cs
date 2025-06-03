using Microsoft.EntityFrameworkCore;

namespace EntityFramework.DAL;

public class DeviceContext : DbContext
{
    public DeviceContext(DbContextOptions<DeviceContext> options) : base(options) { }
    
    public DbSet<Device> Device { get; set; }
    public DbSet<Employee> Employee{ get; set; }
    public DbSet<DeviceType> DeviceType{ get; set; }
    public DbSet<DeviceEmployee> DeviceEmployee { get; set; }
    public DbSet<Person> Person { get; set; }
    public DbSet<Position> Position{ get; set; }
    public DbSet<Account> Account { get; set; }
    public DbSet<Role> Role{ get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>()
            .HasIndex(dt => dt.Name)
            .IsUnique();
        
        
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Salary)
                .HasColumnType("decimal(18,2)");
        });
    }
}