using Microsoft.AspNetCore.Mvc;
using GymFitApplication.Data;
using GymFitApplication.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GymFitApplication.Controllers
{
	public class UserController : Controller
	{
		private readonly GymFitApplicationContext _context;

		public UserController(GymFitApplicationContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			return View();
		}

		//View user list
		public async Task<IActionResult> UserList(string? msg)
		{
			//Get user list from db table
			List<GymFitApplicationUser> userlist
				= await _context.GymFitApplicationUsers.ToListAsync();

			ViewBag.msg = msg;

			return View(userlist);
		}

		public async Task<IActionResult> StudentList(string? msg)
		{
			List<GymFitApplicationUser> userlist = new List<GymFitApplicationUser>();
			var currentUser = await _context.GymFitApplicationUsers.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

			if (currentUser != null)
			{
				var package = await _context.PackageTable.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

				if (package != null)
				{
					userlist = _context.GymFitApplicationUsers.Where(user => user.Packageid == package.PackageId).ToList();
				}
				else
				{
					ViewBag.msg = "No student";
				}
			}

			return View(userlist);
		}

		//Delete user
		public async Task<IActionResult> deletepage(string? userid)
		{
			if (userid == null)
			{
				return BadRequest("Error: User ID is not exists!");
			}

			var user = await _context.GymFitApplicationUsers.FindAsync(userid);

			if (user == null)
			{
				return BadRequest("Error: Not user in the database!");
			}

			_context.GymFitApplicationUsers.Remove(user);
			await _context.SaveChangesAsync();
			return RedirectToAction("UserList", new
			{
				msg = "User with id" + userid
				+ "is deleted from database!"
			});
		}

		//Edit user
		public async Task<IActionResult> editpage(string? userid)
		{
			//Check id
			if (userid == null)
			{
				return NotFound();
			}

			var user = await _context.GymFitApplicationUsers.FindAsync(userid);

			//Check user
			if (user == null)
			{
				return NotFound();
			}

			//Redirect to edit page
			return View(user);

		}

		//Update user
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> updatepage(GymFitApplicationUser user)
		{
			if (ModelState.IsValid)
			{
				var existingUser = await _context.GymFitApplicationUsers.FindAsync(user.Id);

				if (existingUser != null)
				{
					existingUser.Dateofbirth = user.Dateofbirth;
					existingUser.PhoneNumber = user.PhoneNumber;
					existingUser.Gender = user.Gender;

					_context.GymFitApplicationUsers.Update(existingUser);
					await _context.SaveChangesAsync();

					List<GymFitApplicationUser> allusers = _context.GymFitApplicationUsers.ToList();
					return View("UserList", allusers);
				}
			}

			return View("editpage", user);
		}

		//Search user
		public IActionResult Search(string searchString)
		{
			if (searchString != null)
			{
				List<GymFitApplicationUser> users = _context.Users
				.Where(u => u.UserName.Contains(searchString) || u.Email.Contains(searchString) || u.Rolename.Contains(searchString))
				.ToList();

				return View("UserList", users);
			}

			List<GymFitApplicationUser> allusers = _context.GymFitApplicationUsers.ToList();
			return View("UserList", allusers);

		}

	}
}
