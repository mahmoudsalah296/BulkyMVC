using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.Models.ViewModels;

public class UserVM
{
    public ApplicationUser User { get; set; }
    public IEnumerable<SelectListItem> RolesList { get; set; }
    public IEnumerable<SelectListItem>? CompanyList { get; set; }
}
