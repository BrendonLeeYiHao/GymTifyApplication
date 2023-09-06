using System.ComponentModel.DataAnnotations;

namespace GymFitApplication.Models
{
    public class Booking
    {
        [Key] //define primary
        public int BookingId { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Booking Date")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public string StartTime { get; set; }

        [Required]
        [Display(Name = "Duration")]
        public string Duration { get; set; }
    }
}
