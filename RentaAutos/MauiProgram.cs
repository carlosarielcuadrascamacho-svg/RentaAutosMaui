using Microsoft.Extensions.Logging;
using RentaAutos.Data;
using RentaAutos.Controllers;
using RentaAutos.Views;
using Syncfusion.Maui.Toolkit.Hosting;

namespace RentaAutos
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            
            //singleton = solo queremos una unica conexión a la base de datos abierta en toda la app
            builder.Services.AddSingleton<MongoDbContext>();
            builder.Services.AddSingleton<AutosRepository>();

            //Transient: Queremos un controlador y una vista nueva cada vez que entremos a la pantalla
            builder.Services.AddTransient<RegistroController>();
            builder.Services.AddTransient<GaleriaController>(); 
            builder.Services.AddTransient<ReportesController>();
            builder.Services.AddTransient<GaleriaView>();
            builder.Services.AddTransient<RegistroRentaView>();
            builder.Services.AddTransient<ReportesView>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }
    }
}