using AutoMapper;
using MAEMS.Application.DTOs.LlmChatLog;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class LlmChatLogProfile : Profile
{
    public LlmChatLogProfile()
    {
        CreateMap<LlmChatLog, LlmChatLogDto>();
    }
}
