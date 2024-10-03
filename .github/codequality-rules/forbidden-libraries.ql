/**
 * @name Forbidden test mocking libraries
 * @description Prevents the use of FakeItEasy and Moq libraries in the project
 * @kind problem
 * @problem.severity error
 * @precision high
 * @id cs/forbidden-mocking-libraries
 */

import csharp

predicate isForbiddenLibrary(string name) {
  name = "FakeItEasy" or name = "Moq"
}

from NamespaceImport import
where isForbiddenLibrary(import.getImportedNamespace())
select import, "This project imports a forbidden library: " + import.getImportedNamespace()
