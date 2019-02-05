using System.Runtime.InteropServices;
using DesktopNotifications;

namespace Zombie
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("6cd2f896-4216-4014-a033-1f0b9e2a1cba"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
        {

        }
    }
}
