using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SEBotV2.API.Helpers
{
    public class ConfigItem
    {
        public string PropertyName { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class ConfigDbContext : DbContext
    {
        public DbSet<ConfigItem> ConfigItems => Set<ConfigItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=config.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfigItem>().HasKey(c => c.PropertyName);
            base.OnModelCreating(modelBuilder);
        }
    }
    
    public static class ConfigManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General)
        {
            WriteIndented = true
        };

        private static ConfigDbContext GetContext()
        {
            ConfigDbContext context = new();
            context.Database.EnsureCreated();
            return context;
        }

        public static async Task<bool> LoadBoolAsync(string propertyName, bool defaultValue = false)
        {
            try
            {
                using ConfigDbContext db = GetContext();
                ConfigItem? item = await db.ConfigItems.FindAsync(propertyName);
                if (item == null) 
                    return defaultValue;

                if (bool.TryParse(item.Value, out bool result))
                    return result;

                return defaultValue;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to load boolean '{propertyName}': {ex.Message}");
                return defaultValue;
            }
        }

        public static async Task<T?> LoadAsync<T>(string propertyName)
        {
            try
            {
                using ConfigDbContext db = GetContext();
                ConfigItem? item = await db.ConfigItems.FindAsync(propertyName);
                if (item == null)
                    return default;

                return JsonSerializer.Deserialize<T>(item.Value, _jsonOptions);
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to load '{propertyName}': {ex.Message}");
                return default;
            }
        }

        public static async Task AddTypeAsync<T>(string propertyName, T value)
        {
            try
            {
                using ConfigDbContext db = GetContext();
                ConfigItem? exists = await db.ConfigItems.FindAsync(propertyName);
                if (exists != null)
                {
                    LogManager.Debug($"'{propertyName}' already exists, skipping add.");
                    return;
                }

                ConfigItem item = new()
                {
                    PropertyName = propertyName,
                    Value = JsonSerializer.Serialize(value, _jsonOptions)
                };

                await db.ConfigItems.AddAsync(item);
                await db.SaveChangesAsync();
                LogManager.Debug($"Added '{propertyName}' to config.");
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to add '{propertyName}': {ex.Message}");
            }
        }

        public static async Task SaveAsync<T>(string propertyName, T value)
        {
            try
            {
                using ConfigDbContext db = GetContext();
                ConfigItem? item = await db.ConfigItems.FindAsync(propertyName);

                if (item == null)
                {
                    item = new ConfigItem
                    {
                        PropertyName = propertyName,
                        Value = JsonSerializer.Serialize(value, _jsonOptions)
                    };

                    await db.ConfigItems.AddAsync(item);
                }
                else
                {
                    item.Value = JsonSerializer.Serialize(value, _jsonOptions);
                    db.ConfigItems.Update(item);
                }

                await db.SaveChangesAsync();
                LogManager.Debug($"Saved '{propertyName}' to config.");
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to save '{propertyName}': {ex.Message}");
            }
        }
    }
}
