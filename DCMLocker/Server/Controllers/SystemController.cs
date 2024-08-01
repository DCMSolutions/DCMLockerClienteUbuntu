using DCMLocker.Server.Background;
using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Server.Controllers
{
    [Route("ClientApp/[controller]")]
    [Route("KioskApp/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {

        private readonly TBaseLockerController _base;

        /// <summary> -----------------------------------------------------------------------
        /// Constructor
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="context2"></param>
        /// <param name="logger"></param>
        /// <param name="Base"></param>------------------------------------------------------
        public SystemController(TBaseLockerController Base)
        {
            _base = Base;

        }

        [HttpGet("GetIP")]
        public SystemNetwork[] GetIP()
        {
            List<SystemNetwork> retorno = new List<SystemNetwork>() { };
            //var ips =Dns.GetHostEntry(Dns.GetHostName());
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {


                if (((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet) ||
                    (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)) && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            SystemNetwork r = new SystemNetwork();
                            r.NetworkInterfaceType = item.NetworkInterfaceType.ToString();
                            r.NetworkOperationalStatus = item.OperationalStatus.ToString();
                            r.IP = ip.Address.ToString();
                            r.NetMask = ip.IPv4Mask.ToString();
                            retorno.Add(r);
                        }
                    }
                }

            }


            return retorno.ToArray();
        }

        [HttpGet("GetSSID")]
        public string[] GetSSID()
        {
            try
            {
                string s0 = cmd("iwconfig wlan0");
                string s1 = cmd("iwlist wlan0 scan | grep ESSID");

                return new string[] { s0, s1 };
                //return new string[] { System.IO.File.ReadAllText("/etc/wpa_supplicant/wpa_supplicant.conf") };
            }
            catch (Exception er)
            {
                return new string[] { "ERROR:" + er.Message };
            }

            /*
            using (Process compiler = new Process())
            {
                compiler.StartInfo.FileName = "nano";
                compiler.StartInfo.Arguments = "/etc/wp";
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.RedirectStandardOutput = true;
                compiler.Start();

                Console.WriteLine(compiler.StandardOutput.ReadToEnd());

                compiler.WaitForExit();
            }
            */
        }

        [HttpPost("SetSSID")]
        public ActionResult SetSSID([FromBody] SystemSSID ssid)
        {
            string s = "ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev"
                + Environment.NewLine
                + "update_config=1"
                + Environment.NewLine
                + "country=AR" + Environment.NewLine +
                "network={ "
                + Environment.NewLine
                + $"ssid=\"{ssid.SSID}\""
                + Environment.NewLine
                + $"psk=\"{ssid.Pass}\""
                + Environment.NewLine
                + "key_mgmt=WPA-PSK"
                + Environment.NewLine
                + "}";
            try
            {
                System.IO.File.WriteAllText("/etc/wpa_supplicant/wpa_supplicant.conf", s);
                cmd("wpa_supplicant -c /etc/wpa_supplicant/wpa_supplicant.conf -i wlan0");
                return Ok();
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }
        [HttpPost("Filesave")]
        public async Task<ActionResult> PostFile(
        [FromForm] IEnumerable<IFormFile> files)
        {
            var maxAllowedFiles = 3;
            var filesProcessed = 0;
            foreach (var file in files)
            {
                string fileExtension = Path.GetExtension(file.FileName);
                var untrustedFileName = $"tamaños{fileExtension}";

                if (filesProcessed < maxAllowedFiles)
                {

                    try
                    {
                        var path = Path.Combine(_base.PathBase, untrustedFileName);

                        await using FileStream fs = new(path, FileMode.Create);
                        await file.CopyToAsync(fs);



                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }


                    filesProcessed++;

                }

            }
            return Ok();
        }

        [HttpPost("SetWLAN")]
        public ActionResult SetWLAN([FromBody] bool state)
        {
            try
            {
                string s0 = cmd("ifconfig wlan0 " + (state ? "up" : "down"));
                return Ok(s0);
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }

        [HttpPost("Reset")]
        public ActionResult Reset()
        {
            try
            {
                string s0 = cmd("reboot");
                return Ok(s0);
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }

        [HttpPost("Shutdown")]
        [Authorize(Roles = "Admin")]
        public ActionResult Shutdown()
        {
            try
            {
                string s0 = cmd("shutdown -h +1");
                return Ok(s0);
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }

        [HttpPost("Update")]
        public ActionResult Update()
        {
            try
            {
                string s0 = cmd("wget -O - https://github.com/DCMSolutions/DCMLockerUpdate/blob/main/update.sh | bash");
                return Ok(s0);
            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }

        [HttpGet("ResetService")]
        public string ResetService()
        {
            try
            {
                cmd("systemctl restart dcmlocker.service");
                return "Reseteado con éxito";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Error al resetear";
            }
        }

        private string cmd(string comando)
        {
            string s = "NO";
            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = "/bin/bash";
                cmd.StartInfo.Arguments = $"-c \"sudo {comando}\"";
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.Start();
                s = cmd.StandardOutput.ReadToEnd();
                cmd.WaitForExit();
            }
            return s;
        }

        [HttpGet("GetEthernetConfig")]
        public SystemNetwork GetEthernetConfig()
        {
            SystemNetwork retorno = new SystemNetwork();
            //var ips =Dns.GetHostEntry(Dns.GetHostName());
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {


                if ((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet) && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            retorno.NetworkInterfaceType = item.NetworkInterfaceType.ToString();
                            retorno.NetworkOperationalStatus = item.OperationalStatus.ToString();
                            retorno.IP = ip.Address.ToString();
                            retorno.NetMask = ip.IPv4Mask.ToString();
                        }
                    }
                }

            }


            return retorno;
        }

        [HttpPost("SetEthernetIp")]
        public ActionResult SetEthernetIp([FromBody] SystemEthernet eth)
        {


            Console.WriteLine(eth.CalculateGateway());
            string routers = "";
            string s = "interface eth0"
                + Environment.NewLine
                + "metric 303"
                + Environment.NewLine
                + $"static ip_address={eth.IP}" + Environment.NewLine +
                "static routers={routers} "
                + Environment.NewLine
                + $"static domain_name_servers={routers}"
                + Environment.NewLine
                + "interface wlan0"
                + Environment.NewLine
                + "metric 200"
                ;
            try
            {
                //System.IO.File.WriteAllText("/etc/dhcpcd.conf", s);
                //cmd("wpa_supplicant -c /etc/wpa_supplicant/wpa_supplicant.conf -i wlan0");
                return Ok(eth.CalculateGateway());

            }
            catch (Exception er)
            {
                return BadRequest(er.Message);
            }
        }

    }
}
