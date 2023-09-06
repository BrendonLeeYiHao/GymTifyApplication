using GymFitApplication.Data;
using GymFitApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using GymFitApplication.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;


//7 Aguust 2023
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace GymFitApplication.Controllers
{
    public class BookingController : Controller
    {
        public readonly GymFitApplicationContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly UserManager<GymFitApplicationUser> _userManager;

        public BookingController(GymFitApplicationContext context, ILogger<BookingController> logger, UserManager<GymFitApplicationUser> userManager)
        {
            _context = context; //database
            _logger = logger;
            _userManager = userManager;
        }

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

        private const string QueueName = "SQSBookingQueue.fifo";

        public async Task<IActionResult> MakeBookingPage()
        {
            //7 August 2023
            List<string> keys = getKeys();
            AmazonSQSClient agent = new AmazonSQSClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            //get the queue url from the SQS
            GetQueueUrlResponse response = await agent.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = QueueName });

            //get the user amount in the queue
            GetQueueAttributesRequest request = new GetQueueAttributesRequest
            {
                QueueUrl = response.QueueUrl
            };

            request.AttributeNames.Add("ApproximateNumberOfMessages");
            GetQueueAttributesResponse response1 = await agent.GetQueueAttributesAsync(request);
            ViewBag.MessageCount = response1.ApproximateNumberOfMessages;
            //////////////////////

            List<string> disabledDates = await GetDisabledDates();
            ViewBag.DisabledDates = disabledDates;
           
            string username = User.Identity.Name;

            var viewModel = new Booking
            {
                UserName = username,
                BookingDate = DateTime.Now.Date
            };
            return View(viewModel);
        }


        //might remove at a later time
        public async Task <List<string>> GetDisabledDates()
        {
            var disabledDates = new List<string>();

            List<Booking> bookings = await _context.BookingTable.Where(b => b.UserName == User.Identity.Name).ToListAsync();

            foreach (Booking booking in bookings)
            {
                _logger.LogInformation(booking.BookingDate.ToString());
                disabledDates.Add(booking.BookingDate.ToString());
            }
            return disabledDates;
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //prevent cross-site attack
        public async Task<IActionResult> MakeBookingPage(Booking booking) //insert data function
        {
            if (ModelState.IsValid)
            {
                List<string> keys = getKeys();
                AmazonSQSClient agent = new AmazonSQSClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

                //get the queue url from the SQS
                GetQueueUrlResponse response = await agent.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = QueueName });

                try
                {
                    string messageGroupId = booking.BookingDate.ToString("MM/dd/yyyy");

                    SendMessageRequest request = new SendMessageRequest
                    {
                        QueueUrl = response.QueueUrl,
                        MessageBody = JsonConvert.SerializeObject(booking),
                        MessageGroupId = messageGroupId
                    };
                    await agent.SendMessageAsync(request);
                    TempData["StatusMessage"] = "Booking request is under approval!";
                    return RedirectToAction("MakeBookingPage");
                }
                catch (AmazonSQSException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return View(booking);
        }


        //function 4: read the message back from the SQS
        public async Task<IActionResult> ViewBookingRequestPage()
        {
            List<string> keys = getKeys();
            AmazonSQSClient agent = new AmazonSQSClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            //get the queue url from the SQS
            GetQueueUrlResponse response = await agent.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = QueueName });

            //create list to collect all messages from the queue
            List<KeyValuePair<Booking, string>> messagelist = new List<KeyValuePair<Booking, string>>();

            try
            {
                ReceiveMessageRequest request = new ReceiveMessageRequest
                {
                    QueueUrl = response.QueueUrl,
                    MaxNumberOfMessages = 10,
                    VisibilityTimeout = 30,
                    WaitTimeSeconds = 10
                };
                ReceiveMessageResponse response1 = await agent.ReceiveMessageAsync(request);

                if (response1.Messages.Count <= 0)
                {
                    ViewBag.checkData = "Empty";
                    return View();
                }
                else
                {
                    ViewBag.checkData = "Not Empty";
                }

                for (int i = 0; i < response1.Messages.Count; i++)
                {
                    //how to convert json writing in message to an object item
                    Booking bookingitem = JsonConvert.DeserializeObject<Booking>(response1.Messages[i].Body);
                    string deleteId = response1.Messages[i].ReceiptHandle; ; //use for delete message purpose
                    messagelist.Add(new KeyValuePair<Booking, string>(bookingitem, deleteId));
                }
            }
            catch (AmazonSQSException ex)
            {
                return BadRequest(ex.Message);
            }
            return View(messagelist);
        }


        //function 5: delete message from queue after approve / reject
        public async Task<IActionResult> deleteMsgFromQ(string username, DateTime bookingdate, string starttime, string duration, string deleteid, string word)
        {
            _logger.LogInformation("Username: " + username + " , " + "Booking Date: " + bookingdate + "Start Time: " + starttime + "Duration: " + duration);

            _logger.LogInformation("Delete ID: " + deleteid);
            _logger.LogInformation("Word: " + word);
            List<string> keys = getKeys();
            AmazonSQSClient agent = new AmazonSQSClient(keys[0], keys[1], keys[2], RegionEndpoint.USEast1);

            //get the queue url from the SQS
            GetQueueUrlResponse response = await agent.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = QueueName });

            //string nextstepmsg = "";

            Booking booking = new Booking
            {
                UserName = username,
                BookingDate = bookingdate,
                StartTime = starttime,
                Duration = duration
            };

            var user = await _userManager.FindByNameAsync(username);

            //delete message after the smtp action
            try
            {
                DeleteMessageRequest request = new DeleteMessageRequest
                {
                    QueueUrl = response.QueueUrl,
                    ReceiptHandle = deleteid
                };
                await agent.DeleteMessageAsync(request);
            }
            catch (AmazonSQSException ex)
            {
                return BadRequest(ex.Message);
            }

            if (word == "Approve")
            {
                _context.BookingTable.Add(booking);
                await _context.SaveChangesAsync();  //commit changes and save to database
                SendEmail(booking, user.Email, "Approve");
                TempData["StatusMessage"] = "Booking is Approved!";
                _logger.LogInformation("Approve booking request!");
            }
            else if (word == "Reject")
            {
                SendEmail(booking, user.Email, "Reject");
                TempData["StatusMessage"] = "Booking is Rejected!";
                _logger.LogInformation("Reject booking request!");
            }
            //return Content("Testing here");

            return RedirectToAction("ProcessingRequest");
        }

        public IActionResult ProcessingRequest()
        {
            return View();
        }


        public async Task<IActionResult> ManageBookingHistoryPage(string msg, string SearchString)
        {
            List<Booking> bookingList = await _context.BookingTable.Where(b => b.UserName == User.Identity.Name).ToListAsync();

            if(bookingList.Count == 0)
            {
                ViewBag.empty = "Empty";
                return View();
            }

            ViewBag.empty = "Not Empty";
            
            if (!string.IsNullOrEmpty(SearchString)) //if searchstring not empty
            {
                bookingList = bookingList.Where(s => s.BookingDate.ToString().Contains(SearchString) 
                                                    || s.StartTime.Contains(SearchString) 
                                                    || s.Duration.Contains(SearchString)).ToList();
            }

            ViewBag.msg = msg;

            return View(bookingList);
        }

        public async Task<IActionResult> deletepage(int? bookingId)
        {
            if (bookingId == null)
            {
                return BadRequest("Error: Booking ID is missing!");
            }

            var booking = await _context.BookingTable.FindAsync(bookingId);

            if (booking == null)
            {
                return BadRequest("Error: No booking in the database!");
            }

            _context.BookingTable.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageBookingHistoryPage", new { msg = "Booking Id of " + bookingId + " is deleted!" });
        }

        public void SendEmail(Booking booking, string email, string status)
        {
            string fromMail = "lingken2001@gmail.com";
            string fromPassword = "huzrwocotgkrncxr";

            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromMail);
            message.Subject = "GymTify Booking " + status + " Notification";
            message.To.Add(new MailAddress(email));

            if(status == "Approve")
            {
                message.Body = "<html><body>Hi " + booking.UserName + "<br><br>Your booking on " + booking.BookingDate.ToString("MM/dd/yyyy") + " at " + booking.StartTime + " for " + booking.Duration + " is approved. Please be punctual. Thank you :)</body></html>";
            }
            else
            {
                message.Body = "<html><body>Hi " + booking.UserName + "<br><br>Your booking on " + booking.BookingDate.ToString("MM/dd/yyyy") + " at " + booking.StartTime + " for " + booking.Duration + " is rejected. Kindly contact us for more information. </body></html>";
            }
           
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };
            smtpClient.Send(message);
            
        }
    }
}
