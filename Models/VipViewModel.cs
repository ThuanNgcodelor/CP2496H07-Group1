namespace CP2496H07Group1.Models
{
	public class VipViewModel
	{
		public List<Vip> Vips { get; set; } = new();
		public int PageNumber { get; set; }
		public int PageCount { get; set; }
	}
	public class DiscountcodeViewModel
	{

		public long Id { get; set; }
		public required string DiscountCodes { get; set; }
		public required int Points { get; set; }
		public required int Percent { get; set; }
		public required int LongDate { get; set; }
		public List<DiscountcodeViewModel> Accounts { get; set; } = new();
		public long? SelectedDiscountCodeId { get; set; }
	}
}