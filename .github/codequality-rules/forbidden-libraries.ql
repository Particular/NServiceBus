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

from PackageReference package
where isForbiddenLibrary(package.getPackageName())
select package,
  "The use of " + package.getPackageName() +
  " is not allowed. Please use an approved mocking library instead."

