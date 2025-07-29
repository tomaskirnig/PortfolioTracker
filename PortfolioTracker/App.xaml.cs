using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioTracker.Services;
using System.Net.Http;

namespace PortfolioTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Načtěte konfiguraci
            var configuration = new ConfigurationBuilder().AddUserSecrets<App>().Build();

            var services = new ServiceCollection();

            // Registrujte konfiguraci
            services.AddSingleton<IConfiguration>(configuration);

            // Registrujte služby
            services.AddHttpClient<ICoinbaseApiService, CoinbaseApiService>();
            

            services.AddSingleton<CoinbaseJwtService>(provider =>
            {
                var config = provider.GetService<IConfiguration>();
                var keyName = config["Coinbase:KeyName"];
                var privateKey = config["Coinbase:PrivateKey"];
                return new CoinbaseJwtService(keyName, privateKey);
            });

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = new MainWindow(_serviceProvider.GetService<ICoinbaseApiService>());
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }

}
