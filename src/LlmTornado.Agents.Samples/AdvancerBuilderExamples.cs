using LlmTornado.Agents.ChatRuntime.Orchestration;
using System;

namespace LlmTornado.Agents.Samples.AdvancerBuilderExamples;

/// <summary>
/// Example demonstrating the usage of AdvancerBuilder to create orchestration advancers
/// with a fluent and readable syntax.
/// </summary>
/// <remarks>
/// The AdvancerBuilder simplifies the creation of OrchestrationAdvancers by providing
/// a fluent interface that makes the code more maintainable and easier to understand.
/// </remarks>
public class AdvancerBuilderExamples
{
    // Example output type
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ErrorCode { get; set; }
    }

    // Example runnables
    private class SuccessRunnable : OrchestrationRunnable<string, string>
    {
        public SuccessRunnable(Orchestration? orchestrator = null) 
            : base(orchestrator, "SuccessPath") { }

        public override ValueTask<string> Invoke(RunnableProcess<string, string> input)
        {
            return ValueTask.FromResult($"Success: {input.Input}");
        }
    }

    private class ErrorRunnable : OrchestrationRunnable<int, string>
    {
        public ErrorRunnable(Orchestration? orchestrator = null) 
            : base(orchestrator, "ErrorPath") { }

        public override ValueTask<string> Invoke(RunnableProcess<int, string> input)
        {
            return ValueTask.FromResult($"Error code: {input.Input}");
        }
    }

    private class ValidationRunnable : OrchestrationRunnable<ValidationResult, ValidationResult>
    {
        public ValidationRunnable(Orchestration? orchestrator = null) 
            : base(orchestrator, "Validator") { }

        public override ValueTask<ValidationResult> Invoke(RunnableProcess<ValidationResult, ValidationResult> input)
        {
            // Perform validation logic
            return ValueTask.FromResult(input.Input);
        }
    }

    /// <summary>
    /// Example 1: Simple single runnable with condition
    /// Demonstrates the basic usage pattern from the problem statement
    /// </summary>
    public void Example1_SingleRunnableWithCondition()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable = new SuccessRunnable();

        // Original syntax from problem statement:
        // Runnable.AddAdvancerRange(new AdvancerBuilder()
        //     .ToRunnable(NextRunnable)
        //     .WithCondition((condition) => condition.IsTrue)
        //     .WithConversion((toConvert) => toConvert.Convert()));

        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(successRunnable)
                .WithCondition(result => result.IsValid)
                .WithConversion<string>(result => result.Message)
                .Build()
        );
    }

    /// <summary>
    /// Example 2: Multiple runnables with same condition
    /// Demonstrates parallel advancement to multiple runnables from the problem statement
    /// </summary>
    public void Example2_MultipleRunnablesWithSameCondition()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable1 = new SuccessRunnable();
        var successRunnable2 = new SuccessRunnable();

        // Original syntax from problem statement:
        // Runnable.AddAdvancerRange(new AdvancerBuilder()
        //     .ToRunnable(NextRunnable)
        //     .ToRunnable(OtherRunnable)
        //     .WithCondition((condition) => condition.IsTrue)
        //     .WithConversion((toConvert) => toConvert.Convert()));

        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(successRunnable1)
                .ToRunnable(successRunnable2)
                .WithCondition(result => result.IsValid)
                .WithConversion<string>(result => result.Message)
                .Build()
        );
    }

    /// <summary>
    /// Example 3: If/Else pattern using separate builders
    /// Demonstrates conditional branching with different conversions
    /// </summary>
    public void Example3_IfElsePattern()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable = new SuccessRunnable();
        var errorRunnable = new ErrorRunnable();

        // Approach: Use separate AddAdvancerRange calls for if/else logic
        // The "if" branch - when validation succeeds
        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(successRunnable)
                .WithCondition(result => result.IsValid)
                .WithConversion<string>(result => result.Message)
                .Build()
        );

        // The "else" branch - when validation fails
        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(errorRunnable)
                .WithCondition(result => !result.IsValid)
                .WithConversion<int>(result => result.ErrorCode)
                .Build()
        );
    }

    /// <summary>
    /// Example 4: Parallel advances using AllowsParallelAdvances
    /// When you want multiple paths to execute simultaneously
    /// </summary>
    public void Example4_ParallelAdvances()
    {
        var validationRunnable = new ValidationRunnable
        {
            // Enable parallel advancement to all matching conditions
            AllowsParallelAdvances = true
        };
        
        var successRunnable1 = new SuccessRunnable();
        var successRunnable2 = new SuccessRunnable();

        // Both will execute in parallel when condition is met
        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(successRunnable1)
                .ToRunnable(successRunnable2)
                .WithCondition(result => result.IsValid)
                .WithConversion<string>(result => result.Message)
                .Build()
        );
    }

    /// <summary>
    /// Example 5: Using static factory method for cleaner syntax
    /// </summary>
    public void Example5_StaticFactoryMethod()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable = new SuccessRunnable();

        // Using the static For<T>() method provides better type inference
        validationRunnable.AddAdvancerRange(
            AdvancerBuilder.For<ValidationResult>()
                .ToRunnable(successRunnable)
                .WithCondition(result => result.IsValid)
                .WithConversion<string>(result => result.Message)
                .Build()
        );
    }

    /// <summary>
    /// Example 6: Condition without conversion
    /// When the input and output types match
    /// </summary>
    public void Example6_ConditionWithoutConversion()
    {
        var runnable1 = new SuccessRunnable();
        var runnable2 = new SuccessRunnable();

        // No conversion needed when types match
        runnable1.AddAdvancerRange(
            new AdvancerBuilder<string>()
                .ToRunnable(runnable2)
                .WithCondition(output => !string.IsNullOrEmpty(output))
                .Build()
        );
    }

    /// <summary>
    /// Example 7: Always advance (no condition)
    /// When you want to always proceed to the next runnable
    /// </summary>
    public void Example7_AlwaysAdvance()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable = new SuccessRunnable();

        // Without WithCondition, it defaults to always advancing
        validationRunnable.AddAdvancerRange(
            new AdvancerBuilder<ValidationResult>()
                .ToRunnable(successRunnable)
                .WithConversion<string>(result => result.Message)
                .Build()
        );
    }

    /// <summary>
    /// Example 8: Complex conditional branching
    /// Multiple conditions with different target runnables
    /// </summary>
    public void Example8_ComplexBranching()
    {
        var validationRunnable = new ValidationRunnable();
        var successRunnable = new SuccessRunnable();
        var errorRunnable = new ErrorRunnable();
        var warningRunnable = new SuccessRunnable();

        // Valid results go to success path
        validationRunnable.AddAdvancerRange(
            AdvancerBuilder.For<ValidationResult>()
                .ToRunnable(successRunnable)
                .WithCondition(r => r.IsValid && r.ErrorCode == 0)
                .WithConversion<string>(r => r.Message)
                .Build()
        );

        // Warning results (valid but with warnings)
        validationRunnable.AddAdvancerRange(
            AdvancerBuilder.For<ValidationResult>()
                .ToRunnable(warningRunnable)
                .WithCondition(r => r.IsValid && r.ErrorCode > 0)
                .WithConversion<string>(r => $"Warning: {r.Message}")
                .Build()
        );

        // Error results go to error path
        validationRunnable.AddAdvancerRange(
            AdvancerBuilder.For<ValidationResult>()
                .ToRunnable(errorRunnable)
                .WithCondition(r => !r.IsValid)
                .WithConversion<int>(r => r.ErrorCode)
                .Build()
        );
    }
}
