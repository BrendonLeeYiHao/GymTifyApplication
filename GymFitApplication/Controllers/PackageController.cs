using GymFitApplication.Areas.Identity.Data;
using GymFitApplication.Data;
using GymFitApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;

namespace GymFitApplication.Controllers
{
	public class PackageController : Controller
	{
		private readonly GymFitApplicationContext _context;
		private readonly UserManager<GymFitApplicationUser> _userManager;

		public PackageController(GymFitApplicationContext context, UserManager<GymFitApplicationUser> userManager)
		{
			_context = context;
            _userManager = userManager;
		}

		public IActionResult Index()
		{
			return View();
		}


        //Create Package
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Package package)
		{            
			if (ModelState.IsValid)
			{
				_context.PackageTable.Add(package);
				await _context.SaveChangesAsync();
				return RedirectToAction("PackageList");
			}				
			return View(package);
		}

        //View package
        public async Task<IActionResult> PackageList(string? msg)
		{
			//Get user list from db table
			List<Package> packagelist
				= await _context.PackageTable.ToListAsync();

			ViewBag.msg = msg;

			return View(packagelist);
		}

        //Delete package
        public async Task<IActionResult> deletepage(int? packageid)
        {
            if (packageid == null)
            {
                return BadRequest("Error: User ID is not exists!");
            }

            var package = await _context.PackageTable.FindAsync(packageid);

            if (package == null)
            {
                return BadRequest("Error: Not user in the database!");
            }

            _context.PackageTable.Remove(package);
            await _context.SaveChangesAsync();
            return RedirectToAction("PackageList", new
            {
                msg = "Package with id" + packageid
                + "is deleted from database!"
            });
        }

        //Edit package
        public async Task<IActionResult> editpage(int? packageid)
        {
            //Check id
            if (packageid == null)
            {
                return NotFound();
            }

            var package = await _context.PackageTable.FindAsync(packageid);

            //Check user
            if (package == null)
            {
                return NotFound();
            }

            //Redirect to edit page
            return View(package);

        }

        //Update package
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> updatepage(Package package)
        {
            if (ModelState.IsValid)
            {
                var existingPackage = await _context.PackageTable.FindAsync(package.PackageId);

                if (existingPackage != null)
                {
                    existingPackage.Name = package.Name;
                    existingPackage.Description = package.Description;
                    existingPackage.Price = package.Price;
                    existingPackage.Duration = package.Duration;
                    existingPackage.UserId = package.UserId;

                    _context.PackageTable.Update(existingPackage);
                    await _context.SaveChangesAsync();

                    List<Package> allpackages = _context.PackageTable.ToList();
                    return View("PackageList", allpackages);
                }
            }

            return View("editpage", package);
        }

        //Search user
        public IActionResult Search(string searchString)
        {
            if (searchString != null)
            {
                List<Package> packages = _context.PackageTable
                .Where(u => u.Name.Contains(searchString) || u.Description.Contains(searchString))
                .ToList();

                return View("PackageList", packages);
            }

            List<Package> allpackages = _context.PackageTable.ToList();
            return View("PackageList", allpackages);
        }

		public async Task<IActionResult> ViewPackage()
		{
			//Get user list from db table
			List<Package> packagelist
				= await _context.PackageTable.ToListAsync();

			return View(packagelist);
		}

        public async Task<IActionResult> Subscribe(int packageid)
        {
			if (User.Identity.IsAuthenticated)
            {
				//Get Current User
				var currentUser = await _userManager.GetUserAsync(User);

				if (currentUser != null)
				{
					var selectedPackage = await _context.PackageTable.FindAsync(packageid);

					if (selectedPackage != null)
					{
						// Update the user's PackageId
						currentUser.Packageid = packageid;
						_context.GymFitApplicationUsers.Update(currentUser);
						await _context.SaveChangesAsync();

						TempData["SubscriptionSuccessMessage"] = "Subsribe Package Sucessfully!";

						return RedirectToAction("ViewPackage");
					}
				}				
			}
			TempData["NotAuthenticatedMessage"] = "Please login to the account to subsribe!";
			return RedirectToAction("ViewPackage");
		}
	}
}
