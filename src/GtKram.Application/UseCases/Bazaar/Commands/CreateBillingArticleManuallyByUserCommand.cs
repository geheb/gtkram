using GtKram.Domain.Base;
using Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateBillingArticleManuallyByUserCommand(Guid UserId, Guid BillingId, int SellerNumber, int LabelNumber) : ICommand<Result>;
