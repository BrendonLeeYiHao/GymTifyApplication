using System.ComponentModel.DataAnnotations;

namespace GymFitApplication.Models
{
    public class Feedback
    {
        [Key] //define primary
        public int FeedbackId { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "The {0} must be a least {2} and at max {1} characters long.", MinimumLength = 3)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Message")]
        public string Message { get; set; }
    }
}
