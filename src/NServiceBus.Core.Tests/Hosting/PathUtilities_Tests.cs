namespace NServiceBus.Hosting.Tests
{
    using NServiceBus.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class PathUtilities_Tests
    {
        [Test]
        public void Parse_from_path_without_spaces_but_with_quotes()
        {
            var path = PathUtilities.SanitizedPath("\"pathto\\mysuperduper.exe\" somevar");

            Assert.AreEqual("pathto\\mysuperduper.exe", path);
        }

        [Test]
        public void Parse_from_path_without_spaces_but_without_quotes()
        {
            var path = PathUtilities.SanitizedPath("pathto\\mysuperduper.exe somevar");

            Assert.AreEqual("pathto\\mysuperduper.exe", path);
        }

        [Test]
        public void Paths_without_spaces_are_equal()
        {
            var path1 = PathUtilities.SanitizedPath("\"pathto\\mysuperduper.exe\" somevar");
            var path2 = PathUtilities.SanitizedPath("pathto\\mysuperduper.exe somevar");

            Assert.AreEqual(path1, path2);
        }

        [Test]
        public void Parse_from_path_with_spaces_having_a_parameter_with_spaces()
        {
            var path = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"");

            Assert.AreEqual("pathto\\mysuper duper.exe", path);
        }

        [Test]
        public void Parse_from_path_with_spaces_having_a_parameter_without_spaces()
        {
            var path = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" somevar");

            Assert.AreEqual("pathto\\mysuper duper.exe", path);
        }

        [Test]
        public void Both_paths_with_spaces_are_equal()
        {
            var path1 = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"");
            var path2 = PathUtilities.SanitizedPath("\"pathto\\mysuper duper.exe\" somevar");

            Assert.AreEqual(path1, path2);
        }
    }
}
