using AutoMapper;
using MAEMS.Application.DTOs.AgentLog;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class AgentLogProfile : Profile
{
    public AgentLogProfile()
    {
        CreateMap<AgentLog, AgentLogDto>();
    }
}
