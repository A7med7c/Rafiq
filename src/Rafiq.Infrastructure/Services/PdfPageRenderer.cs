using PDFtoImage;
using Rafiq.Application.Common.Interfaces;
using SkiaSharp;

namespace Rafiq.Infrastructure.Services;

public sealed class PdfPageRenderer : IPdfPageRenderer
{
    public byte[]? RenderFirstPageAsJpeg(byte[] pdfBytes)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var bitmap = Conversion.ToImage(stream, page: 0);
            using var image  = SKImage.FromBitmap(bitmap);
            using var data   = image.Encode(SKEncodedImageFormat.Jpeg, quality: 90);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
