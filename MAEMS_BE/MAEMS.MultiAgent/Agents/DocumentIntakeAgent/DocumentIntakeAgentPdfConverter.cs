using Microsoft.Extensions.Logging;
using PDFtoImage;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Chịu trách nhiệm convert PDF → danh sách PNG base64 để gửi vào Ollama images[].
/// </summary>
internal sealed class DocumentIntakeAgentPdfConverter
{
    /// <summary>Số trang PDF tối đa được render.</summary>
    private const int MaxPdfPages = 3;

    /// <summary>DPI khi render PDF → PNG (150 DPI đủ rõ, payload không quá lớn).</summary>
    private const int PdfRenderDpi = 150;

    private readonly ILogger _logger;

    internal DocumentIntakeAgentPdfConverter(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Render tối đa <see cref="MaxPdfPages"/> trang đầu của PDF thành PNG base64
    /// dùng PDFtoImage v4 (Pdfium).
    /// </summary>
    /// <param name="pdfBytes">Nội dung file PDF dưới dạng byte array.</param>
    /// <param name="fileName">Tên file — dùng cho logging.</param>
    /// <returns>Danh sách base64 PNG, mỗi phần tử là một trang.</returns>
    internal List<string> Convert(byte[] pdfBytes, string fileName)
    {
        var totalPages = Conversion.GetPageCount(pdfBytes);
        var pagesToRender = Math.Min(totalPages, MaxPdfPages);

        _logger.LogInformation(
            "PdfConverter: '{FileName}' has {Total} page(s), rendering {Render} at {Dpi} DPI",
            fileName, totalPages, pagesToRender, PdfRenderDpi);

        var renderOptions = new RenderOptions(Dpi: PdfRenderDpi);
        var result = new List<string>(pagesToRender);

        for (var i = 0; i < pagesToRender; i++)
        {
            using var pngStream = new MemoryStream();

            Conversion.SavePng(
                imageStream: pngStream,
                pdfAsByteArray: pdfBytes,
                page: i,
                password: null,
                options: renderOptions);

            result.Add(System.Convert.ToBase64String(pngStream.ToArray()));
        }

        return result;
    }
}
