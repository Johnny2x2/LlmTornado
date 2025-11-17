# AdvancerBuilder Documentation

The `AdvancerBuilder` provides a fluent API for creating `OrchestrationAdvancer` instances with a more readable and maintainable syntax. This builder pattern simplifies the process of defining orchestration transitions between runnables.

## Overview

The builder supports:
- ✅ Single or multiple target runnables
- ✅ Conditional advancement logic
- ✅ Type conversion between runnables
- ✅ Parallel advancement scenarios
- ✅ Fluent, chainable API

## Basic Usage

### Simple Advancement with Condition

```csharp
var sourceRunnable = new MyRunnable();
var targetRunnable = new NextRunnable();

sourceRunnable.AddAdvancerRange(
    new AdvancerBuilder<MyOutputType>()
        .ToRunnable(targetRunnable)
        .WithCondition(output => output.IsValid)
        .Build()
);
```

### Advancement with Type Conversion

```csharp
sourceRunnable.AddAdvancerRange(
    new AdvancerBuilder<MyOutputType>()
        .ToRunnable(targetRunnable)
        .WithCondition(output => output.IsValid)
        .WithConversion<TargetInputType>(output => output.ConvertToTarget())
        .Build()
);
```

### Multiple Target Runnables (Parallel Advancement)

```csharp
sourceRunnable.AddAdvancerRange(
    new AdvancerBuilder<MyOutputType>()
        .ToRunnable(targetRunnable1)
        .ToRunnable(targetRunnable2)
        .WithCondition(output => output.IsValid)
        .WithConversion<TargetInputType>(output => output.ConvertToTarget())
        .Build()
);
```

## API Reference

### AdvancerBuilder\<TInput\>

#### Methods

##### `ToRunnable(OrchestrationRunnableBase runnable)`
Adds a target runnable that this advancer will transition to. Can be called multiple times to create parallel advancers.

**Parameters:**
- `runnable`: The target runnable to advance to

**Returns:** The builder instance for method chaining

**Example:**
```csharp
builder.ToRunnable(nextRunnable)
       .ToRunnable(anotherRunnable)
```

##### `WithCondition(AdvancementRequirement<TInput> condition)`
Sets the condition that must be satisfied for the advancement to occur.

**Parameters:**
- `condition`: A predicate function that evaluates the output and returns true if advancement should occur

**Returns:** The builder instance for method chaining

**Example:**
```csharp
builder.WithCondition(result => result.IsValid && result.ErrorCount == 0)
```

##### `WithConversion<TOutput>(AdvancementResultConverter<TInput, TOutput> converter)`
Sets the conversion function to transform the output before passing it to the next runnable.

**Parameters:**
- `converter`: A function that converts the current output to the target runnable's input type

**Returns:** The builder instance for method chaining

**Example:**
```csharp
builder.WithConversion<int>(result => result.ErrorCode)
```

##### `Build()`
Builds and returns an array of `OrchestrationAdvancer` instances based on the configured settings.

**Returns:** An array of `OrchestrationAdvancer` instances

**Throws:**
- `InvalidOperationException`: If no target runnable has been specified

### Static Factory Method

#### `AdvancerBuilder.For<TOutput>()`
Creates a new `AdvancerBuilder` for the specified output type with better type inference.

**Returns:** A new `AdvancerBuilder<TOutput>` instance

**Example:**
```csharp
var advancers = AdvancerBuilder.For<ValidationResult>()
    .ToRunnable(successRunnable)
    .WithCondition(r => r.IsValid)
    .Build();
```

## Common Patterns

### If/Else Pattern

```csharp
// The "if" branch
runnable.AddAdvancerRange(
    AdvancerBuilder.For<ValidationResult>()
        .ToRunnable(successRunnable)
        .WithCondition(r => r.IsValid)
        .WithConversion<string>(r => r.Message)
        .Build()
);

// The "else" branch
runnable.AddAdvancerRange(
    AdvancerBuilder.For<ValidationResult>()
        .ToRunnable(errorRunnable)
        .WithCondition(r => !r.IsValid)
        .WithConversion<int>(r => r.ErrorCode)
        .Build()
);
```

### Parallel Execution

Enable parallel advancement to execute multiple paths simultaneously:

```csharp
var runnable = new MyRunnable
{
    AllowsParallelAdvances = true
};

runnable.AddAdvancerRange(
    AdvancerBuilder.For<MyOutput>()
        .ToRunnable(path1Runnable)
        .ToRunnable(path2Runnable)
        .WithCondition(output => output.ShouldFork)
        .Build()
);
```

### Complex Conditional Branching

```csharp
// Success path
runnable.AddAdvancerRange(
    AdvancerBuilder.For<Result>()
        .ToRunnable(successRunnable)
        .WithCondition(r => r.IsValid && r.ErrorCode == 0)
        .WithConversion<string>(r => r.Message)
        .Build()
);

// Warning path
runnable.AddAdvancerRange(
    AdvancerBuilder.For<Result>()
        .ToRunnable(warningRunnable)
        .WithCondition(r => r.IsValid && r.ErrorCode > 0)
        .WithConversion<string>(r => $"Warning: {r.Message}")
        .Build()
);

// Error path
runnable.AddAdvancerRange(
    AdvancerBuilder.For<Result>()
        .ToRunnable(errorRunnable)
        .WithCondition(r => !r.IsValid)
        .WithConversion<int>(r => r.ErrorCode)
        .Build()
);
```

### Always Advance (No Condition)

When you want to always proceed to the next runnable:

```csharp
runnable.AddAdvancerRange(
    AdvancerBuilder.For<MyOutput>()
        .ToRunnable(nextRunnable)
        .WithConversion<TargetInput>(o => o.Transform())
        .Build()
);
// Without WithCondition, it defaults to always advancing
```

## Benefits

1. **Readability**: The fluent API makes the code more self-documenting
2. **Maintainability**: Easier to modify and understand advancement logic
3. **Type Safety**: Generic type parameters provide compile-time type checking
4. **Flexibility**: Supports complex scenarios with minimal boilerplate
5. **Reusability**: Create builders once and reuse them across runnables

## Migration from Old API

### Before (Old API)
```csharp
sourceRunnable.AddAdvancer<TargetInputType>(
    methodToInvoke: result => result.IsValid,
    conversionMethod: result => result.Convert(),
    nextRunnable: targetRunnable
);
```

### After (New Builder API)
```csharp
sourceRunnable.AddAdvancerRange(
    AdvancerBuilder.For<SourceOutputType>()
        .ToRunnable(targetRunnable)
        .WithCondition(result => result.IsValid)
        .WithConversion<TargetInputType>(result => result.Convert())
        .Build()
);
```

## Notes

- The builder creates one advancer per target runnable specified
- If no condition is provided via `WithCondition()`, the default behavior is to always advance
- Type conversion is optional and only needed when the source output type differs from the target input type
- The `AddAdvancerRange()` method accepts a params array, so you can pass multiple advancer arrays if needed
