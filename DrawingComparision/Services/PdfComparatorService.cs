using PdfiumViewer;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DrawingComparision.Services
{
    public class PdfComparatorService
    {
        public Bitmap ExtractPdfPageAsBitmap(string pdfPath, int pageIndex)
        {
            using (var document = PdfDocument.Load(pdfPath))
            {
                var size = document.PageSizes[pageIndex];
                int width = (int)size.Width;
                int height = (int)size.Height;

                Image img = document.Render(
                        pageIndex,
                       width,
                      height,
                      300,
                       300,
                   PdfRenderFlags.Annotations
         );

                return new Bitmap( img );
            }
        }

        // Resize bitmap
        public Bitmap ResizeToSameSize(Bitmap image, int width, int height)
        {
            var resized = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, 0, 0, width, height);
            }
            return resized;
        }

        // Main improved comparison with clustering
        public Bitmap CompareAndHighlightImproved(Bitmap img1, Bitmap img2, int threshold = 25, int clusterDistance = 15)
        {
            if (img1 == null || img2 == null)
                throw new ArgumentNullException("Images cannot be null.");

            if (img1.Width != img2.Width || img1.Height != img2.Height)
            {
                int w = Math.Min(img1.Width, img2.Width);
                int h = Math.Min(img1.Height, img2.Height);

                img1 = ResizeToSameSize(img1, w, h);
                img2 = ResizeToSameSize(img2, w, h);
            }

            var diffAreas = DetectDifferenceAreas(img1, img2, threshold);
            var grouped = GroupRectangles(diffAreas, clusterDistance);

            Bitmap output = new Bitmap(img2);

            using (Graphics g = Graphics.FromImage(output))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Pen pen = new Pen(Color.Yellow, 4))
                {
                    foreach (var area in grouped)
                    {
                        g.DrawEllipse(pen, area);
                    }
                }
            }

            return output;
        }

        // Detect raw difference rectangles
        public List<System.Drawing.Rectangle> DetectDifferenceAreas(Bitmap img1, Bitmap img2, int threshold)
        {
            var areas = new List<System.Drawing.Rectangle>();

            for (int y = 0; y < img1.Height; y++)
            {
                for (int x = 0; x < img1.Width; x++)
                {
                    Color c1 = img1.GetPixel(x, y);
                    Color c2 = img2.GetPixel(x, y);

                    int diff =
                        Math.Abs(c1.R - c2.R) +
                        Math.Abs(c1.G - c2.G) +
                        Math.Abs(c1.B - c2.B);

                    if (diff > threshold)
                    {
                        areas.Add(new System.Drawing.Rectangle(x, y, 2, 2));
                    }
                }
            }

            return areas;
        }

        // Group close difference rectangles into clusters
        public List<System.Drawing.Rectangle> GroupRectangles(List<System.Drawing.Rectangle> rects, int mergeDistance)
        {
            if (rects.Count == 0)
                return new List<System.Drawing.Rectangle>();

            List<System.Drawing.Rectangle> grouped = new List<System.Drawing.Rectangle>();
            bool[] visited = new bool[rects.Count];

            for (int i = 0; i < rects.Count; i++)
            {
                if (visited[i]) continue;

                Rectangle cluster = rects[i];
                visited[i] = true;

                bool mergedSomething;

                do
                {
                    mergedSomething = false;
                    for (int j = 0; j < rects.Count; j++)
                    {
                        if (!visited[j])
                        {
                            if (Distance(cluster, rects[j]) < mergeDistance)
                            {
                                cluster = Rectangle.Union(cluster, rects[j]);
                                visited[j] = true;
                                mergedSomething = true;
                            }
                        }
                    }
                } while (mergedSomething);

                grouped.Add(PadRectangle(cluster, 10));
            }

            return grouped;
        }

        private int Distance(Rectangle r1, Rectangle r2)
        {
            int dx = Math.Max(0, Math.Max(r1.Left - r2.Right, r2.Left - r1.Right));
            int dy = Math.Max(0, Math.Max(r1.Top - r2.Bottom, r2.Top - r1.Bottom));
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }

        private Rectangle PadRectangle(Rectangle r, int padding)
        {
            return new Rectangle(
                Math.Max(0, r.Left - padding),
                Math.Max(0, r.Top - padding),
                r.Width + padding * 2,
                r.Height + padding * 2);
        }
    }
}
