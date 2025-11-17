using System;
using System.Collections.Generic;
using System.Linq;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Fluent builder for creating OrchestrationAdvancers with a more readable and maintainable syntax.
/// Supports building single or multiple advancers with shared conditions and conversions.
/// </summary>
/// <typeparam name="TInput">The input type for the advancers (output type of the current runnable)</typeparam>
public class AdvancerBuilder<TInput>
{
    private readonly List<OrchestrationRunnableBase> _targetRunnables = new List<OrchestrationRunnableBase>();
    private AdvancementRequirement<TInput>? _condition;
    private Delegate? _converter;

    /// <summary>
    /// Adds a target runnable that this advancer will transition to.
    /// Can be called multiple times to create parallel advancers with the same condition and conversion.
    /// </summary>
    /// <param name="runnable">The target runnable to advance to</param>
    /// <returns>The builder for method chaining</returns>
    public AdvancerBuilder<TInput> ToRunnable(OrchestrationRunnableBase runnable)
    {
        if (runnable == null)
            throw new ArgumentNullException(nameof(runnable));
        
        _targetRunnables.Add(runnable);
        return this;
    }

    /// <summary>
    /// Sets the condition that must be satisfied for the advancement to occur.
    /// </summary>
    /// <param name="condition">A predicate function that evaluates the output and returns true if advancement should occur</param>
    /// <returns>The builder for method chaining</returns>
    public AdvancerBuilder<TInput> WithCondition(AdvancementRequirement<TInput> condition)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        
        _condition = condition;
        return this;
    }

    /// <summary>
    /// Sets the conversion function to transform the output before passing it to the next runnable.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the conversion (input type of the target runnable)</typeparam>
    /// <param name="converter">A function that converts the current output to the target runnable's input type</param>
    /// <returns>The builder for method chaining</returns>
    public AdvancerBuilder<TInput> WithConversion<TOutput>(AdvancementResultConverter<TInput, TOutput> converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));
        
        _converter = converter;
        return this;
    }

    /// <summary>
    /// Builds the OrchestrationAdvancers based on the configured settings.
    /// Returns an array of advancers, one for each target runnable specified.
    /// </summary>
    /// <returns>An array of OrchestrationAdvancer instances</returns>
    public OrchestrationAdvancer[] Build()
    {
        if (_targetRunnables.Count == 0)
            throw new InvalidOperationException("At least one target runnable must be specified using ToRunnable()");

        // Default condition to always advance if not specified
        var condition = _condition ?? (_ => true);

        var advancers = new List<OrchestrationAdvancer>();

        foreach (var runnable in _targetRunnables)
        {
            OrchestrationAdvancer advancer;

            if (_converter != null)
            {
                // Create advancer with conversion
                // We need to use reflection to create the generic type since we don't know TOutput at compile time
                var converterType = _converter.GetType();
                var genericArgs = converterType.GetGenericArguments();
                
                if (genericArgs.Length >= 2)
                {
                    var outputType = genericArgs[1];
                    var advancerType = typeof(OrchestrationAdvancer<,>).MakeGenericType(typeof(TInput), outputType);
                    advancer = (OrchestrationAdvancer)Activator.CreateInstance(advancerType, condition, _converter, runnable)!;
                }
                else
                {
                    throw new InvalidOperationException("Converter must be a valid AdvancementResultConverter<TInput, TOutput> delegate");
                }
            }
            else
            {
                // Create simple advancer without conversion
                advancer = new OrchestrationAdvancer<TInput>(condition, runnable);
            }

            advancers.Add(advancer);
        }

        return advancers.ToArray();
    }
}

/// <summary>
/// Entry point for creating AdvancerBuilder instances.
/// Provides a more intuitive starting point for the builder pattern.
/// </summary>
public static class AdvancerBuilder
{
    /// <summary>
    /// Creates a new AdvancerBuilder for the specified output type.
    /// </summary>
    /// <typeparam name="TOutput">The output type of the current runnable (input type for conditions)</typeparam>
    /// <returns>A new AdvancerBuilder instance</returns>
    public static AdvancerBuilder<TOutput> For<TOutput>()
    {
        return new AdvancerBuilder<TOutput>();
    }
}
