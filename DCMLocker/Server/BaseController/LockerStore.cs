using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCMLocker.Server.BaseController
{
    public interface ILockerStore
    {
        void Save(string Path);
    }
}
