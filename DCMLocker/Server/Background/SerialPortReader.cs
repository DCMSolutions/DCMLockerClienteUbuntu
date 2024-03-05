using RJCP.IO.Ports;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public class SerialPortReader
{
    private SerialPortStream _serialPort;

    public SerialPortReader(string portName)
    {
        try
        {
            _serialPort = new SerialPortStream(portName, 9600); // portName con velocidad de baudios de 9600 (ajusta según la configuración del dispositivo)
            _serialPort.DataReceived += SerialPortDataReceived; // Suscripción al evento de recepción de datos
            _serialPort.Open(); // Abre el puerto
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No hay lector QR conectado en {portName}.");
        }
    }

    private async void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPortStream sp = (SerialPortStream)sender;
        byte[] buffer = new byte[sp.BytesToRead];
        sp.Read(buffer, 0, buffer.Length);
        string data = System.Text.Encoding.UTF8.GetString(buffer); // Decodifica los bytes recibidos
    }

    public bool HayQR()
    {
        if (_serialPort == null || !_serialPort.IsOpen) return false;
        return true;
    }

    public string ReadData()
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            return null;
        }
        else
        {
            byte[] buffer = new byte[1024]; // Buffer para almacenar los datos leídos del puerto serie
            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return data;
            }
            else
            {
                return "";
            }
        }
    }

    public void Close()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }
    }
}
