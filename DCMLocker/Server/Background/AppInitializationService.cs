using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using DCMLocker.Server.Hubs;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using RJCP.IO.Ports;
using System.Linq;

namespace DCMLocker.Server.Background
{
    public class AppInitializationService : IHostedService
    {
        private Thread _work;
        private SerialPortReader _portReader;

        private readonly QRReaderHub _qrReaderHub;

        public AppInitializationService(QRReaderHub qrReaderHub)
        {
            _qrReaderHub = qrReaderHub;
        }

        public async Task Start()
        {
            StartAsync(cancellationToken: System.Threading.CancellationToken.None);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Coloca aquí el código que deseas ejecutar al iniciar la aplicación
            // Por ejemplo, puedes realizar configuraciones iniciales, cargar datos, etc.
            try
            {
                _work = new Thread(StartReading);
                _work.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Indicar que se ha solicitado detener el servicio

            // Esperar a que el thread termine su ejecución
            if (_work != null && _work.IsAlive)
            {
                _work.Join(); // Esperar a que el thread termine
            }
            // Realizar limpieza o acciones finales aquí

            await Task.CompletedTask;
        }

        public async void StartReading()
        {

            while (true)
            {
                //foreach (string portName in SerialPortStream.GetPortNames())
                //{
                string portName = "ttyACM0";
                _portReader = new SerialPortReader(portName);
                if (_portReader.HayQR())
                {
                    Console.WriteLine("Hay lector QR conectado");
                    string data = "";
                    while (data != null)
                    {
                        data = _portReader.ReadData();
                        Console.WriteLine("read " + data);
                        if (data != "") await _qrReaderHub.SendToken(data);
                    }
                }
                //}
                Console.WriteLine("chequeo los puertos y no hay qr, espera 10seg");
                await Task.Delay(TimeSpan.FromSeconds(10));
            }


            // Cerrar el puerto cuando ya no se necesite
            _portReader.Close();
        }


    }
}
