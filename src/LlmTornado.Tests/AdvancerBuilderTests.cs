using LlmTornado.Agents.ChatRuntime.Orchestration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Tests;

[TestFixture]
public class AdvancerBuilderTests
{
    // Test runnables
    private class TestRunnable1 : OrchestrationRunnable<string, string>
    {
        public TestRunnable1(Orchestration? orchestrator = null, string name = "Test1") 
            : base(orchestrator, name) { }

        public override ValueTask<string> Invoke(RunnableProcess<string, string> input)
        {
            return ValueTask.FromResult(input.Input ?? "default");
        }
    }

    private class TestRunnable2 : OrchestrationRunnable<int, int>
    {
        public TestRunnable2(Orchestration? orchestrator = null, string name = "Test2") 
            : base(orchestrator, name) { }

        public override ValueTask<int> Invoke(RunnableProcess<int, int> input)
        {
            return ValueTask.FromResult(input.Input);
        }
    }

    private class TestRunnable3 : OrchestrationRunnable<string, string>
    {
        public TestRunnable3(Orchestration? orchestrator = null, string name = "Test3") 
            : base(orchestrator, name) { }

        public override ValueTask<string> Invoke(RunnableProcess<string, string> input)
        {
            return ValueTask.FromResult(input.Input ?? "default");
        }
    }

    [Test]
    public void Builder_WithSingleRunnable_CreatesAdvancer()
    {
        // Arrange
        var targetRunnable = new TestRunnable1(name: "Target");
        var builder = new AdvancerBuilder<string>()
            .ToRunnable(targetRunnable);

        // Act
        var advancers = builder.Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(1));
        Assert.That(advancers[0].NextRunnable, Is.EqualTo(targetRunnable));
    }

    [Test]
    public void Builder_WithCondition_CreatesAdvancerWithCondition()
    {
        // Arrange
        var targetRunnable = new TestRunnable1(name: "Target");
        var builder = new AdvancerBuilder<string>()
            .ToRunnable(targetRunnable)
            .WithCondition(output => output == "valid");

        // Act
        var advancers = builder.Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(1));
        
        // Test the condition
        Assert.That(advancers[0].CanAdvance("valid"), Is.True);
        Assert.That(advancers[0].CanAdvance("invalid"), Is.False);
    }

    [Test]
    public void Builder_WithConversion_CreatesAdvancerWithConverter()
    {
        // Arrange
        var targetRunnable = new TestRunnable2(name: "Target");
        var builder = new AdvancerBuilder<string>()
            .ToRunnable(targetRunnable)
            .WithCondition(output => output != null)
            .WithConversion<int>(output => output.Length);

        // Act
        var advancers = builder.Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(1));
        Assert.That(advancers[0], Is.InstanceOf<OrchestrationAdvancer<string, int>>());
        
        // Test conversion
        var testString = "test";
        var canAdvance = advancers[0].CanAdvance(testString);
        Assert.That(canAdvance, Is.True);
        
        // Verify converter result
        var typedAdvancer = (OrchestrationAdvancer<string, int>)advancers[0];
        Assert.That(typedAdvancer.ConverterResult, Is.EqualTo(testString.Length));
    }

    [Test]
    public void Builder_WithMultipleRunnables_CreatesMultipleAdvancers()
    {
        // Arrange
        var runnable1 = new TestRunnable1(name: "Target1");
        var runnable2 = new TestRunnable3(name: "Target2");
        var builder = new AdvancerBuilder<string>()
            .ToRunnable(runnable1)
            .ToRunnable(runnable2)
            .WithCondition(output => output.StartsWith("test"));

        // Act
        var advancers = builder.Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(2));
        Assert.That(advancers[0].NextRunnable, Is.EqualTo(runnable1));
        Assert.That(advancers[1].NextRunnable, Is.EqualTo(runnable2));
        
        // Both should have the same condition
        Assert.That(advancers[0].CanAdvance("test123"), Is.True);
        Assert.That(advancers[1].CanAdvance("test123"), Is.True);
        Assert.That(advancers[0].CanAdvance("invalid"), Is.False);
        Assert.That(advancers[1].CanAdvance("invalid"), Is.False);
    }

    [Test]
    public void Builder_WithMultipleRunnablesAndConversion_CreatesMultipleAdvancersWithConversion()
    {
        // Arrange
        var runnable1 = new TestRunnable2(name: "Target1");
        var runnable2 = new TestRunnable2(name: "Target2");
        var builder = new AdvancerBuilder<string>()
            .ToRunnable(runnable1)
            .ToRunnable(runnable2)
            .WithCondition(output => !string.IsNullOrEmpty(output))
            .WithConversion<int>(output => output.Length);

        // Act
        var advancers = builder.Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(2));
        
        // Test both advancers
        var testString = "hello";
        Assert.That(advancers[0].CanAdvance(testString), Is.True);
        Assert.That(advancers[1].CanAdvance(testString), Is.True);
        
        var typedAdvancer1 = (OrchestrationAdvancer<string, int>)advancers[0];
        var typedAdvancer2 = (OrchestrationAdvancer<string, int>)advancers[1];
        Assert.That(typedAdvancer1.ConverterResult, Is.EqualTo(testString.Length));
        Assert.That(typedAdvancer2.ConverterResult, Is.EqualTo(testString.Length));
    }

    [Test]
    public void Builder_WithNoRunnables_ThrowsException()
    {
        // Arrange
        var builder = new AdvancerBuilder<string>()
            .WithCondition(output => true);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void Builder_WithNullRunnable_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AdvancerBuilder<string>().ToRunnable(null!));
    }

    [Test]
    public void Builder_WithNullCondition_ThrowsException()
    {
        // Arrange
        var targetRunnable = new TestRunnable1(name: "Target");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AdvancerBuilder<string>()
                .ToRunnable(targetRunnable)
                .WithCondition(null!));
    }

    [Test]
    public void Builder_WithNullConversion_ThrowsException()
    {
        // Arrange
        var targetRunnable = new TestRunnable1(name: "Target");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new AdvancerBuilder<string>()
                .ToRunnable(targetRunnable)
                .WithConversion<int>(null!));
    }

    [Test]
    public void AddAdvancerRange_WithBuiltAdvancers_AddsAllAdvancers()
    {
        // Arrange
        var sourceRunnable = new TestRunnable1(name: "Source");
        var targetRunnable1 = new TestRunnable1(name: "Target1");
        var targetRunnable2 = new TestRunnable1(name: "Target2");
        
        var advancers = new AdvancerBuilder<string>()
            .ToRunnable(targetRunnable1)
            .ToRunnable(targetRunnable2)
            .WithCondition(output => output.Length > 5)
            .Build();

        // Act
        sourceRunnable.AddAdvancerRange(advancers);

        // Assert
        Assert.That(sourceRunnable.Advances, Is.Not.Null);
        Assert.That(sourceRunnable.Advances.Count, Is.EqualTo(2));
        Assert.That(sourceRunnable.Advances[0].NextRunnable, Is.EqualTo(targetRunnable1));
        Assert.That(sourceRunnable.Advances[1].NextRunnable, Is.EqualTo(targetRunnable2));
    }

    [Test]
    public void AddAdvancerRange_WithNullArray_ThrowsException()
    {
        // Arrange
        var sourceRunnable = new TestRunnable1(name: "Source");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            sourceRunnable.AddAdvancerRange(null!));
    }

    [Test]
    public void AddAdvancerRange_WithNullElementInArray_ThrowsException()
    {
        // Arrange
        var sourceRunnable = new TestRunnable1(name: "Source");
        var validAdvancer = new AdvancerBuilder<string>()
            .ToRunnable(new TestRunnable1(name: "Target"))
            .Build()[0];
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            sourceRunnable.AddAdvancerRange(validAdvancer, null!));
    }

    [Test]
    public void StaticBuilder_For_CreatesTypedBuilder()
    {
        // Act
        var builder = AdvancerBuilder.For<string>();

        // Assert
        Assert.That(builder, Is.Not.Null);
        Assert.That(builder, Is.InstanceOf<AdvancerBuilder<string>>());
    }

    [Test]
    public void Builder_FluentInterface_ChainsCorrectly()
    {
        // Arrange & Act
        var targetRunnable = new TestRunnable2(name: "Target");
        var advancers = AdvancerBuilder.For<string>()
            .ToRunnable(targetRunnable)
            .WithCondition(output => output.Length > 3)
            .WithConversion<int>(output => output.Length)
            .Build();

        // Assert
        Assert.That(advancers, Is.Not.Null);
        Assert.That(advancers.Length, Is.EqualTo(1));
        Assert.That(advancers[0].NextRunnable, Is.EqualTo(targetRunnable));
    }
}
