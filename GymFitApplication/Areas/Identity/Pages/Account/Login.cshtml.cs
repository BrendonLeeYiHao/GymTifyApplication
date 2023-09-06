// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using GymFitApplication.Areas.Identity.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GymFitApplication.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<GymFitApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<GymFitApplicationUser> _userManager;

        public LoginModel(SignInManager<GymFitApplicationUser> signInManager, ILogger<LoginModel> logger, UserManager<GymFitApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
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
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

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
            [Required(ErrorMessage = "The Username field is required.")]
            //[EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(Input.Email);

                    var subscriptionMsg = await GetSubscriptionIdByEmailAndTopicArn(user.Email, announcementTopicARN);

                    if (subscriptionMsg == "Unconfirmed Subscription")
                    {
                        ModelState.AddModelError(string.Empty, "Please check your email to confirm subscription in order to login!");
                        await _signInManager.SignOutAsync();
                        return Page();
                    }
                    else
                    {
                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl);
                    }
                }
                //if (result.RequiresTwoFactor)
                //{
                //   return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                //}
                //if (result.IsLockedOut)
                //{
                //   _logger.LogWarning("User account locked out.");
                //    return RedirectToPage("./Lockout");
                //}
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
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


        public async Task<string> GetSubscriptionIdByEmailAndTopicArn(string email, string topicArn)
        {
            List<string> keys = getKeys();
            AmazonSimpleNotificationServiceClient agent = new AmazonSimpleNotificationServiceClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            ListSubscriptionsByTopicRequest request = new ListSubscriptionsByTopicRequest
            {
                TopicArn = topicArn
            };

            ListSubscriptionsByTopicResponse response = await agent.ListSubscriptionsByTopicAsync(request);

            foreach(var subscription in response.Subscriptions)
            {
                if(string.Equals(subscription.Endpoint, email))
                {
                    _logger.LogInformation("Print endpoint");
                    if(string.Equals(subscription.SubscriptionArn, "PendingConfirmation"))
                    {
                        _logger.LogInformation("Subscription is pending confirmation");
                        return "Unconfirmed Subscription";
                    }
                    var subscriptionId = GetSubscriptionIdFromArn(subscription.SubscriptionArn);
                    _logger.LogInformation(subscriptionId);
                    return subscriptionId;
                }
            }
            return null; //Subscription not found for the email and topic ARN
        }

        public string GetSubscriptionIdFromArn(string subscriptionArn)
        {
            // Split the ARN using colon as the separator
            string[] arnParts = subscriptionArn.Split(':');

            // The subscription ID is the last part of the ARN
            string subscriptionId = arnParts[arnParts.Length - 1];

            return subscriptionId;
        }
    }
}
