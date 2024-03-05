using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using DCMLocker.Server.Hubs;
using System.Threading.Tasks;
using System.IO.Ports;

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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Inicia el servicio en un hilo aparte
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
                //foreach (string portName in SerialPort.GetPortNames()) // Cambiado a SerialPort.GetPortNames()
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
                Task.Delay(TimeSpan.FromSeconds(10)).Wait(); // Cambiado a Task.Delay(TimeSpan.FromSeconds(10)).Wait()
            }

            // Cerrar el puerto cuando ya no se necesite
            _portReader.Close();
        }
    }
}
