namespace FF.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public double Amount { get; set; }
        public string SaleDate { get; set; }
    }
}
