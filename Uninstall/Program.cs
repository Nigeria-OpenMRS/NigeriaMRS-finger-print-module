using System;
using System.ServiceProcess;

namespace Uninstall
{
    class Program
    {
        static void Main(string[] args)
        {
            StopService();
        }

        static void StopService()
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = "FingerPrintModule";
            Console.WriteLine("The Finger Print Module service is stopping {0}",
                               sc.Status.ToString());

            if (sc.Status != ServiceControllerStatus.Stopped)
            { 
                try
                {                    
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                     
                    Console.WriteLine("Succesfully stopped the Finger Print Module {0}.",
                                       sc.Status.ToString());
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error occured." + ex.ToString());
                    Console.Read();
                }
            }
        }
        
    }
}
