using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HuginTest.Model
{
    public class SaleItem
    {
        public bool IsSpecialPrice { get; internal set; }
        public bool IsWeighable { get; internal set; }
        public int PluId { get; internal set; }
        public decimal Quantity { get; internal set; }
        public bool SaveAndSale { get; internal set; }
        public decimal SpecialPrice { get; internal set; }
        public string ProductName { get; set; }
        public string Barcode { get; internal set; }
        public int Dept { get; internal set; }
    }
}
