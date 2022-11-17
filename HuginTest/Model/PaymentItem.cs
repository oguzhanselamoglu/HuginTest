using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HuginTest.Model
{
    public class PaymentItem
    {
        public decimal Amount { get; internal set; }
        public int PaymentIndex { get; internal set; }
        public string PaymentType { get; internal set; }
        public int SubPaymentIndex { get; internal set; }
    }
}
