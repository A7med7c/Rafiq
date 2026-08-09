namespace Rafiq.Application.Common.Interfaces;

public interface IPdfPageRenderer
{
    /// <summary>
    /// Renders the first page of a PDF as JPEG bytes.
    /// Returns null if the file is not a PDF or conversion fails.
    /// </summary>
    byte[]? RenderFirstPageAsJpeg(byte[] pdfBytes);
}
