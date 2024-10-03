/**
 * @name Forbidden test mocking libraries
 * @description This query identifies any usage of the forbidden NuGet packages Moq and FakeItEasy by checking imports.
 * @kind problem
 * @problem.severity error
 */

import csharp

predicate isForbiddenLibrary(string name) {
  name = "FakeItEasy" or name = "Moq"
}

from ImportDirective import
where isForbiddenLibrary(import.getImportName())
select import, "This project imports a forbidden library: " + import.getImportName()
