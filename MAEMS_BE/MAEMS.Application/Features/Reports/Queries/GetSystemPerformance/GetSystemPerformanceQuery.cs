using MAEMS.Application.DTOs.SystemMonitor;
using MAEMS.Domain.Common;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace MAEMS.Application.Features.Reports.Queries.GetSystemPerformance;

public class GetSystemPerformanceQuery : IRequest<BaseResponse<SystemPerformanceDto>>
{
}