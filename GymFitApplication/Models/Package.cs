using MessagePack;

namespace GymFitApplication.Models
{
	public class Package
	{
		public int PackageId { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public decimal Price { get; set; }

		public int Duration { get; set; }

		public string UserId { get; set; }
	}
}
