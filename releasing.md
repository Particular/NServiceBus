### Required updates as part of a release
* Update references to NServiceBus.Core in the [Wire Compatibility Tests](https://github.com/Particular/NServiceBus.WireCompatibilityTests).
* If the Core release is a major version release, update references to NServiceBus.Core in [Serializer.CompatTests](https://github.com/Particular/NServiceBus.Serializers.CompatTests).
* Core has many downstream packages. It is important to check that these downstream work with the new package, their builds are green and all their tests pass.
