using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioTracker.Models
{
    internal class PortfolioItem
    {
        public string Name { get; set; }
        public decimal CurrItemPrice { get; set; }
        public decimal MyPrice { get; set; }
        public decimal FinalPrice { get; set; }
    }
}
