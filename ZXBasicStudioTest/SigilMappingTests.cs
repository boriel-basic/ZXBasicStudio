using Xunit;
using FluentAssertions;
using ZXBasicStudio.BuildSystem;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace ZXBasicStudioTest
{
    public class SigilMappingTests
    {
        [Fact]
        public void GetVariables_ShouldMatchSigilVariable()
        {
            // Arrange
            // We use GetUninitializedObject to skip the constructor which depends on files
            var basicMap = (ZXBasicMap)FormatterServices.GetUninitializedObject(typeof(ZXBasicMap));
            
            basicMap.GlobalVariables = new[]
            {
                new ZXBasicVariable { Name = "a$" }
            };
            basicMap.Subs = new ZXBasicSub[0];
            basicMap.Functions = new ZXBasicFunction[0];
            basicMap.BuildLocations = new ZXBasicLocation[0];

            // IC Content mimicking a global variable '_a'
            string icContent = "--- end of user code ---\n('var', '_a', '0')";
            // Map content mimicking the same variable
            string mapContent = "8000: ._a";

            // Act
            var variableMap = (ZXVariableMap)FormatterServices.GetUninitializedObject(typeof(ZXVariableMap));
            // We need to initialize the private 'vars' list since we used GetUninitializedObject
            var varsField = typeof(ZXVariableMap).GetField("vars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            varsField!.SetValue(variableMap, new List<ZXVariable>());
            
            variableMap.ProcessGlobalVariables(icContent, mapContent, basicMap);
            var variables = variableMap.Variables;

            // Assert
            variables.Should().NotBeNull();
            var variable = variables.FirstOrDefault(v => v.Name == "a$");
            variable.Should().NotBeNull("Variable 'a$' should be found even if IC uses '_a'");
            variable!.Address.AddressValue.Should().Be(0x8000);
        }

        [Fact]
        public void GetVariables_ShouldBeCaseInsensitive()
        {
            // Arrange
            var basicMap = (ZXBasicMap)FormatterServices.GetUninitializedObject(typeof(ZXBasicMap));
            
            basicMap.GlobalVariables = new[]
            {
                new ZXBasicVariable { Name = "MyVar$" }
            };
            basicMap.Subs = new ZXBasicSub[0];
            basicMap.Functions = new ZXBasicFunction[0];
            basicMap.BuildLocations = new ZXBasicLocation[0];

            // IC Content uses lowercase '_myvar'
            string icContent = "--- end of user code ---\n('var', '_myvar', '0')";
            string mapContent = "9000: ._myvar";

            // Act
            var variableMap = (ZXVariableMap)FormatterServices.GetUninitializedObject(typeof(ZXVariableMap));
            var varsField = typeof(ZXVariableMap).GetField("vars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            varsField!.SetValue(variableMap, new List<ZXVariable>());
            
            variableMap.ProcessGlobalVariables(icContent, mapContent, basicMap);
            var variables = variableMap.Variables;

            // Assert
            var variable = variables.FirstOrDefault(v => v.Name == "MyVar$");
            variable.Should().NotBeNull("Variable 'MyVar$' should be matched case-insensitively");
            variable!.Address.AddressValue.Should().Be(0x9000);
        }

        [Fact]
        public void ProcessLocalVariables_ShouldMatchSubNameWithSigil()
        {
            // Arrange
            var basicMap = (ZXBasicMap)FormatterServices.GetUninitializedObject(typeof(ZXBasicMap));
            
            var sub = new ZXBasicSub { Name = "MySub$" };
            sub.LocalVariables = new List<ZXBasicVariable>();
            sub.InputParameters = new List<ZXBasicParameter> 
            { 
                new ZXBasicParameter { Name = "param1", Offset = -2, Storage = ZXVariableStorage.U16 } 
            };

            basicMap.GlobalVariables = new ZXBasicVariable[0];
            basicMap.Subs = new[] { sub };
            basicMap.Functions = new ZXBasicFunction[0];
            basicMap.BuildLocations = new[]
            {
                new ZXBasicLocation { Name = "MySub$", LocationType = ZXBasicLocationType.Sub, FirstLine = 0, LastLine = 10, File = "main.bas" }
            };

            // IC Content showing start and end of MySub (sigil is stripped in label usually: _MySub)
            // Note: ProcessLocalVariables extracts locName = label.Substring(1) from '_MySub' -> 'MySub'
            string icContent = "('label', '_MySub')\n('label', '_MySub__leave')\n--- end of user code ---";
            // Map content showing start and end addresses
            string mapContent = "8000: ._MySub\n8010: ._MySub__leave";

            // Act
            var variableMap = (ZXVariableMap)FormatterServices.GetUninitializedObject(typeof(ZXVariableMap));
            var varsField = typeof(ZXVariableMap).GetField("vars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            varsField!.SetValue(variableMap, new List<ZXVariable>());
            
            variableMap.ProcessLocalVariables(icContent, mapContent, basicMap);
            var variables = variableMap.Variables;

            // Assert
            variables.Should().NotBeNull();
            // If MySub$ was matched correctly, its parameters should be added
            variables.Should().Contain(v => v.Name == "param1", "Sub MySub$ should be matched to label _MySub and its parameters processed");
            var param = variables.First(v => v.Name == "param1");
            param.Scope.ScopeName.Should().Be("MySub");
        }
    }
}
