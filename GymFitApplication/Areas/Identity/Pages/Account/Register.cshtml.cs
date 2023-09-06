// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using GymFitApplication.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using GymFitApplication.Data;
using GymFitApplication.Models;

//S3 Imports
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.EntityFrameworkCore;


namespace GymFitApplication.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<GymFitApplicationUser> _signInManager;
        private readonly UserManager<GymFitApplicationUser> _userManager;
        private readonly IUserStore<GymFitApplicationUser> _userStore;
        private readonly IUserEmailStore<GymFitApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly GymFitApplicationContext _context;

        public RegisterModel(
            UserManager<GymFitApplicationUser> userManager,
            IUserStore<GymFitApplicationUser> userStore,
            SignInManager<GymFitApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            GymFitApplicationContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [StringLength(20, ErrorMessage = "The {0} must be a least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [RegularExpression(@"^[0][1][0-1]-[0-9]{8}$|[0][1][2-9]-[0-9]{7}$", ErrorMessage = "Invalid Phone Number. Please follow Malaysian Phone Number Format: Ex.012-9402321 / 011-19301123")]
            [Display(Name = "Phone Number")]
            public string Phonenumber { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime Dateofbirth { get; set; }

            [Required]
            [Display(Name = "Gender")]
            public string Gender { get; set; }
            
            [Display(Name = "Role Name")]
            public string Rolename { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                user.PhoneNumber = Input.Phonenumber;
                user.Dateofbirth = Input.Dateofbirth;
                user.Gender = Input.Gender;

                if (string.IsNullOrEmpty(Input.Rolename))
                {
                    user.Rolename = "Customer";
                }
                else
                {
                    user.Rolename = Input.Rolename;
                }
                user.EmailConfirmed = true;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    //specificARNIdentifier for user
                    processSubscription(Input.Email);

                    _logger.LogInformation("User created a new account with password.");

                    ViewData["validRegistration"] = "True";

                    if (string.IsNullOrEmpty(Input.Rolename))
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                    }
                    else if (Input.Rolename == "Tutor")
                    {
                        await _userManager.AddToRoleAsync(user, Input.Rolename);
                        Tutor tutor = new Tutor();
                        tutor.Id = user.Id;
                        tutor.Name = Input.Username;
                        _context.TutorTable.Add(tutor); 
                        await _context.SaveChangesAsync();
                    }
                    //var userId = await _userManager.GetUserIdAsync(user);
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    //var callbackUrl = Url.Page(
                    //    "/Account/ConfirmEmail",
                    //    pageHandler: null,
                    //    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    //    protocol: Request.Scheme);

                    //await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    //    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (User.IsInRole("Admin"))
                    {
                        return RedirectToPage("/Index");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        //return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        return RedirectToPage("Login");
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private GymFitApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<GymFitApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(GymFitApplicationUser)}'. " +
                    $"Ensure that '{nameof(GymFitApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<GymFitApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<GymFitApplicationUser>)_userStore;
        }

        private const string announcementTopicARN = "arn:aws:sns:us-east-1:717071075544:SNSAnnouncementNotification";

        //function: get back the amazon account keys
        private List<string> getKeys()
        {
            //create an empty list
            List<string> keys = new List<string>();

            //link to appsettings.json and get back the values
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfiguration configure = builder.Build(); //build the json file

            //no error
            keys.Add(configure["Values:key1"]); //get key 1
            keys.Add(configure["Values:key2"]); //get key 2
            keys.Add(configure["Values:key3"]); //get key 3

            return keys;
        }

        //function 3: process the subscription action
        public async void processSubscription(string email) //email is from the name attribute of type email
        {
            List<string> keys = getKeys();
            AmazonSimpleNotificationServiceClient agent = new AmazonSimpleNotificationServiceClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            try
            {
                SubscribeRequest request = new SubscribeRequest
                {
                    TopicArn = announcementTopicARN,
                    Endpoint = email,
                    Protocol = "email"
                };

                SubscribeResponse response = await agent.SubscribeAsync(request);
                ViewData["registerID"] = response.ResponseMetadata.RequestId;
                //ViewBag.registerID = response.ResponseMetadata.RequestId;
            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                _logger.LogError(ex.Message);
            }
        }

    }
}
