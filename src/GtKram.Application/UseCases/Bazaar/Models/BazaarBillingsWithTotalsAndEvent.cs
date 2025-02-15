using GtKram.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarBillingsWithTotalsAndEvent(BazaarBillingWithTotals[] Billings, BazaarEvent Event);