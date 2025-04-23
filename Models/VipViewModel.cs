namespace CP2496H07Group1.Models
{
	public class VipViewModel
	{
		public List<Vip> Vips { get; set; } = new();
		public int PageNumber { get; set; }
		public int PageCount { get; set; }
	}
}