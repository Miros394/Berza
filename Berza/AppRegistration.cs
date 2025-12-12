using Microsoft.Toolkit.Uwp.Notifications;
using System;

namespace Berza
{
    public static class AppRegistration
    {
        private const string AUMID = "Berza.App";

        public static void RegistrationCheck()
        {
            try
            {
                DesktopNotificationManagerCompat.RegisterAumidAndComServer<NotificationActivator>(AUMID);

                Console.WriteLine("Toast sistem registrovan.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("GREŠKA pri registraciji toast sistema: " + ex.Message);
            }
        }

        public static string GetAumid() => AUMID;
    }
}
