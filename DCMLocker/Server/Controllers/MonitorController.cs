using DCMLocker.Server.BaseController;
using DCMLocker.Server.Hubs;
using DCMLocker.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCMLocker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IHubContext<LockerHub, ILockerHub> _hubContext;
        private readonly TBaseLockerController _base;
        public MonitorController(IHubContext<LockerHub, ILockerHub> context2, ILogger<LockerController> logger, TBaseLockerController Base)
        {
            _base = Base;
            _log = logger;
            _hubContext = context2;
        }

        
    }
}
