using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

void CreateIcon(string path, int size)
{
    using var bmp = new Bitmap(size, size);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    
    // Fond violet
    using var bgBrush = new SolidBrush(Color.FromArgb(255, 90, 60, 141)); // #5a3c8d
    g.FillRectangle(bgBrush, 0, 0, size, size);
    
    // Lettre T blanche
    float fontSize = size * 0.65f;
    using var font = new Font("Arial", fontSize, FontStyle.Bold);
    using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    g.DrawString("T", font, Brushes.White, new RectangleF(0, 0, size, size), sf);
    
    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
}

// Créer les tailles nécessaires pour un .ico
var sizes = new[] { 16, 32, 48, 256 };
var pngPaths = new List<string>();
foreach (var s in sizes)
{
    var p = $"icon_{s}.png";
    CreateIcon(p, s);
    pngPaths.Add(p);
    Console.WriteLine($"Created {p}");
}

// Construire le .ico manuellement
using var icoStream = new FileStream("Assets/tracepml.ico", FileMode.Create);
using var writer = new BinaryWriter(icoStream);

// Header ICO
writer.Write((short)0);      // reserved
writer.Write((short)1);      // type = ICO
writer.Write((short)sizes.Length);

// Charger les PNG en mémoire
var pngDatas = new List<byte[]>();
foreach (var p in pngPaths)
    pngDatas.Add(File.ReadAllBytes(p));

// Directory entries
int offset = 6 + sizes.Length * 16;
for (int i = 0; i < sizes.Length; i++)
{
    byte w = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
    byte h = w;
    writer.Write(w);
    writer.Write(h);
    writer.Write((byte)0);   // color palette
    writer.Write((byte)0);   // reserved
    writer.Write((short)1);  // color planes
    writer.Write((short)32); // bits per pixel
    writer.Write(pngDatas[i].Length);
    writer.Write(offset);
    offset += pngDatas[i].Length;
}

// Image data
foreach (var data in pngDatas)
    writer.Write(data);

// Cleanup
foreach (var p in pngPaths) File.Delete(p);
Console.WriteLine("Created Assets/tracepml.ico");
