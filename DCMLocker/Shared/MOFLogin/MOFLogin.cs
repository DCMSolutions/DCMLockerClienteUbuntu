using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCMLocker.Shared.MOFLogin
{
    public class TMOFUserCreate
    {
        public string user { get; set; }
        public string pass { get; set; }
        public string confirmpass { get; set; }
    }

    public class TMOFError
    {
        public int Code { get; set; }
        public string Msn { get; set; }
    }
    public class TMOFAuthentication
    {
        public string Token { get; set; }
        public string Secret { get; set; }
        public DateTime ClienteExpire { get; set; }
        public TMOFError Error { get; set; }
    }

    public class TMOFUser
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Pass { get; set; }
        public string DNI { get; set; }

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Imagen { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }

        public bool LockoutEnable { get; set; }
        public DateTimeOffset LockoutEnd { get; set; }
        public int AccessFaledCount { get; set; }


        public List<TMOFRoleUser> Roles { get; set; }

    }

    public class TMOFRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Dsc { get; set; }

        public List<TMOFRoleUser> Users { get; set; }
    }

    public class TMOFRoleUser
    {
        public int IdRole { get; set; }
        public Guid IdUser { get; set; }


    }
}
