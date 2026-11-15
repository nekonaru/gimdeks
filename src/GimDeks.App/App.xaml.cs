using System.IO;
using System.Windows;
using GimDeks.App.Services;
using GimDeks.App.ViewModels;
using GimDeks.Core.Interfaces;
using GimDeks.Data;
using GimDeks.Data.Repositories;
using GimDeks.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace GimDeks.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static string LogPath => DatabasePaths.ResolveLogPath();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            WriteLog(args.Exception);
            MessageBox.Show(
                "Terjadi kesalahan tak terduga. Detail tersimpan di gimdeks.log. " + args.Exception.Message,
                "GimDeks",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            Services = BuildServices();
            var initializer = Services.GetRequiredService<DatabaseInitializer>();
            initializer.EnsureCreated();
        }
        catch (Exception exception)
        {
            WriteLog(exception);
            MessageBox.Show(
                "Database tidak bisa dibuka atau dibuat. Detail tersimpan di gimdeks.log.\n\n" + exception.Message,
                "GimDeks",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        var databasePath = DatabasePaths.ResolveDatabasePath();
        services.AddSingleton(new SqliteConnectionFactory(databasePath));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton(new DatabaseBackupService(
            new SqliteConnectionFactory(databasePath),
            DatabasePaths.ResolveBackupsDirectory()));
        services.AddSingleton<IStorageRepository, StorageRepository>();
        services.AddSingleton<IGameRepository, GameRepository>();
        services.AddSingleton<IHistoryRepository, HistoryRepository>();
        services.AddSingleton<ITagRepository, TagRepository>();
        services.AddSingleton<IDriveLinkRepository, DriveLinkRepository>();
        services.AddSingleton<ExcelImporter>();
        services.AddSingleton<ExcelImportService>();
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<ThemingService>();
        services.AddSingleton<DriveService>();
        services.AddSingleton<PortableModeService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<GameListViewModel>();
        services.AddSingleton<StorageViewModel>();
        services.AddSingleton<ExcludedViewModel>();
        services.AddSingleton<EliminatedViewModel>();
        services.AddSingleton<ImportExportViewModel>();
        services.AddSingleton<DuplicatesViewModel>();
        services.AddSingleton<HddSuggestionViewModel>();

        return services.BuildServiceProvider();
    }

    private static void WriteLog(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 1_000_000)
            {
                File.Move(LogPath, LogPath + ".old", overwrite: true);
            }

            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}\n\n");
        }
        catch
        {
            // Never crash while reporting a crash.
        }
    }
}
