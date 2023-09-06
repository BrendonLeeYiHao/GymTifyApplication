using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace GymFitApplication.Areas.Identity.Data;

// Add profile data for application users by adding properties to the GymFitApplicationUser class
public class GymFitApplicationUser : IdentityUser
{
    [PersonalData]
    public DateTime Dateofbirth { get; set; }


    [PersonalData]
    public string Gender { get; set; }

    [PersonalData]
    public string Rolename { get; set; }

    [PersonalData]
    public int Packageid { get; set; }

}

