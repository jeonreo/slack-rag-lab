namespace SlackRag.Api.Batch;

public static class BatchArgsParser
{
    public static BatchArgs Parse(string[] args)
    {
        string? channel = null;
        int windowHours = 24;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--channel" && i + 1 < args.Length) channel = args[i + 1];
            if (args[i] == "--windowHours" && i + 1 < args.Length && int.TryParse(args[i + 1], out var w)) windowHours = w;
            if (args[i] == "--dryRun" && i + 1 < args.Length && bool.TryParse(args[i + 1], out var d)) dryRun = d;
        }

        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Missing required --channel");

        return new BatchArgs(channel, windowHours, dryRun);
    }
}
