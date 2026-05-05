using AutoMapper;
using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;

namespace MAEMS.Application.Features.TuitionFees.Queries.GetAllActiveTuitionFees;

public class GetAllActiveTuitionFeesQueryHandler : IRequestHandler<GetAllActiveTuitionFeesQuery, BaseResponse<IEnumerable<TuitionFeeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllActiveTuitionFeesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<TuitionFeeDto>>> Handle(GetAllActiveTuitionFeesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var fees = await _unitOfWork.TuitionFees.GetActiveFeesAsync();
            var dtos = _mapper.Map<IEnumerable<TuitionFeeDto>>(fees);

            return new BaseResponse<IEnumerable<TuitionFeeDto>>
            {
                Success = true,
                Message = "Lấy danh sách học phí thành công",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse<IEnumerable<TuitionFeeDto>>
            {
                Success = false,
                Message = $"Lỗi khi lấy danh sách học phí: {ex.Message}"
            };
        }
    }
}
