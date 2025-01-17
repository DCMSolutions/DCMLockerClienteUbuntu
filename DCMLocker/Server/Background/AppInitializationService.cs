using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using DCMLocker.Server.Hubs;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace DCMLocker.Server.Background
{
    public class AppInitializationService : IHostedService
    {
        private Thread _work;


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
            try
            {
                // Directorio donde se encuentran los dispositivos serie en Linux
                string devicesDirectory = "/dev/";

                // Obtener todos los archivos en el directorio de dispositivos
                string[] devices = Directory.GetFiles(devicesDirectory);

                // Filtrar solo los dispositivos serie (puertos COM)
                var serialDevices = Array.FindAll(devices, d => d.StartsWith("/dev/tty"));

                if (serialDevices.Length == 0)
                {
                    Console.WriteLine("No se encontraron puertos COM activos.");
                }
                else
                {
                    // Tomar el primer puerto serie disponible
                    string firstPort = serialDevices[0];

                    Console.WriteLine("Intentando conectar al primer puerto disponible: " + firstPort);

                    try
                    {
                        // Abrir el flujo de archivos para el puerto serie
                        using (FileStream serialStream = new FileStream(firstPort, FileMode.Open))
                        {
                            Console.WriteLine("Conexión establecida correctamente. Esperando datos...");

                            // Bucle continuo para leer los datos del puerto serie
                            while (true)
                            {
                                byte[] buffer = new byte[1024];
                                int bytesRead = serialStream.Read(buffer, 0, buffer.Length);

                                if (bytesRead > 0)
                                {
                                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    try
                                    {
                                        if (data != null) await _qrReaderHub.SendToken(data);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al conectar al puerto serie: " + ex.Message);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Hubo un error con el lector QR.");
            }
        }
    }
}
