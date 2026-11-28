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

    private static void Log(Exception exception) => CrashLog.Write(exception);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            MessageBox.Show(
                GimDeks.App.Services.Loc.Get("Ui_Crash_Body") + " " + args.Exception.Message,
                "GimDeks",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            Services = BuildServices();
            var settings = Services.GetRequiredService<AppSettingsService>();
            Services.GetRequiredService<LocalizationService>().Apply(settings.Load().UiLanguage);
            var initializer = Services.GetRequiredService<DatabaseInitializer>();
            initializer.EnsureCreated();
        }
        catch (Exception exception)
        {
            Log(exception);
            MessageBox.Show(
                GimDeks.App.Services.Loc.Get("Ui_Db_Body") + "\n\n" + exception.Message,
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
        var initialSettings = new AppSettingsService().Load();
        services.AddSingleton(new SqliteConnectionFactory(databasePath));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton(new DatabaseBackupService(
            new SqliteConnectionFactory(databasePath),
            DatabasePaths.ResolveBackupsDirectory())
        {
            MaxBackups = Math.Clamp(initialSettings.BackupRetention, 1, 200),
        });
        services.AddSingleton<CsvExporter>();
        services.AddSingleton<CollectionBundleService>();
        services.AddSingleton<CsvImportService>();
        services.AddSingleton<IStorageRepository, StorageRepository>();
        services.AddSingleton<IGameRepository, GameRepository>();
        services.AddSingleton<IHistoryRepository, HistoryRepository>();
        services.AddSingleton<ITagRepository, TagRepository>();
        services.AddSingleton<IDriveLinkRepository, DriveLinkRepository>();
        services.AddSingleton<IPlaySessionRepository, PlaySessionRepository>();
        services.AddSingleton<IGameNoteRepository, GameNoteRepository>();
        services.AddSingleton<IGameImageRepository, GameImageRepository>();
        services.AddSingleton<IFilterPresetRepository, FilterPresetRepository>();
        services.AddSingleton<IImportHistoryRepository, ImportHistoryRepository>();
        services.AddSingleton<ICapacitySnapshotRepository, CapacitySnapshotRepository>();
        services.AddSingleton<IActivityRepository, ActivityRepository>();
        services.AddSingleton<ILaunchProfileRepository, LaunchProfileRepository>();
        services.AddSingleton<ICustomFieldRepository, CustomFieldRepository>();
        services.AddSingleton<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddSingleton<ISaveGameRepository, SaveGameRepository>();
        services.AddSingleton<IModRepository, ModRepository>();
        services.AddSingleton<SessionTrackerService>();
        services.AddSingleton<UndoRedoService>();
        services.AddSingleton<ExcelImporter>();
        services.AddSingleton<ExcelImportService>();
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<ThemingService>();
        services.AddSingleton<LocalizationService>();
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
        services.AddSingleton<StatisticsViewModel>();
        services.AddSingleton<HealthViewModel>();
        services.AddSingleton<ScanGamesViewModel>();
        services.AddSingleton<BackupManagerViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ActivityViewModel>();
        services.AddSingleton<MiniPlayerViewModel>();
        services.AddSingleton<AutomationService>();
        services.AddSingleton<AutomationViewModel>();

        return services.BuildServiceProvider();
    }

}

