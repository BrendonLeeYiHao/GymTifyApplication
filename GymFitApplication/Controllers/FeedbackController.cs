using GymFitApplication.Data;
using GymFitApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace GymFitApplication.Controllers
{
    public class FeedbackController : Controller
    {
        public readonly GymFitApplicationContext _context;
        public readonly ILogger<FeedbackController> _logger;

        public FeedbackController(GymFitApplicationContext context, ILogger<FeedbackController> logger)
        {
            _context = context; //database
            _logger = logger;
        }

        public IActionResult ContactPage()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken] //prevent cross-site attack
        public async Task<IActionResult> ContactPage(Feedback feedback) //insert data function
        {
            if (ModelState.IsValid)
            {
                _context.FeedbackTable.Add(feedback);
                await _context.SaveChangesAsync();  //commit changes and save to database
                TempData["StatusMessage"] = "Feedback is provided successfully!";
                return RedirectToAction("ContactPage");
            }
            return View(feedback);
        }


        public async Task<IActionResult> ViewFeedbackPage(string SearchString)
        {
            List<Feedback> feedbackList =  await _context.FeedbackTable.ToListAsync();

            if (feedbackList.Count == 0)
            {
                ViewBag.empty = "Empty";
                return View();
            }

            ViewBag.empty = "Not Empty";

            if (!string.IsNullOrEmpty(SearchString)) //if searchstring not empty
            {
                feedbackList = feedbackList.Where(s => s.Name.Contains(SearchString)
                                                    || s.Email.Contains(SearchString)).ToList();
            }

            return View(feedbackList);
        }
    }
}
