using Microsoft.AspNetCore.Mvc;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GymFitApplication.Controllers
{
    public class AnnouncementController : Controller
    {
        private const string announcementTopicARN = "arn:aws:sns:us-east-1:717071075544:SNSAnnouncementNotification";

        //function 1: to get back the amazon account keys
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

        public IActionResult AnnouncementPage(string msg)
        {
            ViewBag.msg = msg;
            return View();
        }


        public async Task<IActionResult> processBroadcast(string subjecttitle, string msgBody)
        {
            if (subjecttitle == null || msgBody == null)
            {
                return RedirectToAction(nameof(AnnouncementPage), new { msg = "Error" });
            }

            List<string> keys = getKeys();
            AmazonSimpleNotificationServiceClient agent = new AmazonSimpleNotificationServiceClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            try
            {
                PublishRequest request = new PublishRequest
                {
                    TopicArn = announcementTopicARN,
                    Subject = subjecttitle,
                    Message = msgBody
                };

                await agent.PublishAsync(request);
                return RedirectToAction(nameof(AnnouncementPage), new { msg = "Message is successfully broadcasted!" });
            }
            catch (AmazonSimpleNotificationServiceException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
