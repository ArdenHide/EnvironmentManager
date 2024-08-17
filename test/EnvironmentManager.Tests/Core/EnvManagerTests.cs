using Moq;
using Xunit;
using AutoMapper;
using FluentAssertions;
using EnvironmentManager.Core;
using Microsoft.Extensions.Logging;
using EnvironmentManager.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnvironmentManager.Tests.Core;

public class EnvManagerTests : TestData
{
    public class Constructors
    {
        [Fact]
        internal void WithoutParameters_ShouldInitializeObjectWithoutErrorsWithDefaultConfiguration()
        {
            var envManager = new EnvManager();

            envManager.Should().NotBeNull();
            envManager.Mapper.ConfigurationProvider.Should().BeSameAs(DefaultMapperConfiguration.DefaultConfiguration);
            envManager.Logger.Should().BeSameAs(NullLogger<IEnvManager>.Instance);
        }

        [Fact]
        internal void WithCustomConfiguration_ShouldInitializeObjectWithoutErrorsWithCustomConfiguration()
        {
            var mockConfig = new Mock<MapperConfiguration>(DefaultConfigurationExpressions.DefaultConfiguration).Object;
            var mockLogger = new Mock<ILogger<EnvManager>>().Object;

            var envManager = new EnvManager(config: mockConfig, logger: mockLogger);

            envManager.Should().NotBeNull();
            envManager.Mapper.ConfigurationProvider.Should().BeSameAs(mockConfig);
            envManager.Logger.Should().BeSameAs(mockLogger);
        }
    }

    public class Get : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();
            Environment.SetEnvironmentVariable(variableName, stored);

            RunTest(() => EnvironmentManager.Get(typeof(TOutput), variableName), expected!);
        }
    }

    public class Get_Generic : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();
            Environment.SetEnvironmentVariable(variableName, stored);

            RunTest(() => EnvironmentManager.Get<TOutput>(variableName), expected);
        }
    }

    public class GetRequired : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();
            Environment.SetEnvironmentVariable(variableName, stored);

            RunTest(() => EnvironmentManager.GetRequired(typeof(TOutput), variableName), expected!);
        }
    }

    public class GetRequired_Generic : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();
            Environment.SetEnvironmentVariable(variableName, stored);

            RunTest(() => EnvironmentManager.GetRequired<TOutput>(variableName), expected);
        }
    }

    public class GetInternal : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();
            Environment.SetEnvironmentVariable(variableName, stored);

            RunTest(() => EnvironmentManager.GetInternal<TOutput>(typeof(TOutput), variableName, true), expected);
        }
    }

    public class ConvertEnvironmentValueInternal : TestData
    {
        [Theory]
        [MemberData(nameof(CommonTestData))]
        [MemberData(nameof(ImplementedTestData))]
        public void WhenValidValue_ShouldReturnConvertedValue<TOutput>(string stored, TOutput expected)
        {
            var variableName = NameOfVariable<TOutput>();

            RunTest(() => EnvironmentManager.ConvertEnvironmentValueInternal<TOutput>(typeof(TOutput), variableName, stored, true), expected);
        }

        [Fact]
        internal void WhenNullValueWithRaiseException_ShouldThrowArgumentNullException()
        {
            var name = NameOfVariable<int>();

            var testCode = () => EnvironmentManager.ConvertEnvironmentValueInternal<int>(typeof(int), name, null, true);

            testCode.Should().Throw<ArgumentNullException>()
                .WithMessage($"Environment variable '{name}' is null or empty. (Parameter 'envValue')");
        }

        [Fact]
        internal void WhenNullValueWithWithoutRaiseException_ShouldThrowArgumentNullException()
        {
            var name = NameOfVariable<int>();

            var result = EnvironmentManager.ConvertEnvironmentValueInternal<int>(typeof(int), name, null, false);

            result.Should().Be(default);
            MockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Environment variable '{name}' is null or empty. Trying return default value."),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
                ),
                Times.Once
            );
        }

        [Fact]
        internal void WhenInvalidValueWithRaiseException_ShouldThrowInvalidCastException()
        {
            var name = NameOfVariable<int>();

            var testCode = () => EnvironmentManager.ConvertEnvironmentValueInternal<int>(typeof(int), name, "abc", true);

            testCode.Should().Throw<InvalidCastException>()
                .WithMessage($"Failed to convert environment variable '{name}' to type '{typeof(int)}'.")
                .WithInnerException<FormatException>()
                .WithMessage("Input string was not in a correct format.");
        }

        [Fact]
        internal void WhenInvalidValueWithoutRaiseException_ShouldReturnDefaultInt()
        {
            var name = NameOfVariable<int>();

            var result = EnvironmentManager.ConvertEnvironmentValueInternal<int>(typeof(int), name, "abc", false);

            result.Should().Be(default);
            MockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Failed to convert environment variable '{name}' to type 'System.Int32'. Trying return default value."),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
                ),
                Times.Once
            );
        }
    }
}