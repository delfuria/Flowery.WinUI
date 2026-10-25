using Flowery.Controls.Scheduler.Contracts;
using Windows.ApplicationModel.DataTransfer;

namespace Flowery.Controls.Scheduler.Utilities;

/// <summary>In-process drag data for dropping an external item onto the scheduler.</summary>
/// <remarks>A <see cref="DataPackage"/> property carries only a key. The item stays in this process until the
/// drag operation completes or the package is destroyed.</remarks>
public static class ExternalDropHandler
{
    public const string Format = "Flowery.Scheduler.Appointment";

    private static readonly Dictionary<string, ISchedulerDroppable> Items = new();

    /// <summary>Adds <paramref name="appointment"/> to a drag package, for example in a DragStarting handler.
    /// Adds <see cref="DataPackageOperation.Copy"/> to the requested operations, which the scheduler accepts.</summary>
    public static void SetData(DataPackage package, ISchedulerDroppable appointment)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(appointment);
        var token = Guid.NewGuid().ToString("N");
        lock (Items)
            Items[token] = appointment;
        package.Properties[Format] = token;
        package.RequestedOperation |= DataPackageOperation.Copy;
        // The handlers capture the token on purpose: it is the key the package carries.
        package.OperationCompleted += (_, _) => Release(token);
        package.Destroyed += (_, _) => Release(token);
    }

    /// <summary>Returns the item stored by <see cref="SetData"/>, or null for other drag data.</summary>
    public static ISchedulerDroppable? TryGetItem(DataPackageView? data)
    {
        if (data?.Properties is not { } properties || !properties.TryGetValue(Format, out var value) || value is not string token)
            return null;
        lock (Items)
            return Items.GetValueOrDefault(token);
    }

    private static void Release(string token)
    {
        lock (Items)
            Items.Remove(token);
    }
}
