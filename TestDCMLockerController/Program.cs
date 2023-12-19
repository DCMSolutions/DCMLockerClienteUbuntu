using System;
using DCMLockerCommunication;

namespace TestDCMLockerController
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DCMLockerTCPDriver
                driver = new DCMLockerTCPDriver();
            driver.IP = "192.168.2.178";
            driver.Port = 4001;
            driver.OnConnection += Driver_OnConnection;
            driver.OnDisConnection += Driver_OnDisConnection;
            driver.OnError += Driver_OnError;
            driver.OnCUChange += Driver_OnCUChange;

            driver.Start();
            Console.WriteLine("Presione q  para terminar ");
            while (Console.ReadKey().KeyChar != 'q')
            {
                driver.OpenBox(0, 15);
            }
            Console.WriteLine("Cerrando ");
            driver.Stop();
            Console.WriteLine("Fin ");
        }

        private static void Driver_OnError(object sender, EventArgs e)
        {
            Console.WriteLine("ERROR:" + ((EvtArgError)e).Er.Message);
        }

        private static void Driver_OnDisConnection(object sender, EventArgs e)
        {
            Console.WriteLine("DESCONEXION");
        }

        private static void Driver_OnConnection(object sender, EventArgs e)
        {
            //Console.WriteLine("--------- CONEXION-----------------");
        }

        private static void Driver_OnCUChange(object sender, EventArgs e)
        {
            EvtArgCUChange v = (EvtArgCUChange)e;
            Console.WriteLine("CAMBIO:");
            for (int x = 0; x < 16; x++)
            {
                Console.WriteLine($"PUERTAS [{x}]: {v.CU.DoorStatus[x]}");
                Console.WriteLine($"SENSOR [{x}]: {v.CU.SensorStatus[x]}");
            }
            

        }
    }
}
