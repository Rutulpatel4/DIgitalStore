using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DigitalStore.Startup))]
namespace DigitalStore
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
