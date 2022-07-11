## Language Features

It is acceptable to use language features for the given `LangVersion` defined in the project that compile successfully against all targeted TFMs. Note that all language features later than C# 7.3 are not officially supported for .NET Framework so compatibility varies on a feature by feature level. This can result in some language features to cause compiler errors or slower performance.

## Performance related

For public facing interfaces we favor read-only collections and enumerables. For internal types we favor speed and allocation reduction. Avoid using collection interfaces internally.

### In hot paths

* Avoid allocations
* Avoid using `System.Linq`
* Avoid using `foreach` over collections that do not have a struct enumerator
