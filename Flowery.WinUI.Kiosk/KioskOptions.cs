namespace Flowery.WinUI.Kiosk;

internal sealed record KioskOptions(bool Manual, string? SmokeReport)
{
    public static KioskOptions Parse(string[] arguments)
    {
        var manual = false;
        string? report = null;
        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--manual":
                    manual = true;
                    break;
                case "--smoke-test":
                    if (++index >= arguments.Length || !System.IO.Path.IsPathFullyQualified(arguments[index]))
                        throw new ArgumentException("--smoke-test requires an absolute XML report path.");
                    report = System.IO.Path.GetFullPath(arguments[index]);
                    manual = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown kiosk option '{arguments[index]}'.");
            }
        }
        return new KioskOptions(manual, report);
    }
}
