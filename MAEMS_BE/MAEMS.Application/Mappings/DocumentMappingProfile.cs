using AutoMapper;
using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        CreateMap<Document, DocumentDto>();
        CreateMap<DocumentDto, Document>();
    }
}