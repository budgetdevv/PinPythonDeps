using System.Diagnostics;
using System.Text;

static bool IsCommentLine(string line)
{
    return line.TrimStart().StartsWith("#");
}

static bool TryGetPipFreezePackageToVersionMap(out Dictionary<string, string> pipFreezePackageToVersionMap)
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

    pipFreezePackageToVersionMap = new(StringComparer.OrdinalIgnoreCase);

    try
    {
        process.Start();

        var pipFreezeText = process.StandardOutput.ReadToEnd();

        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Pip exited with code {process.ExitCode}: {error}");
        }

        foreach (var currentPipFreezeRequirement in pipFreezeText.Split([ '\r', '\n' ], StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsCommentLine(currentPipFreezeRequirement))
            {
                continue;
            }

            var splitResult = currentPipFreezeRequirement.Split("==");

            if (splitResult.Length != 2)
            {
                // We do not know how to handle this...
                continue;
            }

            pipFreezePackageToVersionMap[splitResult[0]] = splitResult[1];
        }

        return true;
    }

    catch // (Exception ex)
    {
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

if (!TryGetPipFreezePackageToVersionMap(out var pipFreezePackageToVersionMap))
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

var pinnedRequirementsText = new StringBuilder();

foreach (var existingRequirement in existingRequirements!)
{
    if (IsCommentLine(existingRequirement))
    {
        continue;
    }

    // Split the package name and version ( Handle ==, >=, <= etc )

    var separatorIndex = existingRequirement.IndexOfAny([ '=', '>', '<', '!', '~' ]);

    var packageName = existingRequirement;

    if (separatorIndex != -1)
    {
        packageName = existingRequirement[..separatorIndex].Trim();
    }

    if (pipFreezePackageToVersionMap.TryGetValue(packageName, out var versionText))
    {
        pinnedRequirementsText.AppendLine($"{packageName}=={versionText}");
    }
}

const string PINNED_REQUIREMENTS_FILE_NAME = "requirements-pinned.txt";

await File.WriteAllTextAsync(PINNED_REQUIREMENTS_FILE_NAME, pinnedRequirementsText.ToString());

Console.WriteLine($"Pinned requirements written to {PINNED_REQUIREMENTS_FILE_NAME} !");