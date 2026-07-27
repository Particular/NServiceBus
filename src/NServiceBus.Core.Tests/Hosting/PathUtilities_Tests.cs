namespace NServiceBus.Core.Tests.Host;

using NUnit.Framework;

[TestFixture]
public class PathUtilities_Tests
{
    [Test]
    public void Parse_from_path_without_spaces_but_with_quotes()
    {
        var path = PathUtilities.SanitizedPath("\"pathto\\my.exe\" somevar");

        Assert.That(path, Is.EqualTo("pathto\\my.exe"));
    }

    [Test]
    public void Parse_from_path_without_spaces_but_without_quotes()
    {
        var path = PathUtilities.SanitizedPath("pathto\\my.exe somevar");

        Assert.That(path, Is.EqualTo("pathto\\my.exe"));
    }

    [Test]
    public void Paths_without_spaces_are_equal()
    {
        var path1 = PathUtilities.SanitizedPath("\"pathto\\my.exe\" somevar");
        var path2 = PathUtilities.SanitizedPath("pathto\\my.exe somevar");

        Assert.That(path2, Is.EqualTo(path1));
    }

    [Test]
    public void Parse_from_path_with_spaces_having_a_parameter_with_spaces()
    {
        var path = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"");

        Assert.That(path, Is.EqualTo("pathto\\mysuper duper.exe"));
    }

    [Test]
    public void Parse_from_path_with_spaces_having_a_parameter_without_spaces()
    {
        var path = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" somevar");

        Assert.That(path, Is.EqualTo("pathto\\mysuper duper.exe"));
    }

    [Test]
    public void Both_paths_with_spaces_are_equal()
    {
        var path1 = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"");
        var path2 = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" somevar");

        Assert.That(path2, Is.EqualTo(path1));
    }
}
