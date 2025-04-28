namespace CP2496H07Group1.Dtos
{
    public class PurchaseVipRequest
    {
        public long VipId { get; set; }
        public long AccountId { get; set; }
        public int Pin { get; set; }
        public long? DiscountId { get; }
    }
}