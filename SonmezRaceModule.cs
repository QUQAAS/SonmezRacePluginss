using AssettoServer.Server.Plugin;
using Autofac;
using Microsoft.Extensions.Hosting;

namespace SonmezRacePlugin;

public class SonmezRaceModule : AssettoServerModule<SonmezRaceConfiguration>
{
    protected override void Load(ContainerBuilder builder)
    {
        // 0.0.55: IAssettoServerAutostart yok, duz IHostedService kullanilir
        builder.RegisterType<SonmezRaceService>().AsSelf().As<IHostedService>().SingleInstance();
    }
}
