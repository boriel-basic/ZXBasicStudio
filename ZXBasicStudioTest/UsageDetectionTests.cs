using Xunit;
using FluentAssertions;
using ZXBasicStudio.BuildSystem;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ZXBasicStudioTest
{
    public class UsageDetectionTests
    {
        [Fact]
        public void ZXBasicMap_ShouldDetectSigilVariableUsage()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            string mainFileContent = "dim a$ as string\na$ = \"Hello\"\nprint a$";
            string mainPath = Path.Combine(tempDir, "main.bas");
            File.WriteAllText(mainPath, mainFileContent);

            var mainCodeFile = new ZXCodeFile(mainPath);
            var allFiles = new List<ZXCodeFile> { mainCodeFile };
            string buildLog = ""; // No unused warnings for now

            // Act
            var basicMap = new ZXBasicMap(mainCodeFile, allFiles, buildLog);

            // Assert
            basicMap.GlobalVariables.Should().Contain(v => v.Name == "a$");
            var varA = basicMap.GlobalVariables.First(v => v.Name == "a$");
            varA.Unused.Should().BeFalse("a$ is used later in the code");

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ZXBasicMap_ShouldDistinguishSigilAndNonSigilVariables()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            // a is used, a$ is NOT used (only defined)
            string mainFileContent = "dim a as integer\ndim a$ as string\na = 10\nprint a";
            string mainPath = Path.Combine(tempDir, "main.bas");
            File.WriteAllText(mainPath, mainFileContent);

            var mainCodeFile = new ZXCodeFile(mainPath);
            var allFiles = new List<ZXCodeFile> { mainCodeFile };
            string buildLog = "";

            // Act
            var basicMap = new ZXBasicMap(mainCodeFile, allFiles, buildLog);

            // Assert
            basicMap.GlobalVariables.Should().Contain(v => v.Name == "a");
            basicMap.GlobalVariables.Should().NotContain(v => v.Name == "a$", "a$ is NOT used, so it should be skipped (if it's not marked as unused in buildLog, the local regex check should mark it as unused and it's skipped in GlobalVariables list generation)");

            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}
