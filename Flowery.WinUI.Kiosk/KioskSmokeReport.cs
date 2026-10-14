using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Flowery.WinUI.Kiosk;

internal sealed class KioskSmokeReport(string path)
{
    private readonly List<KioskSmokeResult> _results = [];
    internal string Path { get; } = System.IO.Path.GetFullPath(path);
    internal int Failed => _results.Count(result => !result.Passed);

    internal void Add(string name, bool passed, string message)
    {
        var uniqueName = name;
        for (var suffix = 2; _results.Any(result => result.Name == uniqueName); suffix++)
            uniqueName = $"{name}.{suffix}";
        _results.Add(new KioskSmokeResult(uniqueName, passed, message));
    }

    internal void Save()
    {
        var document = new XDocument(new XElement("tests",
            new XAttribute("total", _results.Count),
            new XAttribute("passed", _results.Count - Failed),
            new XAttribute("failed", Failed),
            _results.Select(result => new XElement("test",
                new XAttribute("name", result.Name),
                new XAttribute("outcome", result.Passed ? "Passed" : "Failed"),
                new XElement("message", result.Message)))));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var pendingPath = Path + ".writing";
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, CloseOutput = false };
        using (var stream = new FileStream(pendingPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var writer = XmlWriter.Create(stream, settings))
                document.Save(writer);
            stream.Flush(flushToDisk: true);
        }
        File.Move(pendingPath, Path, overwrite: true);
        var saved = XDocument.Load(Path).Root ?? throw new InvalidDataException("The kiosk report has no root element.");
        if (saved.Name != "tests" || (int?)saved.Attribute("total") != _results.Count ||
            (int?)saved.Attribute("failed") != Failed || saved.Elements("test").Count() != _results.Count)
            throw new InvalidDataException("The published kiosk report failed readback validation.");
        File.WriteAllLines(Path + ".log", _results.Select(result =>
            $"{(result.Passed ? "Passed" : "Failed")} {result.Name}: {result.Message}"), new UTF8Encoding(false));
    }

    private sealed record KioskSmokeResult(string Name, bool Passed, string Message);
}
