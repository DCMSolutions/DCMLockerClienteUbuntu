using System;
using System.IO.Ports;
using System.Text;

public class SerialPortReader
{
    private SerialPort _serialPort;

    public SerialPortReader(string portName)
    {
        try
        {
            _serialPort = new SerialPort(portName, 9600);
            _serialPort.DataReceived += SerialPortDataReceived;
            _serialPort.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No hay lector QR conectado en {portName}.");
        }
    }

    private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        string data = sp.ReadLine();
        Console.WriteLine("Datos recibidos: " + data);
    }

    public bool HayQR()
    {
        return _serialPort != null && _serialPort.IsOpen;
    }

    public string ReadData()
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            return null;
        }
        else
        {
            return _serialPort.ReadLine();
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
