using System;
using System.Reflection;
using System.ServiceModel;
using Zombie.Utilities;
using ZombieUtilities;

namespace ZombieService.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ZombieTalker : IZombieTalker
    {
        public ZombieSettings GetSettings()
        {
            return Program.Settings;
        }

        public bool SetSettings(ZombieSettings settings)
        {
            try
            {
                // (Konrad) Update Service Settings.
                Program.Settings = settings;

                // (Konrad) Update Registry
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                RegistryUtils.SetImagePath(settings, assemblyLocation);

                // (Konrad) This flag is true for local settings only.
                if (!settings.StoreSettings || !SettingsUtils.StoreSettings(settings))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}
