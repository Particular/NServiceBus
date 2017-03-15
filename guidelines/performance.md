## Performance considerations

* Avoid allocations in hot paths:
* Avoid LINQ.
* Avoid using foreach over collections that do not have a struct enumerator. For public facing interfaces we favor readonly collections and enumerables. For internal types we favor speed and allocation reduction, therefore avoid using collection interfaces internally.

