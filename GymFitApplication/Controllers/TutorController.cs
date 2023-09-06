using Amazon.S3.Model;
using Amazon.S3;
using GymFitApplication.Data;
using GymFitApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Amazon; //for linking your AWS account
using Microsoft.EntityFrameworkCore;

namespace GymFitApplication.Controllers
{
    public class TutorController : Controller
    {
        private readonly GymFitApplicationContext _context;
        private const string s3BucketName = "gymfitapplication";

        private List<string> getKeys()
        {
            //create an empty list
            List<string> keys = new List<string>();

            //1. link to appsettings.json and get back the values
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build(); //build the json file

            //2. read the info from json using configure instance
            keys.Add(configure["Values:Key1"]);
            keys.Add(configure["Values:Key2"]);
            keys.Add(configure["Values:Key3"]);

            return keys;
        }

        public TutorController(GymFitApplicationContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> editpage(string? tutorid)
        {
            //Check id
            if (tutorid == null)
            {
                return NotFound();
            }

            var tutor = await _context.TutorTable.FindAsync(tutorid);

            //Check tutor
            if (tutor == null)
            {
                return NotFound();
            }

            //Redirect to edit page
            return View(tutor);
        }

        // Update Tutor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> updatepage(Tutor tutor, List<IFormFile> imagefiles)
        {
            if (ModelState.IsValid)
            {
                List<string> keys = getKeys();
                var awsS3client = new AmazonS3Client(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

                string imageUrl = null;

                //2. read image by image and store to S3
                foreach (var image in imagefiles)
                {
                    if (image.Length <= 0)
                    {
                        TempData["ImageUploadErrorMessage"] = "It is an empty file. Unable to upload!";
                        return View("editpage", tutor);
                    }
                    else if (image.Length > 1048576) //not more than 1MB
                    {
                        TempData["ImageUploadErrorMessage"] = "It is over 2MB limit of size. Unable to upload!";
                        return View("editpage", tutor);
                    }
                    else if (image.ContentType.ToLower() != "image/png" &&
                        image.ContentType.ToLower() != "image/jpeg"
                        && image.ContentType.ToLower() != "image/gif") //not valid image reject also
                    {
                        TempData["ImageUploadErrorMessage"] = "It is not a valid image! Unable to upload!";
                        return View("editpage", tutor);
                    }

                    //3. upload image to S3 and get the URL
                    try
                    {
                        //upload to S3
                        PutObjectRequest uploadRequest = new PutObjectRequest //generate the request
                        {
                            InputStream = image.OpenReadStream(),
                            BucketName = s3BucketName,
                            Key = "images/" + image.FileName,
                            CannedACL = S3CannedACL.PublicRead
                        };

                        //send out the request
                        await awsS3client.PutObjectAsync(uploadRequest);

                        imageUrl = $"https://{s3BucketName}.s3.amazonaws.com/images/{image.FileName}";
                    }
                    catch (AmazonS3Exception ex)
                    {
                        TempData["ImageUploadErrorMessage"] = ("Unable to upload to S3 due to technical issue. Error message: " + ex.Message);
                        return View("editpage", tutor);
                    }
                    catch (Exception ex)
                    {
                        TempData["ImageUploadErrorMessage"] = ("Unable to upload to S3 due to technical issue. Error message: " + ex.Message);
                        return View("editpage", tutor);
                    }

                }

                tutor.Image = imageUrl;

                var existingTutor = await _context.TutorTable.FindAsync(tutor.Id);

                if (existingTutor != null)
                {                     
                    existingTutor.Description = tutor.Description;
                    existingTutor.Title = tutor.Title;

                    if (tutor.Image != null)
                    {
                        existingTutor.Image = tutor.Image;
                    }

                    _context.TutorTable.Update(existingTutor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("TutorList");
                }
            }

            return View("editpage", tutor);
        }

        //Display tutorList in Management Section
        public async Task<IActionResult> TutorList(string? msg)
        {
            //Get tutor list from db table
            List<Tutor> tutorList
                = await _context.TutorTable.ToListAsync();

            return View(tutorList);
        }

        //Display tutorList in public view
        public async Task<IActionResult> ViewTutor()
        {
            //Get tutor list from db table
            List<Tutor> tutorList
                = await _context.TutorTable.ToListAsync();

            return View(tutorList);
        }

		public IActionResult Search(string searchString)
		{
			if (searchString != null)
			{
				List<Tutor> tutors = _context.TutorTable
				.Where(u => u.Name.Contains(searchString) || u.Title.Contains(searchString) || u.Description.Contains(searchString))
				.ToList();

				return View("TutorList", tutors);
			}

			List<Tutor> alltutors = _context.TutorTable.ToList();
			return View("TutorList", alltutors);
		}

		public async Task<IActionResult> deletepage(string? tutorid)
		{
			if (tutorid == null)
			{
				return BadRequest("Error: Tutor ID is not exists!");
			}

			var tutor = await _context.TutorTable.FindAsync(tutorid);

			if (tutor == null)
			{
				return BadRequest("Error: Not Tutor in the database!");
			}

            if(tutor.Image != null)
            {
                string imageUrl = tutor.Image;
                var uri = new Uri(imageUrl);
                var imageKey = uri.PathAndQuery.TrimStart('/');

                List<string> keys = getKeys();
                var awsS3client = new AmazonS3Client(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

                try
                {
                    DeleteObjectRequest request = new DeleteObjectRequest
                    {
                        BucketName = s3BucketName,
                        Key = imageKey
                    };
                    await awsS3client.DeleteObjectAsync(request);

                }
                catch (AmazonS3Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
			
			_context.TutorTable.Remove(tutor);
			await _context.SaveChangesAsync();
			return RedirectToAction("TutorList", new
			{
				msg = "Product with id" + tutorid
				+ "is removed from database!"
			});
		} 
	}
}
