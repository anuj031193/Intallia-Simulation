using System;
using System.Configuration;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobSimulation.Forms;
using JobSimulation.DAL;
using JobSimulation.BLL;

namespace JobSimulation
{
    class Program
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Program> _logger;

        public Program(IServiceProvider serviceProvider, ILogger<Program> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [STAThread]
        static void Main()
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            var program = serviceProvider.GetRequiredService<Program>();

            try
            {
                program.Run();
            }
            catch (Exception ex)
            {
                program.HandleFatalException(ex);
            }
        }

        public void Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                ConfigureExceptionHandling();

                // Verify database connection
                var userRepository = _serviceProvider.GetService<UserRepository>();
                userRepository.TestConnection();

                Application.Run(_serviceProvider.GetRequiredService<frmUserLogin>());
            }
            catch (Exception ex)
            {
                HandleFatalException(ex);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"]?.ConnectionString
                ?? throw new ConfigurationErrorsException("Missing database connection string");

            // Configure logging
            services.AddLogging(builder =>
                builder.AddConsole()
                       .AddDebug()
                       .SetMinimumLevel(LogLevel.Debug));

            // Database Access
            services.AddScoped<UserRepository>(_ => new UserRepository(connectionString));
            services.AddScoped<SectionRepository>(_ => new SectionRepository(connectionString));
            services.AddScoped<SkillMatrixRepository>(_ => new SkillMatrixRepository(connectionString));
            services.AddScoped<TaskRepository>(_ => new TaskRepository(connectionString));
            services.AddScoped<ActivityRepository>(provider =>
            {
                var sectionService = provider.GetRequiredService<SectionService>();
                var skillMatrixRepository = provider.GetRequiredService<SkillMatrixRepository>();
                var taskRepository = provider.GetRequiredService<TaskRepository>();
                return new ActivityRepository(connectionString, sectionService, skillMatrixRepository, taskRepository);
            });

            // Business Logic
            services.AddScoped<FileService>();
            services.AddScoped<SectionService>();

            // Forms
            services.AddTransient<frmUserLogin>();
            services.AddTransient<frmSectionLauncher>();
            services.AddTransient<frmSimulationSoftware>();
            services.AddTransient<frmSimulationLibrary>();

            services.AddSingleton<Program>(); // Register Program class as a singleton

        }

        private void ConfigureExceptionHandling()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
                HandleException(e.Exception, "UI Thread Exception");

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                HandleException(e.ExceptionObject as Exception, "Domain Exception");
        }

        private void HandleException(Exception ex, string context)
        {
            _logger?.LogCritical(ex, $"Unhandled {context}");
            MessageBox.Show(
                "A critical error occurred. Please restart the application.",
                "Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void HandleFatalException(Exception ex)
        {
            try
            {
                _logger?.LogCritical(ex, "Application bootstrapping failed");
            }
            finally
            {
                MessageBox.Show(
                    "Failed to initialize application. Please check configuration.",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
    }
}
