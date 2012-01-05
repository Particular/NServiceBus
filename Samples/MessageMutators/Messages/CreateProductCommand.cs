using System;
using System.ComponentModel.DataAnnotations;
using NServiceBus;

namespace Messages
{
    public class CreateProductCommand : ICommand
    {
        [Required]
        public string ProductId { get; set; }

        [StringLength(20, ErrorMessage = "The Product Name value cannot exceed 20 characters. ")]
        public string ProductName { get; set; }

        [Range(1, 5)]
        public decimal ListPrice { get; set; }

        [Range(typeof(DateTime), "1/1/2012", "15/1/2012", ErrorMessage = "Value for {0} must be between {1} and {2}")]
        public DateTime SellEndDate { get; set; }

        public byte[] Image { get; set; }
        public override string ToString()
        {
            return string.Format(
                "CreateProductCommand: ProductId={0}, ProductName={1}, ListPrice={2} SellEndDate={3} Image (length)={4}",
                ProductId, ProductName, ListPrice, SellEndDate, (Image == null ? 0: Image.Length));
        }
    }
}
