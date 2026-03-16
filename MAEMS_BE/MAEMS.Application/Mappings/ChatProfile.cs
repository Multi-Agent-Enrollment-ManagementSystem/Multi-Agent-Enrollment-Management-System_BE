using AutoMapper;
using MAEMS.Application.DTOs.Chat;

namespace MAEMS.Application.Mappings;

/// <summary>
/// AutoMapper profile cho Chat DTOs
/// Note: Mapping từ Infrastructure entities được thực hiện trong Handler
/// </summary>
public class ChatProfile : Profile
{
    public ChatProfile()
    {
        // Mappings sẽ được thực hiện từ Infrastructure model trong QueryHandler
        // không cần cấu hình ở đây để tránh phụ thuộc vào Infrastructure
    }
}
