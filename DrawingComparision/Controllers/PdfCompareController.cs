using Microsoft.AspNetCore.Mvc;
using DrawingComparision.Services;
using System.Drawing;
using System.Drawing.Imaging;

namespace DrawingComparision.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PdfCompareController : Controller
    {
        private readonly PdfComparatorService _comparator;

        public PdfCompareController(PdfComparatorService comparator)
        {
            _comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
        }

        [HttpPost("compare")]
        public IActionResult Compare([FromForm] IFormFile pdf1, [FromForm] IFormFile pdf2)
        {
            if (pdf1 == null || pdf2 == null)
                return BadRequest("Two PDF files required.");

            // Save temp files
            string tmpFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmpFolder);

            string file1 = Path.Combine(tmpFolder, "pdf1.pdf");
            string file2 = Path.Combine(tmpFolder, "pdf2.pdf");

            using (var stream = new FileStream(file1, FileMode.Create)) pdf1.CopyTo(stream);
            using (var stream = new FileStream(file2, FileMode.Create)) pdf2.CopyTo(stream);

            Bitmap originalPage = _comparator.ExtractPdfPageAsBitmap(file1, 0);
            Bitmap modifiedPage = _comparator.ExtractPdfPageAsBitmap(file2, 0);

            int w = Math.Min(originalPage.Width, modifiedPage.Width);
            int h = Math.Min(originalPage.Height, modifiedPage.Height);
            originalPage = _comparator.ResizeToSameSize(originalPage, w, h);
            modifiedPage = _comparator.ResizeToSameSize(modifiedPage, w, h);

            Bitmap diff = _comparator.CompareAndHighlightImproved(originalPage, modifiedPage);

            // Convert Bitmap to byte[]
            using (var ms = new MemoryStream())
            {
                diff.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return File(ms.ToArray(), "image/png"); // return as image
            }
        }
    }
}
