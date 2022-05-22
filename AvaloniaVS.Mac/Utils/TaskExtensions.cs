namespace AvaloniaVS.Mac.Utils;

internal static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                MonoDevelop.Core.LoggingService.LogError("Exception caught by FireAndForget", t.Exception);
            }
        }, TaskScheduler.Default);
    }
}
