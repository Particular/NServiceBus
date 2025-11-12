# Pull Request #7445: Explicit Custom Saga Finder Registration API

## Overview

**Title:** Explicit custom saga finder registration API  
**Status:** Open (as of November 12, 2025)  
**Author:** @andreasohlund  
**Label:** Breaking change  
**Branch:** `explicit-saga-finders` → `master`

This PR introduces a significant refactoring of the saga finder mechanism in NServiceBus, replacing automatic assembly scanning for custom saga finders with an explicit registration API.

## Summary

The changes introduce a new explicit mapping API for custom saga finders through the `ConfigureFinderMapping<TMessage, TFinder>()` method. This is a **breaking change** that removes the previous automatic discovery of saga finders via assembly scanning. Additionally, the PR aligns exception types for validation to use `ArgumentException` consistently.

### Key Changes

1. **Explicit Finder Registration:** Custom saga finders must now be explicitly registered using `mapper.ConfigureFinderMapping<TMessage, TFinder>()` in the saga's `ConfigureHowToFindSaga` method.

2. **Removal of Assembly Scanning:** The framework no longer automatically discovers and registers custom finders through assembly scanning.

3. **Exception Type Alignment:** Validation exceptions have been standardized to throw `ArgumentException` instead of mixed exception types.

4. **Code Modernization:** Extensive use of C# modern features including:
   - Primary constructors
   - Expression-bodied members
   - Collection expressions
   - Target-typed new expressions

## Breaking Changes

### 1. Custom Finder Registration

**Before:**
```csharp
// Custom finders were automatically discovered through assembly scanning
public class CustomFinder : ISagaFinder<MySaga.SagaData, StartMessage>
{
    public Task<MySaga.SagaData> FindBy(StartMessage message, ...)
    {
        // Implementation
    }
}

protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
{
    // No explicit configuration needed - finder discovered automatically
}
```

**After:**
```csharp
// Custom finders must be explicitly registered
public class CustomFinder : ISagaFinder<MySaga.SagaData, StartMessage>
{
    public Task<MySaga.SagaData> FindBy(StartMessage message, ...)
    {
        // Implementation
    }
}

protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
{
    // Explicit registration required
    mapper.ConfigureFinderMapping<StartMessage, CustomFinder>();
}
```

### 2. Exception Type Changes

Validation errors that previously threw different exception types now consistently throw `ArgumentException`:
- Property mapping validation
- Finder configuration validation
- Saga correlation validation

## API Changes

### New Public APIs

1. **IConfigureHowToFindSagaWithFinder Interface**
   ```csharp
   public interface IConfigureHowToFindSagaWithFinder
   {
       void ConfigureMapping<TSagaEntity, TMessage, TFinder>()
           where TFinder : ISagaFinder<TSagaEntity, TMessage>
           where TSagaEntity : IContainSagaData;
   }
   ```

2. **SagaPropertyMapper.ConfigureFinderMapping Method**
   ```csharp
   public class SagaPropertyMapper<TSagaData>
   {
       public void ConfigureFinderMapping<TMessage, TFinder>()
           where TFinder : ISagaFinder<TSagaData, TMessage>;
   }
   ```

### Modified APIs

1. **IConfigureHowToFindSagaWithMessage**
   - Added `class` constraint to `TSagaEntity` generic parameter

2. **IConfigureHowToFindSagaWithMessageHeaders**
   - Added `class` constraint to `TSagaEntity` generic parameter

### Removed/Obsoleted APIs

1. **SagaMetadata.Create overload** - Removed overload accepting `IEnumerable<Type> availableTypes` and `Conventions`
2. **SagaMetadataCollection.Initialize overload** - Removed overload accepting `Conventions` parameter
3. **SagaFinderDefinition properties:**
   - Removed `Type` property
   - Removed `MessageTypeName` property
   - Removed `Properties` dictionary

### Internal Refactoring

Several internal classes were removed or significantly refactored:
- `CorrelationSagaToMessageMap` - Removed
- `CustomFinderSagaToMessageMap` - Removed
- `HeaderFinderSagaToMessageMap` - Removed
- `PropertyFinderSagaToMessageMap` - Removed
- `SagaFinder` abstract class - Removed
- `SagaToMessageMap` - Removed

New internal interfaces:
- `ICoreSagaFinder` - Internal interface for saga finder implementations

## Statistics

- **Files Changed:** 43
- **Additions:** +427 lines
- **Deletions:** -794 lines
- **Net Change:** -367 lines (code reduction through simplification)
- **Commits:** 24 commits

## Impact on Different Areas

### 1. Core Saga Framework
- **CustomFinderAdapter**: Now uses `ObjectFactory<TFinder>` for strongly-typed finder instantiation
- **PropertySagaFinder**: Changed to resolve `ISagaPersister` via DI instead of constructor injection
- **HeaderPropertySagaFinder**: Changed to resolve `ISagaPersister` via DI instead of constructor injection
- **SagaMapper**: New class consolidating saga mapping logic previously in `SagaMetadata`
- **SagaMetadata**: Simplified by removing finder scanning and delegating mapping to `SagaMapper`

### 2. Finder Disposal
The PR adds proper disposal support for custom finders:
- Finders implementing `IDisposable` are synchronously disposed
- Finders implementing `IAsyncDisposable` are asynchronously disposed

### 3. Test Updates
All acceptance tests and unit tests were updated to use the new explicit finder registration:
- `When_adding_state_to_context.cs`
- `When_finder_cant_find_saga_instance.cs`
- `When_finder_returns_existing_saga.cs`
- Various saga metadata creation tests

### 4. Public API Surface
The public API surface was reduced by removing:
- Type-based overloads for finder discovery
- Internal properties exposed on public types
- Convention-based initialization methods

## Migration Guide

### For Users with Custom Saga Finders

If you have custom saga finders in your codebase:

1. **Identify Custom Finders**: Find all classes implementing `ISagaFinder<TSagaData, TMessage>`

2. **Add Explicit Registration**: In each saga's `ConfigureHowToFindSaga` method, add:
   ```csharp
   mapper.ConfigureFinderMapping<MessageType, CustomFinderType>();
   ```

3. **Update Validation**: If you have custom validation that was catching specific exception types, update to catch `ArgumentException`

### Example Migration

**Before:**
```csharp
public class OrderSaga : Saga<OrderSagaData>,
    IAmStartedByMessages<StartOrder>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
    {
        // Finder automatically discovered
    }
    
    public class OrderFinder : ISagaFinder<OrderSagaData, StartOrder>
    {
        public async Task<OrderSagaData> FindBy(StartOrder message, ...)
        {
            // Custom logic
        }
    }
}
```

**After:**
```csharp
public class OrderSaga : Saga<OrderSagaData>,
    IAmStartedByMessages<StartOrder>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
    {
        mapper.ConfigureFinderMapping<StartOrder, OrderFinder>();
    }
    
    public class OrderFinder : ISagaFinder<OrderSagaData, StartOrder>
    {
        public async Task<OrderSagaData> FindBy(StartOrder message, ...)
        {
            // Custom logic
        }
    }
}
```

## Rationale

The move from implicit scanning to explicit registration provides several benefits:

1. **Performance**: Eliminates assembly scanning overhead during endpoint startup
2. **Clarity**: Makes finder registration explicit and visible in code
3. **AOT Compatibility**: Supports ahead-of-time compilation scenarios where reflection is limited
4. **Trimming Support**: Better compatibility with IL trimming for smaller deployment sizes
5. **Type Safety**: Compile-time validation of finder configurations

## Review Feedback

From the Copilot review summary:
- The changes consolidate saga metadata creation logic
- Code modernization improves readability
- The explicit API makes finder configuration more discoverable
- Removal of automatic scanning aligns with the broader NServiceBus 10 direction

## Testing

The PR includes comprehensive test updates:
- Unit tests for finder disposal (sync and async)
- Acceptance tests for finder integration
- Validation tests for proper error handling
- Tests ensuring finder invocation with proper context

## Related Work

This PR is part of a broader effort in NServiceBus 10 to:
- Move away from convention-based scanning
- Provide explicit registration APIs
- Improve AOT and trimming compatibility
- Modernize the codebase with current C# features

Similar changes have been made for:
- Installer registration (PR #7428)
- Feature activation (PR #7413)
- Persistence configuration

## Compatibility

- **Target Framework**: .NET Core 3.1+ / .NET Framework 4.7.2+
- **NServiceBus Version**: 10.0 (breaking change)
- **Persistence**: All persistence packages will need updates to work with the new API

## Documentation Needs

The following documentation should be updated:
1. Saga finder configuration documentation
2. Migration guide from NServiceBus 9 to 10
3. Custom finder implementation examples
4. Troubleshooting guide for common migration issues

## Links

- **Pull Request**: https://github.com/Particular/NServiceBus/pull/7445
- **Branch**: `explicit-saga-finders`
- **Milestone**: NServiceBus 10.0
