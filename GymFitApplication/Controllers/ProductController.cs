using GymFitApplication.Data;
using GymFitApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Amazon; //for linking your AWS account
using Amazon.S3;
using Amazon.S3.Model;

namespace GymFitApplication.Controllers
{
    public class ProductController : Controller
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

		public ProductController(GymFitApplicationContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Create Product
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Product product, List<IFormFile> imagefiles)
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
						return View("Index",product);
					}
					else if (image.Length > 2048576) //not more than 2MB
					{
						TempData["ImageUploadErrorMessage"] = "It is over 2MB limit of size. Unable to upload!";
                        return View("Index", product);
                    }
					else if (image.ContentType.ToLower() != "image/png" &&
						image.ContentType.ToLower() != "image/jpeg"
						&& image.ContentType.ToLower() != "image/gif") //not valid image reject also
					{
						TempData["ImageUploadErrorMessage"] = "It is not a valid image! Unable to upload!";
                        return View("Index", product);
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
                        return View("Index", product);
                    }
					catch (Exception ex)
					{
						TempData["ImageUploadErrorMessage"] = ("Unable to upload to S3 due to technical issue. Error message: " + ex.Message);
                        return View("Index", product);
                    }

				}

                product.Image = imageUrl;
                _context.ProductTable.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction("ProductList");
            }
            return View("Index", product);
        }

        // View Product
        public async Task<IActionResult> ProductList(string? msg)
        {
            string cloudfrontdomain = "https://d3svoy2tv8bd04.cloudfront.net/";
            //Get user list from db table
            List<Product> productList
                = await _context.ProductTable.ToListAsync();
            
            foreach(var product in productList)
            {
                if(product.Image != null)
                {
                    string imageUrl = product.Image;
                    var uri = new Uri(imageUrl);
                    var imageKey = uri.PathAndQuery.TrimStart('/');
                    product.Image = cloudfrontdomain + imageKey;
                }  
            }                     

            return View(productList);
        }

        // Delete Product
        public async Task<IActionResult> deletepage(int? productid)
        {
            if (productid == null)
            {
                return BadRequest("Error: Product ID is not exists!");
            }

            var product = await _context.ProductTable.FindAsync(productid);

            if (product == null)
            {
                return BadRequest("Error: Not Product in the database!");
            }

            if(product.Image != null)
            {
                string imageUrl = product.Image;
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

            _context.ProductTable.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("ProductList", new
            {
                msg = "Product with id" + productid
                + "is removed from database!"
            });
        }

        //Edit Product
        public async Task<IActionResult> editpage(int? productid)
        {
            //Check id
            if (productid == null)
            {
                return NotFound();
            }

            var product = await _context.ProductTable.FindAsync(productid);

            //Check product
            if (product == null)
            {
                return NotFound();
            }

            //Redirect to edit page
            return View(product);
        }

        // Update Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> updatepage(Product product, List<IFormFile> imagefiles)
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
                        return BadRequest("It is an empty file. Unable to upload!");
                    }
                    else if (image.Length > 1048576) //not more than 1MB
                    {
                        return BadRequest("It is over 1MB limit of size. Unable to upload!");
                    }
                    else if (image.ContentType.ToLower() != "image/png" &&
                        image.ContentType.ToLower() != "image/jpeg"
                        && image.ContentType.ToLower() != "image/gif") //not valid image reject also
                    {
                        return BadRequest("It is not a valid image! Unable to upload!");
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
                        return BadRequest("Unable to upload to S3 due to technical issue. Error message: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest("Unable to upload to S3 due to technical issue. Error message: " + ex.Message);
                    }

                }

                product.Image = imageUrl;

                var existingProduct = await _context.ProductTable.FindAsync(product.ProductId);

                if (existingProduct != null)
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    if(product.Image != null)
                    {
                        existingProduct.Image = product.Image;
                    }                    

                    _context.ProductTable.Update(existingProduct);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ProductList");                    
                }
            }

            return View("editpage", product);
        }

        //Search Product
        public IActionResult Search(string searchString)
        {
            if (searchString != null)
            {
                List<Product> products = _context.ProductTable
                .Where(u => u.Name.Contains(searchString) || u.Description.Contains(searchString))
                .ToList();

                return View("ProductList", products);
            }

            List<Product> allproducts = _context.ProductTable.ToList();
            return View("ProductList", allproducts);
        }

        public async Task<IActionResult> ViewProduct()
        {
            string cloudfrontdomain = "https://d3svoy2tv8bd04.cloudfront.net/";
            //Get product list from db table
            List<Product> productList
                = await _context.ProductTable.ToListAsync();

            foreach (var product in productList)
            {
                string imageUrl = product.Image;
                var uri = new Uri(imageUrl);
                var imageKey = uri.PathAndQuery.TrimStart('/');
                product.Image = cloudfrontdomain + imageKey;
            }

            return View(productList);
        }

        //Upload Image
    }
}
