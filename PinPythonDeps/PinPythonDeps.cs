using System.Diagnostics;
using System.Text;

static bool TryGetPipFreezeOutput(out string? output)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "pip",
        Arguments = "freeze",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    using var process = new Process { StartInfo = startInfo };

    try
    {
        process.Start();

        output = process.StandardOutput.ReadToEnd();

        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Pip exited with code {process.ExitCode}: {error}");
        }

        return true;
    }

    catch // (Exception ex)
    {
        output = null;

        return false;
    }
}

static async Task<(bool success, string[]? contents)> TryLoadExistingRequirements()
{
    try
    {
        var existingRequirements = await File.ReadAllLinesAsync("requirements.txt");

        return (success: true, contents: existingRequirements);
    }

    catch (FileNotFoundException)
    {
        return (success: false, contents: null);
    }
}

if (!TryGetPipFreezeOutput(out var pipFreezeText))
{
    Console.Error.WriteLine("Failed to get pip freeze output.");

    return;
}

var (requirementsLoaded, existingRequirements) = await TryLoadExistingRequirements();

if (!requirementsLoaded)
{
    Console.Error.WriteLine("No existing requirements.txt found!");

    return;
}

var foundPackages = new HashSet<string>(existingRequirements!.Length);

foreach (var existingRequirement in existingRequirements)
{
    // Split the package name and version ( Handle ==, >=, <= etc )

    var separatorIndex = existingRequirement.IndexOfAny([ '=', '>', '<', '!', '~' ]);

    var packageName = existingRequirement;

    if (separatorIndex != -1)
    {
        packageName = existingRequirement[..separatorIndex].Trim();
    }

    foundPackages.Add(packageName);
}

var pinnedRequirementsText = new StringBuilder();

foreach (var currentPipFreezeRequirement in pipFreezeText!.Split([ '\r', '\n' ], StringSplitOptions.RemoveEmptyEntries))
{
    var splitIndex = currentPipFreezeRequirement.IndexOf("==", StringComparison.Ordinal);

    if (splitIndex == -1)
    {
        // We don't know how to handle this line, skip it
        continue;
    }

    var packageName = currentPipFreezeRequirement[..splitIndex].Trim();

    if (!foundPackages.Contains(packageName))
    {
        // The package isn't listed in requirements.txt, so skip it
        continue;
    }

    pinnedRequirementsText.AppendLine(currentPipFreezeRequirement);
}

const string PINNED_REQUIREMENTS_FILE_NAME = "requirements-pinned.txt";

await File.WriteAllTextAsync(PINNED_REQUIREMENTS_FILE_NAME, pinnedRequirementsText.ToString());

Console.WriteLine($"Pinned requirements written to {PINNED_REQUIREMENTS_FILE_NAME} !");