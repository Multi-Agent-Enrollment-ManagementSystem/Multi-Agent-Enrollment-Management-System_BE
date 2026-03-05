using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;

public class GetAllFullApplicationsQuery : IRequest<BaseResponse<List<FullApplicationDto>>> { }