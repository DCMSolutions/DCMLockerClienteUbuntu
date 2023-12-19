using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCMLocker.Server.BaseController
{
    public class BoxTokenKey
    {
        public string Token { get; set; }
        public string Tag { get; set; }
        public int Box { get; set; }
        public string User { get; set; }
        public DateTime DTExpiration { get; set; }
    }
}
